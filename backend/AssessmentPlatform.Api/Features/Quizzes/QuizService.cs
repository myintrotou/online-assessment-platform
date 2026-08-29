using AssessmentPlatform.Api.Ai;
using AssessmentPlatform.Api.Common;
using AssessmentPlatform.Api.Data;
using AssessmentPlatform.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssessmentPlatform.Api.Features.Quizzes;

public class QuizService(AppDbContext db, ICurrentUser currentUser, IAiService ai)
{
    // ---------------- Instructor ----------------

    public async Task<QuizManageDetail> CreateAsync(CreateQuizRequest request, CancellationToken ct)
    {
        RequireInstructor();

        var quiz = new Quiz
        {
            Title = request.Title.Trim(),
            Subject = request.Subject.Trim(),
            Description = request.Description?.Trim(),
            TimeLimitMinutes = request.TimeLimitMinutes,
            CreatedById = currentUser.Id
        };

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync(ct);
        return ToManageDetail(quiz);
    }

    public async Task<List<QuizListItem>> ListMineAsync(CancellationToken ct)
    {
        RequireInstructor();

        return await db.Quizzes
            .Where(q => q.CreatedById == currentUser.Id)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuizListItem(
                q.Id, q.Title, q.Subject, q.IsPublished,
                q.Questions.Count,
                q.Attempts.Count(a => a.SubmittedAt != null),
                q.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<QuizManageDetail> GetForManageAsync(Guid id, CancellationToken ct)
        => ToManageDetail(await LoadOwnedQuizAsync(id, includeQuestions: true, ct));

    public async Task<IReadOnlyList<GeneratedQuestionDto>> GenerateAsync(
        Guid id, GenerateQuestionsRequest request, CancellationToken ct)
    {
        var quiz = await LoadOwnedQuizAsync(id, includeQuestions: false, ct);

        var drafts = await ai.GenerateQuestionsAsync(
            quiz.Subject, request.Topic.Trim(), request.Count, request.Difficulty, ct);

        return drafts
            .Select(d => new GeneratedQuestionDto(d.Text, d.Options, d.CorrectOptionIndex, d.Explanation, d.Topic))
            .ToList();
    }

    public async Task<QuizManageDetail> AddQuestionsAsync(Guid id, SaveQuestionsRequest request, CancellationToken ct)
    {
        var quiz = await LoadOwnedQuizAsync(id, includeQuestions: true, ct);
        if (quiz.IsPublished)
            throw AppException.Conflict("Unpublish the quiz before changing its questions.");

        var nextOrder = quiz.Questions.Count == 0 ? 0 : quiz.Questions.Max(q => q.OrderIndex) + 1;

        foreach (var input in request.Questions)
        {
            ValidateQuestion(input);
            quiz.Questions.Add(new Question
            {
                Text = input.Text.Trim(),
                Options = input.Options.Select(o => o.Trim()).ToList(),
                CorrectOptionIndex = input.CorrectOptionIndex,
                Explanation = string.IsNullOrWhiteSpace(input.Explanation) ? null : input.Explanation.Trim(),
                Topic = string.IsNullOrWhiteSpace(input.Topic) ? null : input.Topic.Trim(),
                Source = input.Source,
                OrderIndex = nextOrder++
            });
        }

        await db.SaveChangesAsync(ct);
        return ToManageDetail(quiz);
    }

    public async Task DeleteQuestionAsync(Guid id, Guid questionId, CancellationToken ct)
    {
        var quiz = await LoadOwnedQuizAsync(id, includeQuestions: true, ct);
        if (quiz.IsPublished)
            throw AppException.Conflict("Unpublish the quiz before changing its questions.");

        var question = quiz.Questions.FirstOrDefault(q => q.Id == questionId)
            ?? throw AppException.NotFound("Question");

        db.Questions.Remove(question);
        await db.SaveChangesAsync(ct);
    }

    public async Task<QuizManageDetail> SetPublishedAsync(Guid id, bool publish, CancellationToken ct)
    {
        var quiz = await LoadOwnedQuizAsync(id, includeQuestions: true, ct);

        if (publish && quiz.Questions.Count == 0)
            throw AppException.Conflict("Add at least one question before publishing.");

        quiz.IsPublished = publish;
        quiz.PublishedAt = publish ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return ToManageDetail(quiz);
    }

    public async Task<QuizResultsSummary> GetResultsAsync(Guid id, CancellationToken ct)
    {
        var quiz = await LoadOwnedQuizAsync(id, includeQuestions: true, ct);

        var attempts = await db.Attempts
            .Where(a => a.QuizId == id && a.SubmittedAt != null)
            .Include(a => a.Student)
            .Include(a => a.Answers)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(ct);

        var allAnswers = attempts.SelectMany(a => a.Answers).ToList();

        var breakdown = quiz.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(q =>
            {
                var forQuestion = allAnswers.Where(x => x.QuestionId == q.Id).ToList();
                var answered = forQuestion.Count;
                var correct = forQuestion.Count(x => x.IsCorrect);
                return new QuestionAccuracy(q.Id, q.Text, q.Topic, answered, correct,
                    answered == 0 ? 0 : Math.Round(correct * 100.0 / answered, 1));
            })
            .ToList();

        var rows = attempts
            .Select(a => new AttemptRow(a.Id, a.Student.FullName, a.ScorePercent,
                a.CorrectCount, a.TotalQuestions, a.SubmittedAt))
            .ToList();

        var average = attempts.Count == 0 ? 0 : Math.Round(attempts.Average(a => a.ScorePercent), 1);

        return new QuizResultsSummary(quiz.Id, quiz.Title, attempts.Count, average, breakdown, rows);
    }

    // ---------------- Student ----------------

    public async Task<List<PublishedQuizItem>> ListPublishedAsync(CancellationToken ct)
    {
        var studentId = currentUser.Id;

        var quizzes = await db.Quizzes
            .Where(q => q.IsPublished)
            .OrderByDescending(q => q.PublishedAt)
            .Select(q => new
            {
                q.Id, q.Title, q.Subject, q.Description, q.TimeLimitMinutes,
                QuestionCount = q.Questions.Count,
                MyAttempts = q.Attempts.Where(a => a.StudentId == studentId && a.SubmittedAt != null).ToList()
            })
            .ToListAsync(ct);

        return quizzes.Select(q => new PublishedQuizItem(
                q.Id, q.Title, q.Subject, q.Description, q.TimeLimitMinutes, q.QuestionCount,
                q.MyAttempts.Count,
                q.MyAttempts.Count == 0 ? null : q.MyAttempts.Max(a => a.ScorePercent)))
            .ToList();
    }

    // ---------------- helpers ----------------

    private void RequireInstructor()
    {
        if (!currentUser.IsInstructor)
            throw AppException.Forbidden("Only instructors can manage quizzes.");
    }

    private async Task<Quiz> LoadOwnedQuizAsync(Guid id, bool includeQuestions, CancellationToken ct)
    {
        RequireInstructor();

        IQueryable<Quiz> query = db.Quizzes;
        if (includeQuestions)
            query = query.Include(q => q.Questions);

        var quiz = await query.FirstOrDefaultAsync(q => q.Id == id, ct)
            ?? throw AppException.NotFound("Quiz");

        if (quiz.CreatedById != currentUser.Id)
            throw AppException.Forbidden("This quiz belongs to another instructor.");

        return quiz;
    }

    private static void ValidateQuestion(QuestionInput input)
    {
        if (input.Options.Count < 2)
            throw new AppException("Each question needs at least two options.");
        if (input.CorrectOptionIndex < 0 || input.CorrectOptionIndex >= input.Options.Count)
            throw new AppException("correctOptionIndex is outside the range of options.");
        if (input.Options.Any(string.IsNullOrWhiteSpace))
            throw new AppException("Options cannot be blank.");
    }

    private static QuizManageDetail ToManageDetail(Quiz quiz) => new(
        quiz.Id, quiz.Title, quiz.Subject, quiz.Description, quiz.TimeLimitMinutes,
        quiz.IsPublished, quiz.CreatedAt, quiz.PublishedAt,
        quiz.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(q => new ManageQuestion(q.Id, q.Text, q.Options, q.CorrectOptionIndex,
                q.Explanation, q.Topic, q.Source, q.OrderIndex))
            .ToList());
}
