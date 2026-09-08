using FixedIT.API.Constants;
using FixedIT.API.BackgroundServices;
using FixedIT.API.Configuration;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.Filters;
using FixedIT.API.Hubs;
using FixedIT.API.Localization;
using FixedIT.API.Middleware;
using FixedIT.API.Models;
using FixedIT.API.Services;
using FixedIT.API.Services.ML;
using FixedIT.Shared.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using RabbitMQ.Client;
using QuestPDF.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers(options =>
    options.Filters.AddService<AuditLogActionFilter>(order: -3000));
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
        RateLimitPolicyNames.PasswordReset,
        httpContext =>
        {
            var security = httpContext.RequestServices
                .GetRequiredService<IOptions<SecurityOptions>>()
                .Value;
            var address = httpContext.Connection.RemoteIpAddress?.ToString() ?? "nepoznato";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: address,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = security.PasswordResetIpPermitLimit,
                    Window = TimeSpan.FromMinutes(security.PasswordResetIpWindowMinutes),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
        });
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = BosnianValidationMessages.Build(context.ModelState);
        var response = new ErrorResponse(
            StatusCodes.Status400BadRequest,
            message,
            context.HttpContext.TraceIdentifier);
        return new BadRequestObjectResult(response);
    };
});
builder.Services.Configure<SeedDataOptions>(
    builder.Configuration.GetSection(SeedDataOptions.SectionName));
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<SecurityOptions>()
    .Bind(builder.Configuration.GetSection(SecurityOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<SchedulingOptions>()
    .Bind(builder.Configuration.GetSection(SchedulingOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options =>
        {
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
            catch (InvalidTimeZoneException)
            {
                return false;
            }
        },
        "Scheduling:TimeZoneId mora biti važeća vremenska zona.")
    .ValidateOnStart();
builder.Services
    .AddOptions<PaginationOptions>()
    .Bind(builder.Configuration.GetSection(PaginationOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => options.DefaultPageSize <= options.MaxPageSize,
        "Pagination:DefaultPageSize ne može biti veći od Pagination:MaxPageSize.")
    .ValidateOnStart();
builder.Services
    .AddOptions<FileStorageOptions>()
    .Bind(builder.Configuration.GetSection(FileStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => !Path.IsPathRooted(options.RootPath)
            && !options.RootPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Contains("..", StringComparer.Ordinal),
        "FileStorage:RootPath mora biti sigurna relativna putanja.")
    .Validate(
        options => options.RequestPath.StartsWith('/')
            && !options.RequestPath.Contains("..", StringComparison.Ordinal),
        "FileStorage:RequestPath mora biti sigurna apsolutna putanja zahtjeva.")
    .Validate(
        options => options.AllowedImageMimeTypes.Length > 0
            && options.AllowedImageMimeTypes.All(mimeType =>
                string.Equals(mimeType, FileStorageConstants.JpegMimeType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(mimeType, FileStorageConstants.PngMimeType, StringComparison.OrdinalIgnoreCase)),
        "FileStorage:AllowedImageMimeTypes smije sadržavati samo image/jpeg i image/png.")
    .ValidateOnStart();
builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => options.DeliveryRetryDelaysSeconds.All(delay => delay is >= 1 and <= 300),
        "RabbitMQ:DeliveryRetryDelaysSeconds mora sadržavati vrijednosti od 1 do 300 sekundi.")
    .ValidateOnStart();
builder.Services
    .AddOptions<PayPalOptions>()
    .Bind(builder.Configuration.GetSection(PayPalOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => options.BaseUrl.EndsWith("/", StringComparison.Ordinal),
        "PayPal:BaseUrl mora završavati znakom '/'.")
    .ValidateOnStart();
builder.Services
    .AddOptions<RecommendationOptions>()
    .Bind(builder.Configuration.GetSection(RecommendationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<ReportOptions>()
    .Bind(builder.Configuration.GetSection(ReportOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => Enum.TryParse<LicenseType>(options.QuestPdfLicense, true, out _),
        "Reports:QuestPdfLicense mora sadržavati ispravan tip QuestPDF licence.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection mora biti konfigurisan.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services
    .AddIdentityCore<User>(options => options.User.RequireUniqueEmail = true)
    .AddRoles<Role>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IPasswordHasher<User>, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPaginationService, PaginationService>();
builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfessionalService, ProfessionalService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IJobSearchService, JobSearchService>();
builder.Services.AddScoped<IJobPostingService, JobPostingService>();
builder.Services.AddScoped<IJobOfferService, JobOfferService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationStateService, ReservationStateService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<IPaymentService>(serviceProvider =>
    serviceProvider.GetRequiredService<PaymentService>());
builder.Services.AddScoped<IPaymentRefundService>(serviceProvider =>
    serviceProvider.GetRequiredService<PaymentService>());
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRecommendationQueryService, RecommendationQueryService>();
builder.Services.AddScoped<IRecommendationActivityService, RecommendationActivityService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<AuditLogActionFilter>();
builder.Services.AddScoped<RabbitMqPublisher>();
builder.Services.AddScoped<IReservationEventPublisher>(serviceProvider =>
    serviceProvider.GetRequiredService<RabbitMqPublisher>());
builder.Services.AddScoped<INotificationEventPublisher>(serviceProvider =>
    serviceProvider.GetRequiredService<RabbitMqPublisher>());
builder.Services.AddSingleton<IConnectionFactory>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
    return new ConnectionFactory
    {
        HostName = options.Host,
        Port = options.Port,
        UserName = options.Username,
        Password = options.Password,
        VirtualHost = options.VirtualHost,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true
    };
});
builder.Services.AddSingleton<IConnection>(serviceProvider =>
    serviceProvider
        .GetRequiredService<IConnectionFactory>()
        .CreateConnection("FixedIT.API"));
builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.AddHttpClient(
    PayPalHttpClientNames.Api,
    (serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<PayPalOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    });
builder.Services.AddSingleton<IPayPalService, PayPalService>();
builder.Services.AddSingleton<RecommendationService>();
builder.Services.AddHostedService<ModelRetrainingService>();
builder.Services.AddHttpContextAccessor();

var reportOptions = builder.Configuration
    .GetSection(ReportOptions.SectionName)
    .Get<ReportOptions>()
    ?? throw new InvalidOperationException("Konfiguracija Reports mora biti navedena.");
QuestPDF.Settings.License = Enum.Parse<LicenseType>(
    reportOptions.QuestPdfLicense,
    ignoreCase: true);

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("Konfiguracija Jwt mora biti navedena.");
if (jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key mora sadržavati najmanje 32 znaka.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = JwtService.CreateTokenValidationParameters(jwtOptions);
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token)
                    && context.HttpContext.Request.Path.StartsWithSegments(
                        AuthenticationConstants.HubPathPrefix))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue(
                    ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.Fail("Korisnički nalog nije dostupan.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();
                var userState = await db.Users
                    .IgnoreQueryFilters()
                    .Where(user => user.Id == userId)
                    .Select(user => new { user.IsActive, user.SecurityStamp })
                    .SingleOrDefaultAsync(
                        context.HttpContext.RequestAborted);
                if (userState is null || !userState.IsActive)
                {
                    context.Fail("Korisnički nalog je deaktiviran.");
                    return;
                }

                var tokenSecurityStamp = context.Principal?.FindFirstValue(
                    AuthenticationConstants.SecurityStampClaimType);
                if (string.IsNullOrWhiteSpace(tokenSecurityStamp)
                    || !string.Equals(
                        tokenSecurityStamp,
                        userState.SecurityStamp,
                        StringComparison.Ordinal))
                {
                    context.Fail("Prijava više nije važeća.");
                }
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicyNames.AdminOnly,
        policy => policy.RequireRole(RoleNames.Admin));
});

var allowedOrigins = builder.Configuration
    .GetSection(ConfigurationSectionNames.AllowedOrigins)
    .Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Mora biti konfigurisan najmanje jedan CORS izvor.");
}

builder.Services.AddCors(options => options.AddPolicy(
    CorsPolicyNames.FixedIT,
    policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
var fileStorageOptions = app.Services
    .GetRequiredService<IOptions<FileStorageOptions>>()
    .Value;
var uploadRootPath = Path.GetFullPath(Path.Combine(
    app.Environment.ContentRootPath,
    fileStorageOptions.RootPath));
Directory.CreateDirectory(uploadRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRootPath),
    RequestPath = fileStorageOptions.RequestPath
});
app.UseCors(CorsPolicyNames.FixedIT);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<ChatHub>(AuthenticationConstants.ChatHubPath);
app.MapHub<NotificationsHub>(AuthenticationConstants.NotificationsHubPath);

app.Run();
