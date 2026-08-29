using System.ComponentModel.DataAnnotations;
using AssessmentPlatform.Api.Domain;

namespace AssessmentPlatform.Api.Features.Quizzes;

public record CreateQuizRequest(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(120)] string Subject,
    [MaxLength(2000)] string? Description,
    [Range(0, 240)] int TimeLimitMinutes);

public record QuestionInput(
    [Required, MaxLength(2000)] string Text,
    [Required, MinLength(2)] List<string> Options,
    int CorrectOptionIndex,
    [MaxLength(4000)] string? Explanation,
    [MaxLength(120)] string? Topic,
    QuestionSource Source = QuestionSource.Manual);

public record SaveQuestionsRequest(
    [Required, MinLength(1)] List<QuestionInput> Questions);

public record GenerateQuestionsRequest(
    [Required, MaxLength(200)] string Topic,
    [Range(1, 20)] int Count,
    string Difficulty = "medium");

public record GeneratedQuestionDto(
    string Text, List<string> Options, int CorrectOptionIndex, string Explanation, string Topic);

public record QuizListItem(
    Guid Id, string Title, string Subject, bool IsPublished,
    int QuestionCount, int AttemptCount, DateTime CreatedAt);

public record QuizManageDetail(
    Guid Id, string Title, string Subject, string? Description, int TimeLimitMinutes,
    bool IsPublished, DateTime CreatedAt, DateTime? PublishedAt, List<ManageQuestion> Questions);

public record ManageQuestion(
    Guid Id, string Text, List<string> Options, int CorrectOptionIndex,
    string? Explanation, string? Topic, QuestionSource Source, int OrderIndex);

public record PublishedQuizItem(
    Guid Id, string Title, string Subject, string? Description, int TimeLimitMinutes,
    int QuestionCount, int MyAttempts, double? MyBestScore);

public record QuizResultsSummary(
    Guid QuizId, string Title, int AttemptCount, double AverageScore,
    List<QuestionAccuracy> QuestionBreakdown, List<AttemptRow> Attempts);

public record QuestionAccuracy(
    Guid QuestionId, string Text, string? Topic, int Answered, int Correct, double AccuracyPercent);

public record AttemptRow(
    Guid AttemptId, string StudentName, double ScorePercent,
    int CorrectCount, int TotalQuestions, DateTime? SubmittedAt);
