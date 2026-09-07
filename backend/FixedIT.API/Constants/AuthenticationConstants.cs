namespace FixedIT.API.Constants;

public static class AuthenticationConstants
{
    public const string SecurityStampClaimType = "fixedit_security_stamp";
    public const string RefreshTokenHeaderName = "X-Refresh-Token";
    public const string RefreshTokenCookieName = "fixedit_refresh_token";
    public const int RefreshTokenByteLength = 64;
    public const string BearerTokenType = "Bearer";
    public const string HubPathPrefix = "/hubs";
    public const string ChatHubPath = "/hubs/chat";
    public const string NotificationsHubPath = "/hubs/notifications";
}

public static class AuthorizationPolicyNames
{
    public const string AdminOnly = "AdminOnly";
}

public static class CorsPolicyNames
{
    public const string FixedIT = "FixedITPolicy";
}

public static class ConfigurationSectionNames
{
    public const string AllowedOrigins = "AllowedOrigins";
}
