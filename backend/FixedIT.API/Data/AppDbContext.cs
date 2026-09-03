using FixedIT.API.Constants;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, string>(options)
{
    public DbSet<City> Cities => Set<City>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ReservationStatusDefinition> ReservationStatusDefinitions => Set<ReservationStatusDefinition>();
    public DbSet<ProfessionalProfile> ProfessionalProfiles => Set<ProfessionalProfile>();
    public DbSet<ProfessionalCategory> ProfessionalCategories => Set<ProfessionalCategory>();
    public DbSet<ProfessionalAvailability> ProfessionalAvailabilities => Set<ProfessionalAvailability>();
    public DbSet<PortfolioItem> PortfolioItems => Set<PortfolioItem>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobPostingImage> JobPostingImages => Set<JobPostingImage>();
    public DbSet<JobOffer> JobOffers => Set<JobOffer>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationStatusHistory> ReservationStatusHistories => Set<ReservationStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserRating> UserRatings => Set<UserRating>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RecommendationActivity> RecommendationActivities => Set<RecommendationActivity>();

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>()
            .HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureUser(builder);
        ConfigureReferenceData(builder);
        ConfigureProfessional(builder);
        ConfigureJobs(builder);
        ConfigureReservations(builder);
        ConfigureCommunication(builder);
        ConfigureOperationalData(builder);
    }

    private static void ConfigureUser(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.Property(user => user.FirstName)
                .HasMaxLength(DatabaseConstants.NameMaxLength)
                .IsRequired();
            entity.Property(user => user.LastName)
                .HasMaxLength(DatabaseConstants.NameMaxLength)
                .IsRequired();
            entity.Property(user => user.ProfilePictureUrl)
                .HasMaxLength(DatabaseConstants.UrlMaxLength);
            entity.Property(user => user.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(user => user.CityId);

            entity.HasOne(user => user.City)
                .WithMany(city => city.Users)
                .HasForeignKey(user => user.CityId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureReferenceData(ModelBuilder builder)
    {
        builder.Entity<City>(entity =>
        {
            entity.Property(city => city.Name)
                .HasMaxLength(DatabaseConstants.NameMaxLength)
                .IsRequired();
            entity.HasIndex(city => new { city.CountryId, city.Name }).IsUnique();
            entity.HasOne(city => city.Country)
                .WithMany(country => country.Cities)
                .HasForeignKey(city => city.CountryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Country>(entity =>
        {
            entity.Property(country => country.Name)
                .HasMaxLength(DatabaseConstants.NameMaxLength)
                .IsRequired();
            entity.Property(country => country.Code)
                .HasMaxLength(3)
                .IsRequired();
            entity.HasIndex(country => country.Name).IsUnique();
            entity.HasIndex(country => country.Code).IsUnique();
            entity.HasData(new Country
            {
                Id = 1,
                Name = "Bosna i Hercegovina",
                Code = "BIH"
            });
        });

        builder.Entity<ReservationStatusDefinition>(entity =>
        {
            entity.Property(status => status.Name)
                .HasMaxLength(DatabaseConstants.NameMaxLength)
                .IsRequired();
            entity.Property(status => status.Description)
                .HasMaxLength(DatabaseConstants.DescriptionMaxLength)
                .IsRequired();
            entity.HasData(
                new ReservationStatusDefinition { Id = ReservationStatus.Pending, Name = "Na čekanju", Description = "Rezervacija čeka odgovor profesionalca." },
                new ReservationStatusDefinition { Id = ReservationStatus.Accepted, Name = "Prihvaćena", Description = "Profesionalac je prihvatio rezervaciju." },
                new ReservationStatusDefinition { Id = ReservationStatus.InProgress, Name = "U toku", Description = "Rad na rezervaciji je započet." },
                new ReservationStatusDefinition { Id = ReservationStatus.Completed, Name = "Završena", Description = "Rezervisani posao je završen." },
                new ReservationStatusDefinition { Id = ReservationStatus.Cancelled, Name = "Otkazana", Description = "Rezervacija je otkazana." });
        });

        builder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name)
                .HasMaxLength(DatabaseConstants.NameMaxLength)
                .IsRequired();
            entity.Property(category => category.Description)
                .HasMaxLength(DatabaseConstants.DescriptionMaxLength)
                .IsRequired();
            entity.Property(category => category.IconUrl)
                .HasMaxLength(DatabaseConstants.UrlMaxLength);
            entity.HasIndex(category => category.Name).IsUnique();
        });
    }

    private static void ConfigureProfessional(ModelBuilder builder)
    {
        builder.Entity<ProfessionalProfile>(entity =>
        {
            entity.Property(profile => profile.Bio)
                .HasMaxLength(DatabaseConstants.DescriptionMaxLength)
                .IsRequired();
            entity.Property(profile => profile.HourlyRate)
                .HasPrecision(DatabaseConstants.DecimalPrecision, DatabaseConstants.DecimalScale);
            entity.Property(profile => profile.AverageRating)
                .HasPrecision(DatabaseConstants.DecimalPrecision, DatabaseConstants.DecimalScale);
            entity.HasIndex(profile => profile.UserId).IsUnique();

            entity.HasOne(profile => profile.User)
                .WithOne(user => user.ProfessionalProfile)
                .HasForeignKey<ProfessionalProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProfessionalCategory>(entity =>
        {
            entity.HasKey(link => new { link.ProfessionalProfileId, link.CategoryId });
            entity.HasOne(link => link.ProfessionalProfile)
                .WithMany(profile => profile.ProfessionalCategories)
                .HasForeignKey(link => link.ProfessionalProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Category)
                .WithMany(category => category.ProfessionalCategories)
                .HasForeignKey(link => link.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProfessionalAvailability>(entity =>
        {
            entity.Property(item => item.StartTime).HasColumnType("time");
            entity.Property(item => item.EndTime).HasColumnType("time");
            entity.HasIndex(item => new { item.ProfessionalProfileId, item.DayOfWeek, item.StartTime })
                .IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ProfessionalAvailabilities_TimeRange",
                "[StartTime] < [EndTime]"));
            entity.HasOne(item => item.ProfessionalProfile)
                .WithMany(profile => profile.Availability)
                .HasForeignKey(item => item.ProfessionalProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PortfolioItem>(entity =>
        {
            entity.Property(item => item.Title)
                .HasMaxLength(DatabaseConstants.TitleMaxLength)
                .IsRequired();
            entity.Property(item => item.Description)
                .HasMaxLength(DatabaseConstants.DescriptionMaxLength)
                .IsRequired();
            entity.Property(item => item.ImageUrl)
                .HasMaxLength(DatabaseConstants.UrlMaxLength)
                .IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnType("datetime2");
            entity.HasOne(item => item.ProfessionalProfile)
                .WithMany(profile => profile.PortfolioItems)
                .HasForeignKey(item => item.ProfessionalProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureJobs(ModelBuilder builder)
    {
        builder.Entity<JobPosting>(entity =>
        {
            entity.Property(job => job.Title)
                .HasMaxLength(DatabaseConstants.TitleMaxLength)
                .IsRequired();
            entity.Property(job => job.Description)
                .HasMaxLength(DatabaseConstants.DescriptionMaxLength)
                .IsRequired();
            entity.Property(job => job.Budget)
                .HasPrecision(DatabaseConstants.DecimalPrecision, DatabaseConstants.DecimalScale);
            entity.Property(job => job.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(job => new { job.Status, job.CategoryId, job.CityId });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_JobPostings_Budget_Positive",
                "[Budget] > 0"));

            entity.HasOne(job => job.ClientUser)
                .WithMany(user => user.JobPostings)
                .HasForeignKey(job => job.ClientUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(job => job.Category)
                .WithMany(category => category.JobPostings)
                .HasForeignKey(job => job.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(job => job.City)
                .WithMany(city => city.JobPostings)
                .HasForeignKey(job => job.CityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobOffer>(entity =>
        {
            entity.Property(offer => offer.Message)
                .HasMaxLength(DatabaseConstants.DescriptionMaxLength)
                .IsRequired();
            entity.Property(offer => offer.ProposedPrice)
                .HasPrecision(DatabaseConstants.DecimalPrecision, DatabaseConstants.DecimalScale);
            entity.HasIndex(offer => new { offer.JobPostingId, offer.ProfessionalProfileId })
                .IsUnique();
            entity.HasIndex(offer => new { offer.JobPostingId, offer.Status })
                .IsUnique()
                .HasFilter("[Status] = 2");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_JobOffers_ProposedPrice_Positive",
                "[ProposedPrice] > 0"));

            entity.HasOne(offer => offer.JobPosting)
                .WithMany(job => job.Offers)
                .HasForeignKey(offer => offer.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(offer => offer.ProfessionalProfile)
                .WithMany(profile => profile.JobOffers)
                .HasForeignKey(offer => offer.ProfessionalProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureReservations(ModelBuilder builder)
    {
        builder.Entity<Reservation>(entity =>
        {
            entity.Property(reservation => reservation.ServiceDescription)
                .HasMaxLength(DatabaseConstants.DescriptionMaxLength)
                .IsRequired();
            entity.Property(reservation => reservation.CancellationReason)
                .HasMaxLength(DatabaseConstants.ShortTextMaxLength);
            entity.Property(reservation => reservation.TotalPrice)
                .HasPrecision(DatabaseConstants.DecimalPrecision, DatabaseConstants.DecimalScale);
            entity.Property(reservation => reservation.ScheduledAt).HasColumnType("datetime2");
            entity.Property(reservation => reservation.CreatedAt).HasColumnType("datetime2");
            entity.Property(reservation => reservation.UpdatedAt).HasColumnType("datetime2");
            entity.HasIndex(reservation => new
            {
                reservation.ProfessionalProfileId,
                reservation.ScheduledAt
            })
                .IsUnique()
                .HasFilter("[Status] <> 5");
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Reservations_DurationMinutes_Positive",
                    "[DurationMinutes] > 0");
                table.HasCheckConstraint(
                    "CK_Reservations_TotalPrice_Positive",
                    "[TotalPrice] > 0");
            });

            entity.HasOne(reservation => reservation.ClientUser)
                .WithMany(user => user.ClientReservations)
                .HasForeignKey(reservation => reservation.ClientUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reservation => reservation.ProfessionalProfile)
                .WithMany(profile => profile.Reservations)
                .HasForeignKey(reservation => reservation.ProfessionalProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reservation => reservation.Category)
                .WithMany(category => category.Reservations)
                .HasForeignKey(reservation => reservation.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reservation => reservation.StatusDefinition)
                .WithMany(status => status.Reservations)
                .HasForeignKey(reservation => reservation.Status)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobPostingImage>(entity =>
        {
            entity.Property(image => image.ImageUrl)
                .HasMaxLength(DatabaseConstants.UrlMaxLength)
                .IsRequired();
            entity.Property(image => image.CreatedAt).HasColumnType("datetime2");
            entity.HasOne(image => image.JobPosting)
                .WithMany(job => job.Images)
                .HasForeignKey(image => image.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property(payment => payment.PayPalOrderId)
                .HasMaxLength(DatabaseConstants.ExternalIdMaxLength)
                .IsRequired();
            entity.Property(payment => payment.PayPalCaptureId)
                .HasMaxLength(DatabaseConstants.ExternalIdMaxLength);
            entity.Property(payment => payment.PayPalRefundId)
                .HasMaxLength(DatabaseConstants.ExternalIdMaxLength);
            entity.Property(payment => payment.Amount)
                .HasPrecision(DatabaseConstants.DecimalPrecision, DatabaseConstants.DecimalScale);
            entity.Property(payment => payment.Currency)
                .HasMaxLength(DatabaseConstants.CurrencyMaxLength)
                .IsRequired();
            entity.Property(payment => payment.CreatedAt).HasColumnType("datetime2");
            entity.Property(payment => payment.CompletedAt).HasColumnType("datetime2");
            entity.Property(payment => payment.RefundedAt).HasColumnType("datetime2");
            entity.HasIndex(payment => payment.ReservationId).IsUnique();
            entity.HasIndex(payment => payment.PayPalOrderId).IsUnique();
            entity.HasIndex(payment => payment.PayPalCaptureId)
                .IsUnique()
                .HasFilter("[PayPalCaptureId] IS NOT NULL");
            entity.HasIndex(payment => payment.PayPalRefundId)
                .IsUnique()
                .HasFilter("[PayPalRefundId] IS NOT NULL");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Payments_Amount_Positive",
                "[Amount] > 0"));
            entity.HasOne(payment => payment.Reservation)
                .WithOne(reservation => reservation.Payment)
                .HasForeignKey<Payment>(payment => payment.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Review>(entity =>
        {
            entity.Property(review => review.Comment)
                .HasMaxLength(DatabaseConstants.DescriptionMaxLength)
                .IsRequired();
            entity.Property(review => review.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(review => review.ReservationId).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Reviews_Rating",
                $"[Rating] >= {DatabaseConstants.RatingMinimum} AND [Rating] <= {DatabaseConstants.RatingMaximum}"));

            entity.HasOne(review => review.Reservation)
                .WithOne(reservation => reservation.Review)
                .HasForeignKey<Review>(review => review.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(review => review.ClientUser)
                .WithMany(user => user.Reviews)
                .HasForeignKey(review => review.ClientUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(review => review.ProfessionalProfile)
                .WithMany(profile => profile.Reviews)
                .HasForeignKey(review => review.ProfessionalProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCommunication(ModelBuilder builder)
    {
        builder.Entity<Conversation>(entity =>
        {
            entity.Property(conversation => conversation.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(conversation => conversation.ReservationId)
                .IsUnique()
                .HasFilter("[ReservationId] IS NOT NULL");
            entity.HasOne(conversation => conversation.Reservation)
                .WithOne(reservation => reservation.Conversation)
                .HasForeignKey<Conversation>(conversation => conversation.ReservationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(participant => new { participant.ConversationId, participant.UserId });
            entity.HasOne(participant => participant.Conversation)
                .WithMany(conversation => conversation.Participants)
                .HasForeignKey(participant => participant.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(participant => participant.User)
                .WithMany(user => user.ConversationParticipations)
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Message>(entity =>
        {
            entity.Property(message => message.Content)
                .HasMaxLength(DatabaseConstants.ContentMaxLength)
                .IsRequired();
            entity.Property(message => message.SentAt).HasColumnType("datetime2");
            entity.HasIndex(message => new { message.ConversationId, message.SentAt });
            entity.HasOne(message => message.Conversation)
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(message => message.SenderUser)
                .WithMany(user => user.SentMessages)
                .HasForeignKey(message => message.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureOperationalData(ModelBuilder builder)
    {
        builder.Entity<Notification>(entity =>
        {
            entity.Property(notification => notification.Title)
                .HasMaxLength(DatabaseConstants.TitleMaxLength)
                .IsRequired();
            entity.Property(notification => notification.Body)
                .HasMaxLength(DatabaseConstants.DescriptionMaxLength)
                .IsRequired();
            entity.Property(notification => notification.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(notification => new { notification.UserId, notification.IsRead, notification.CreatedAt });
            entity.HasOne(notification => notification.User)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(log => log.Action)
                .HasMaxLength(DatabaseConstants.ActionMaxLength)
                .IsRequired();
            entity.Property(log => log.EntityType)
                .HasMaxLength(DatabaseConstants.EntityTypeMaxLength)
                .IsRequired();
            entity.Property(log => log.EntityId)
                .HasMaxLength(DatabaseConstants.ExternalIdMaxLength)
                .IsRequired();
            entity.Property(log => log.Details)
                .HasMaxLength(DatabaseConstants.ContentMaxLength);
            entity.Property(log => log.IpAddress)
                .HasMaxLength(DatabaseConstants.IpAddressMaxLength)
                .IsRequired();
            entity.Property(log => log.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(log => log.CreatedAt);
            entity.HasOne(log => log.User)
                .WithMany(user => user.AuditLogs)
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserRating>(entity =>
        {
            entity.Property(rating => rating.Timestamp).HasColumnType("datetime2");
            entity.HasIndex(rating => rating.ReviewId)
                .IsUnique()
                .HasFilter("[ReviewId] IS NOT NULL");
            entity.HasIndex(rating => new { rating.UserId, rating.ProfessionalProfileId });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_UserRatings_Rating",
                $"[Rating] >= {DatabaseConstants.RatingMinimum} AND [Rating] <= {DatabaseConstants.RatingMaximum}"));
            entity.HasOne(rating => rating.User)
                .WithMany(user => user.Ratings)
                .HasForeignKey(rating => rating.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(rating => rating.ProfessionalProfile)
                .WithMany(profile => profile.UserRatings)
                .HasForeignKey(rating => rating.ProfessionalProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(rating => rating.Review)
                .WithOne(review => review.UserRating)
                .HasForeignKey<UserRating>(rating => rating.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasQueryFilter(token => token.User.IsActive);
            entity.Property(token => token.TokenHash)
                .HasMaxLength(DatabaseConstants.TokenHashMaxLength)
                .IsRequired();
            entity.Property(token => token.ExpiresAt).HasColumnType("datetime2");
            entity.Property(token => token.CreatedAt).HasColumnType("datetime2");
            entity.Property(token => token.RevokedAt).HasColumnType("datetime2");
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.RevokedAt, token.ExpiresAt });
            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RecommendationActivity>(entity =>
        {
            entity.Property(activity => activity.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(activity => new { activity.UserId, activity.CreatedAt });
            entity.HasIndex(activity => new
            {
                activity.Type,
                activity.ProfessionalProfileId,
                activity.CategoryId
            });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_RecommendationActivities_Target",
                "([Type] = 1 AND [ProfessionalProfileId] IS NOT NULL AND [CategoryId] IS NULL) "
                + "OR ([Type] = 2 AND [ProfessionalProfileId] IS NULL AND [CategoryId] IS NOT NULL)"));
            entity.HasOne(activity => activity.User)
                .WithMany(user => user.RecommendationActivities)
                .HasForeignKey(activity => activity.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(activity => activity.ProfessionalProfile)
                .WithMany(profile => profile.RecommendationActivities)
                .HasForeignKey(activity => activity.ProfessionalProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(activity => activity.Category)
                .WithMany(category => category.RecommendationActivities)
                .HasForeignKey(activity => activity.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ReservationStatusHistory>(entity =>
        {
            entity.Property(history => history.Reason)
                .HasMaxLength(DatabaseConstants.ShortTextMaxLength);
            entity.Property(history => history.ChangedByUserId)
                .HasMaxLength(DatabaseConstants.UserIdMaxLength)
                .IsRequired();
            entity.Property(history => history.ChangedAt).HasColumnType("datetime2");
            entity.HasIndex(history => new { history.ReservationId, history.ChangedAt });
            entity.HasOne(history => history.Reservation)
                .WithMany(reservation => reservation.StatusHistory)
                .HasForeignKey(history => history.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(token => token.CreatedAt).HasColumnType("datetime2");
            entity.Property(token => token.ExpiresAt).HasColumnType("datetime2");
            entity.Property(token => token.UsedAt).HasColumnType("datetime2");
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAt });
            entity.HasOne(token => token.User)
                .WithMany(user => user.PasswordResetTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
