using FixedIT.API.Configuration;
using FixedIT.API.Constants;
using FixedIT.API.Localization;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();
        var options = serviceProvider.GetRequiredService<IOptions<SeedDataOptions>>().Value;

        ValidateOptions(options);
        await SeedRolesAsync(roleManager);
        await SeedReferenceDataAsync(db);

        var cities = await db.Cities.ToDictionaryAsync(city => city.Name);
        var categories = await db.Categories.ToDictionaryAsync(category => category.Name);

        await EnsureUserAsync(
            userManager,
            options.AdminEmail,
            "FixedIT",
            "Administrator",
            cities[SeedDataConstants.CityNames[0]].Id,
            null,
            options.AdminPassword,
            RoleNames.Admin);

        var clients = new List<User>();
        foreach (var seedClient in SeedDataConstants.Clients)
        {
            clients.Add(await EnsureUserAsync(
                userManager,
                seedClient.Email,
                seedClient.FirstName,
                seedClient.LastName,
                cities[seedClient.CityName].Id,
                seedClient.PhoneNumber,
                options.DefaultUserPassword,
                RoleNames.Client));
        }

        var professionals = new List<ProfessionalProfile>();
        foreach (var seedProfessional in SeedDataConstants.Professionals)
        {
            var seedUser = seedProfessional.User;
            var user = await EnsureUserAsync(
                userManager,
                seedUser.Email,
                seedUser.FirstName,
                seedUser.LastName,
                cities[seedUser.CityName].Id,
                seedUser.PhoneNumber,
                options.DefaultUserPassword,
                RoleNames.Professional);

            professionals.Add(await EnsureProfessionalProfileAsync(
                db,
                user,
                seedProfessional,
                categories));
        }

        await SeedCompletedReservationsAsync(db, clients, professionals);
    }

    private static void ValidateOptions(SeedDataOptions options)
    {
        if (SeedDataConstants.Reservations.Length < SeedDataConstants.SeededReservationCount)
        {
            throw new InvalidOperationException(
                $"Potrebno je najmanje {SeedDataConstants.SeededReservationCount} završenih početnih rezervacija.");
        }

        if (string.IsNullOrWhiteSpace(options.AdminEmail)
            || string.IsNullOrWhiteSpace(options.AdminPassword)
            || string.IsNullOrWhiteSpace(options.DefaultUserPassword))
        {
            throw new InvalidOperationException(
                $"Prije pokretanja API-ja konfigurišite {SeedDataOptions.SectionName}:AdminEmail, AdminPassword i DefaultUserPassword.");
        }
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.Client, RoleNames.Professional })
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            EnsureSucceeded(await roleManager.CreateAsync(new Role { Name = roleName }));
        }
    }

    private static async Task SeedReferenceDataAsync(AppDbContext db)
    {
        const int defaultCountryId = 1;
        var existingCityNames = await db.Cities.Select(city => city.Name).ToHashSetAsync();
        foreach (var cityName in SeedDataConstants.CityNames.Where(name => !existingCityNames.Contains(name)))
        {
            db.Cities.Add(new City { Name = cityName, CountryId = defaultCountryId });
        }

        var existingCategoryNames = await db.Categories.Select(category => category.Name).ToHashSetAsync();
        foreach (var category in SeedDataConstants.Categories.Where(item => !existingCategoryNames.Contains(item.Name)))
        {
            db.Categories.Add(new Category
            {
                Name = category.Name,
                Description = category.Description
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<User> EnsureUserAsync(
        UserManager<User> userManager,
        string email,
        string firstName,
        string lastName,
        int cityId,
        string? phoneNumber,
        string password,
        string roleName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                CityId = cityId,
                PhoneNumber = phoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            EnsureSucceeded(await userManager.CreateAsync(user, password));
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, roleName));
        }

        return user;
    }

    private static async Task<ProfessionalProfile> EnsureProfessionalProfileAsync(
        AppDbContext db,
        User user,
        SeedProfessional seed,
        IReadOnlyDictionary<string, Category> categories)
    {
        var profile = await db.ProfessionalProfiles
            .SingleOrDefaultAsync(item => item.UserId == user.Id);

        if (profile is null)
        {
            profile = new ProfessionalProfile
            {
                UserId = user.Id,
                Bio = seed.Bio,
                HourlyRate = seed.HourlyRate,
                YearsOfExperience = seed.YearsOfExperience,
                IsVerified = true
            };
            db.ProfessionalProfiles.Add(profile);
            await db.SaveChangesAsync();
        }

        var existingCategoryIds = await db.ProfessionalCategories
            .Where(link => link.ProfessionalProfileId == profile.Id)
            .Select(link => link.CategoryId)
            .ToHashSetAsync();

        foreach (var categoryName in seed.CategoryNames)
        {
            var categoryId = categories[categoryName].Id;
            if (existingCategoryIds.Add(categoryId))
            {
                db.ProfessionalCategories.Add(new ProfessionalCategory
                {
                    ProfessionalProfileId = profile.Id,
                    CategoryId = categoryId
                });
            }
        }

        await db.SaveChangesAsync();
        return profile;
    }

    private static async Task SeedCompletedReservationsAsync(
        AppDbContext db,
        IReadOnlyList<User> clients,
        IReadOnlyList<ProfessionalProfile> professionals)
    {
        var now = DateTime.UtcNow;

        foreach (var seed in SeedDataConstants.Reservations)
        {
            var reservation = await db.Reservations
                .SingleOrDefaultAsync(item => item.ServiceDescription == seed.ServiceDescription);

            if (reservation is null)
            {
                var scheduledAt = now.AddDays(-seed.DaysAgo);
                reservation = new Reservation
                {
                    ClientUserId = clients[seed.ClientIndex].Id,
                    ProfessionalProfileId = professionals[seed.ProfessionalIndex].Id,
                    ServiceDescription = seed.ServiceDescription,
                    ScheduledAt = scheduledAt,
                    DurationMinutes = SeedDataConstants.DefaultDurationMinutes,
                    Status = ReservationStatus.Completed,
                    TotalPrice = seed.TotalPrice,
                    CreatedAt = scheduledAt.AddDays(-1),
                    UpdatedAt = scheduledAt
                };
                db.Reservations.Add(reservation);
                await db.SaveChangesAsync();
            }

            var review = await db.Reviews
                .SingleOrDefaultAsync(item => item.ReservationId == reservation.Id);
            if (review is null)
            {
                review = new Review
                {
                    ReservationId = reservation.Id,
                    ClientUserId = reservation.ClientUserId,
                    ProfessionalProfileId = reservation.ProfessionalProfileId,
                    Rating = seed.Rating,
                    Comment = seed.ReviewComment,
                    CreatedAt = reservation.UpdatedAt
                };
                db.Reviews.Add(review);
                await db.SaveChangesAsync();
            }

            var userRating = await db.UserRatings.SingleOrDefaultAsync(rating =>
                    rating.UserId == reservation.ClientUserId
                    && rating.ProfessionalProfileId == reservation.ProfessionalProfileId
                    && rating.Timestamp == reservation.UpdatedAt);
            if (userRating is null)
            {
                db.UserRatings.Add(new UserRating
                {
                    ReviewId = review.Id,
                    UserId = reservation.ClientUserId,
                    ProfessionalProfileId = reservation.ProfessionalProfileId,
                    Rating = seed.Rating,
                    Timestamp = reservation.UpdatedAt
                });
            }
            else if (!userRating.ReviewId.HasValue)
            {
                userRating.ReviewId = review.Id;
            }

            await db.SaveChangesAsync();
        }

        foreach (var profile in professionals)
        {
            profile.AverageRating = await db.Reviews
                .Where(review => review.ProfessionalProfileId == profile.Id)
                .AverageAsync(review => (decimal)review.Rating);
        }

        await db.SaveChangesAsync();
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Kreiranje početnih korisničkih podataka nije uspjelo: {BosnianIdentityErrors.Build(result)}");
    }
}
