namespace AssessmentPlatform.Api.Domain;

public class Question
{
    public Guid Id { get; set; }

    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public string Text { get; set; } = string.Empty;

    /// <summary>The answer choices shown to the student, in display order.</summary>
    public List<string> Options { get; set; } = new();

    /// <summary>Zero-based index into <see cref="Options"/>.</summary>
    public int CorrectOptionIndex { get; set; }

    /// <summary>Short explanation of the answer. Filled in by the instructor or the AI generator.</summary>
    public string? Explanation { get; set; }

    /// <summary>Sub-topic label, e.g. "Recursion" - used to group results and drive study guidance.</summary>
    public string? Topic { get; set; }

    public QuestionSource Source { get; set; }
    public int OrderIndex { get; set; }

    public ICollection<QuestionHelp> HelpEntries { get; set; } = new List<QuestionHelp>();
}
