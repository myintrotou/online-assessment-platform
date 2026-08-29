using AssessmentPlatform.Api.Domain;

namespace AssessmentPlatform.Api.Ai;

/// <summary>
/// Offline stand-in selected when Ai:Provider = "Stub". Lets the whole app run and be demoed
/// with no API key. Returns obviously-fake content so it is never mistaken for the real thing.
/// </summary>
public class StubAiService : IAiService
{
    public Task<IReadOnlyList<GeneratedQuestion>> GenerateQuestionsAsync(
        string subject, string topic, int count, string difficulty, CancellationToken ct)
    {
        var list = Enumerable.Range(1, Math.Clamp(count, 1, 20))
            .Select(i => new GeneratedQuestion(
                $"[Sample] Question {i} on {topic} ({subject}, {difficulty}).",
                new List<string> { "Option A", "Option B", "Option C", "Option D" },
                i % 4,
                "Sample explanation from the offline stub provider.",
                topic))
            .ToList();

        return Task.FromResult<IReadOnlyList<GeneratedQuestion>>(list);
    }

    public Task<string> ExplainQuestionAsync(
        string questionText, IReadOnlyList<string> options, int correctIndex, HelpKind kind, CancellationToken ct)
        => Task.FromResult($"[Stub {kind}] Correct option index is {correctIndex}. Configure a Gemini API key for real help.");

    public Task<string> BuildStudyGuidanceAsync(
        string subject, IReadOnlyList<TopicResult> results, CancellationToken ct)
        => Task.FromResult("[Stub guidance] Revise the topics you missed and re-attempt. Configure a Gemini API key for a personalised plan.");
}
