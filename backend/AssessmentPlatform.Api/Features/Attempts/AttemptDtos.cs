using System.ComponentModel.DataAnnotations;
using AssessmentPlatform.Api.Domain;

namespace AssessmentPlatform.Api.Features.Attempts;

public record AttemptQuestionView(Guid QuestionId, string Text, List<string> Options, int OrderIndex);

public record ActiveAttemptView(
    Guid AttemptId, Guid QuizId, string QuizTitle, string Subject,
    int TimeLimitMinutes, DateTime StartedAt, List<AttemptQuestionView> Questions);

public record SubmitAnswerInput(Guid QuestionId, int? SelectedOptionIndex);

public record SubmitAttemptRequest(
    [Required] List<SubmitAnswerInput> Answers);

public record ReviewedQuestion(
    Guid QuestionId, string Text, List<string> Options,
    int CorrectOptionIndex, int? SelectedOptionIndex, bool IsCorrect,
    string? Explanation, string? Topic);

public record AttemptResultView(
    Guid AttemptId, Guid QuizId, string QuizTitle, string Subject,
    double ScorePercent, int CorrectCount, int TotalQuestions,
    DateTime StartedAt, DateTime? SubmittedAt, int DurationSeconds,
    List<ReviewedQuestion> Questions, string? StudyGuidance);

public record AttemptHistoryItem(
    Guid AttemptId, Guid QuizId, string QuizTitle, string Subject,
    double ScorePercent, int CorrectCount, int TotalQuestions, DateTime? SubmittedAt);

public record QuestionHelpView(HelpKind Kind, string Content, bool WasCached);
