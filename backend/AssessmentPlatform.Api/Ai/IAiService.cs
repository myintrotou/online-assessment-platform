using AssessmentPlatform.Api.Domain;

namespace AssessmentPlatform.Api.Ai;

/// <summary>One AI-authored multiple-choice question, before it is saved to a quiz.</summary>
public record GeneratedQuestion(
    string Text,
    List<string> Options,
    int CorrectOptionIndex,
    string Explanation,
    string Topic);

/// <summary>Whether a student got a given topic right, used to build study guidance.</summary>
public record TopicResult(string Topic, bool Correct);

/// <summary>
/// The provider-agnostic AI contract. The rest of the app depends only on this;
/// swapping Gemini for another model is one line in Program.cs.
/// </summary>
public interface IAiService
{
    /// <summary>Draft a set of MCQs on a topic for an instructor to review before saving.</summary>
    Task<IReadOnlyList<GeneratedQuestion>> GenerateQuestionsAsync(
        string subject, string topic, int count, string difficulty, CancellationToken ct);

    /// <summary>Produce a solution, theory explainer, or study tip for one question, on demand.</summary>
    Task<string> ExplainQuestionAsync(
        string questionText, IReadOnlyList<string> options, int correctIndex, HelpKind kind, CancellationToken ct);

    /// <summary>Write a personalised study plan from a finished attempt's per-topic results.</summary>
    Task<string> BuildStudyGuidanceAsync(
        string subject, IReadOnlyList<TopicResult> results, CancellationToken ct);
}
