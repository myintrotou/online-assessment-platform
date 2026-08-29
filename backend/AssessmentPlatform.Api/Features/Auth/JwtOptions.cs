namespace AssessmentPlatform.Api.Features.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "assessment-platform";
    public string Audience { get; set; } = "assessment-platform";

    /// <summary>Signing secret. Must be at least 32 chars. Set from an env var in production.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 720;
}
