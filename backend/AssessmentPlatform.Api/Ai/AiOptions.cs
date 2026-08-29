namespace AssessmentPlatform.Api.Ai;

public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>"Gemini" for the real Google API, "Stub" for canned offline responses (no key needed).</summary>
    public string Provider { get; set; } = "Gemini";

    /// <summary>Gemini API key from https://aistudio.google.com/app/apikey. Set via env var in production.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.5-flash-lite";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public int TimeoutSeconds { get; set; } = 45;
    public int MaxRetries { get; set; } = 2;
}
