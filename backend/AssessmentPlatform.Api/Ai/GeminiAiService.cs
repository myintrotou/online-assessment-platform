using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssessmentPlatform.Api.Common;
using AssessmentPlatform.Api.Domain;
using Microsoft.Extensions.Options;

namespace AssessmentPlatform.Api.Ai;

/// <summary>
/// Talks to Google's Generative Language REST API. Handles structured (JSON-schema) output for
/// question generation, plain-text output for explanations, and retries transient failures.
/// </summary>
public class GeminiAiService(HttpClient http, IOptions<AiOptions> options, ILogger<GeminiAiService> logger) : IAiService
{
    private readonly AiOptions _options = options.Value;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<GeneratedQuestion>> GenerateQuestionsAsync(
        string subject, string topic, int count, string difficulty, CancellationToken ct)
    {
        count = Math.Clamp(count, 1, 20);

        var prompt = $"""
            You are an exam author. Write {count} multiple-choice questions.
            Subject: {subject}
            Topic focus: {topic}
            Difficulty: {difficulty}

            Rules:
            - Exactly 4 options per question.
            - Exactly one option is correct.
            - "correctOptionIndex" is the 0-based index of the correct option.
            - "explanation" is 1-3 sentences on why that answer is correct.
            - "topic" is a short sub-topic label (2-4 words).
            - Do not use "all of the above" or "none of the above".
            """;

        var schema = new
        {
            type = "ARRAY",
            items = new
            {
                type = "OBJECT",
                properties = new
                {
                    text = new { type = "STRING" },
                    options = new { type = "ARRAY", items = new { type = "STRING" } },
                    correctOptionIndex = new { type = "INTEGER" },
                    explanation = new { type = "STRING" },
                    topic = new { type = "STRING" }
                },
                required = new[] { "text", "options", "correctOptionIndex", "explanation", "topic" }
            }
        };

        var raw = await CallAsync(prompt, schema, temperature: 0.8, ct);

        var parsed = TryDeserialize<List<GeneratedQuestion>>(raw)
            ?? throw AppException.Unavailable("The AI returned a response we could not read. Please try again.");

        var valid = parsed
            .Where(q => !string.IsNullOrWhiteSpace(q.Text)
                        && q.Options is { Count: >= 2 }
                        && q.CorrectOptionIndex >= 0
                        && q.CorrectOptionIndex < q.Options.Count)
            .ToList();

        if (valid.Count == 0)
            throw AppException.Unavailable("The AI could not produce usable questions. Try a more specific topic.");

        return valid;
    }

    public async Task<string> ExplainQuestionAsync(
        string questionText, IReadOnlyList<string> options, int correctIndex, HelpKind kind, CancellationToken ct)
    {
        var answer = correctIndex >= 0 && correctIndex < options.Count ? options[correctIndex] : "(unknown)";
        var optionList = string.Join("\n", options.Select((o, i) => $"{(char)('A' + i)}. {o}"));

        var instruction = kind switch
        {
            HelpKind.Solution => "Give a clear step-by-step solution that leads to the correct answer.",
            HelpKind.Theory => "Explain the underlying concept this question tests, as if teaching it for the first time.",
            HelpKind.Guidance => "Give study guidance: what to revise and how to approach questions like this one.",
            _ => "Explain the correct answer."
        };

        var prompt = $"""
            Question: {questionText}
            Options:
            {optionList}
            Correct answer: {answer}

            {instruction}
            Keep it under 180 words, plain language, and do not restate the question.
            """;

        return await CallAsync(prompt, schema: null, temperature: 0.4, ct);
    }

    public async Task<string> BuildStudyGuidanceAsync(
        string subject, IReadOnlyList<TopicResult> results, CancellationToken ct)
    {
        var byTopic = results
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Topic) ? "General" : r.Topic)
            .Select(g => $"- {g.Key}: {g.Count(x => x.Correct)}/{g.Count()} correct")
            .ToList();

        var prompt = $"""
            A student just finished a {subject} quiz. Results by topic:
            {string.Join("\n", byTopic)}

            Write a short, encouraging study plan (under 200 words):
            1. What they seem to understand well.
            2. The 2-3 topics to prioritise revising.
            3. One concrete next step for each weak topic.
            """;

        return await CallAsync(prompt, schema: null, temperature: 0.5, ct);
    }

    private async Task<string> CallAsync(string prompt, object? schema, double temperature, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw AppException.Unavailable("AI features are not configured. Add a Gemini API key to enable them.");

        object generationConfig = schema is null
            ? new { temperature }
            : new { temperature, responseMimeType = "application/json", responseSchema = schema };

        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig
        };

        var url = $"/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var response = await http.PostAsJsonAsync(url, body, Json, ct);

                if (IsTransient(response.StatusCode))
                {
                    if (attempt > _options.MaxRetries)
                        throw AppException.Unavailable("The AI service is busy. Please try again in a moment.");
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), ct);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    logger.LogWarning("Gemini call failed ({Status}): {Body}", (int)response.StatusCode, Truncate(errorBody, 500));
                    throw AppException.Unavailable("The AI request was rejected. Check the API key and model name.");
                }

                var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(Json, ct);
                // Thinking models can emit several parts (reasoning traces, signatures); take the first real text.
                var text = payload?.Candidates?
                    .FirstOrDefault()?.Content?.Parts?
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text))?.Text;

                if (string.IsNullOrWhiteSpace(text))
                    throw AppException.Unavailable("The AI returned an empty response. Please try again.");

                return text.Trim();
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                if (attempt > _options.MaxRetries)
                    throw AppException.Unavailable("The AI service timed out. Please try again.");
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), ct);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Network error calling Gemini");
                if (attempt > _options.MaxRetries)
                    throw AppException.Unavailable("Could not reach the AI service. Please try again.");
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), ct);
            }
        }
    }

    private static bool IsTransient(HttpStatusCode code) => code
        is HttpStatusCode.TooManyRequests
        or HttpStatusCode.InternalServerError
        or HttpStatusCode.BadGateway
        or HttpStatusCode.ServiceUnavailable
        or HttpStatusCode.GatewayTimeout;

    private static T? TryDeserialize<T>(string raw)
    {
        try { return JsonSerializer.Deserialize<T>(raw, Json); }
        catch (JsonException) { return default; }
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")] public List<Candidate>? Candidates { get; set; }

        public sealed class Candidate
        {
            [JsonPropertyName("content")] public ContentBody? Content { get; set; }
        }

        public sealed class ContentBody
        {
            [JsonPropertyName("parts")] public List<Part>? Parts { get; set; }
        }

        public sealed class Part
        {
            [JsonPropertyName("text")] public string? Text { get; set; }
        }
    }
}
