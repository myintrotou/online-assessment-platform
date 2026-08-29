using AssessmentPlatform.Api.Ai;
using AssessmentPlatform.Api.Common;
using AssessmentPlatform.Api.Data;
using AssessmentPlatform.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssessmentPlatform.Api.Features.Attempts;

public class AttemptService(AppDbContext db, ICurrentUser currentUser, IAiService ai)
{
    /// <summary>Start a new attempt, or resume the student's existing unsubmitted one for this quiz.</summary>
    public async Task<ActiveAttemptView> StartAsync(Guid quizId, CancellationToken ct)
    {
        var studentId = currentUser.Id;

        var quiz = await db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct)
            ?? throw AppException.NotFound("Quiz");

        if (!quiz.IsPublished)
            throw AppException.Forbidden("This quiz is not open for attempts.");
        if (quiz.Questions.Count == 0)
            throw AppException.Conflict("This quiz has no questions yet.");

        var attempt = await db.Attempts
            .FirstOrDefaultAsync(a => a.QuizId == quizId && a.StudentId == studentId && a.SubmittedAt == null, ct);

        if (attempt is null)
        {
            attempt = new Attempt
            {
                QuizId = quizId,
                StudentId = studentId,
                TotalQuestions = quiz.Questions.Count,
                StartedAt = DateTime.UtcNow
            };
            db.Attempts.Add(attempt);
            await db.SaveChangesAsync(ct);
        }

        return new ActiveAttemptView(
            attempt.Id, quiz.Id, quiz.Title, quiz.Subject, quiz.TimeLimitMinutes, attempt.StartedAt,
            quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new AttemptQuestionView(q.Id, q.Text, q.Options, q.OrderIndex))
                .ToList());
    }

    public async Task<AttemptResultView> SubmitAsync(Guid attemptId, SubmitAttemptRequest request, CancellationToken ct)
    {
        var attempt = await db.Attempts
            .Include(a => a.Quiz).ThenInclude(q => q.Questions)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw AppException.NotFound("Attempt");

        if (attempt.StudentId != currentUser.Id)
            throw AppException.Forbidden();
        if (attempt.IsSubmitted)
            throw AppException.Conflict("This attempt has already been submitted.");

        var picks = request.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().SelectedOptionIndex);

        var questions = attempt.Quiz.Questions.OrderBy(q => q.OrderIndex).ToList();
        var correct = 0;

        foreach (var question in questions)
        {
            picks.TryGetValue(question.Id, out var selected);
            var isCorrect = selected.HasValue && selected.Value == question.CorrectOptionIndex;
            if (isCorrect) correct++;

            attempt.Answers.Add(new AttemptAnswer
            {
                QuestionId = question.Id,
                SelectedOptionIndex = selected,
                IsCorrect = isCorrect
            });
        }

        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.CorrectCount = correct;
        attempt.TotalQuestions = questions.Count;
        attempt.ScorePercent = questions.Count == 0 ? 0 : Math.Round(correct * 100.0 / questions.Count, 1);
        attempt.DurationSeconds = (int)Math.Max(0, (attempt.SubmittedAt.Value - attempt.StartedAt).TotalSeconds);

        await db.SaveChangesAsync(ct);
        return await BuildResultAsync(attempt.Id, ct);
    }

    public Task<AttemptResultView> GetResultAsync(Guid attemptId, CancellationToken ct)
        => BuildResultAsync(attemptId, ct);

    public async Task<List<AttemptHistoryItem>> GetHistoryAsync(CancellationToken ct)
    {
        var studentId = currentUser.Id;

        return await db.Attempts
            .Where(a => a.StudentId == studentId && a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new AttemptHistoryItem(
                a.Id, a.QuizId, a.Quiz.Title, a.Quiz.Subject,
                a.ScorePercent, a.CorrectCount, a.TotalQuestions, a.SubmittedAt))
            .ToListAsync(ct);
    }

    /// <summary>Get (and cache) an AI solution / theory explainer / study tip for one question.</summary>
    public async Task<QuestionHelpView> GetQuestionHelpAsync(Guid attemptId, Guid questionId, HelpKind kind, CancellationToken ct)
    {
        var attempt = await LoadOwnedSubmittedAttemptAsync(attemptId, ct);

        var question = await db.Questions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.QuizId == attempt.QuizId, ct)
            ?? throw AppException.NotFound("Question");

        var cached = await db.QuestionHelpEntries
            .FirstOrDefaultAsync(h => h.QuestionId == questionId && h.Kind == kind, ct);
        if (cached is not null)
            return new QuestionHelpView(kind, cached.Content, WasCached: true);

        var content = await ai.ExplainQuestionAsync(
            question.Text, question.Options, question.CorrectOptionIndex, kind, ct);

        db.QuestionHelpEntries.Add(new QuestionHelp { QuestionId = questionId, Kind = kind, Content = content });
        await SaveIgnoringRaceAsync(ct);

        return new QuestionHelpView(kind, content, WasCached: false);
    }

    /// <summary>Get (and cache) an AI study plan for a finished attempt, based on per-topic performance.</summary>
    public async Task<string> GetStudyGuidanceAsync(Guid attemptId, CancellationToken ct)
    {
        var attempt = await db.Attempts
            .Include(a => a.Quiz)
            .Include(a => a.Review)
            .Include(a => a.Answers).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw AppException.NotFound("Attempt");

        if (attempt.StudentId != currentUser.Id)
            throw AppException.Forbidden();
        if (!attempt.IsSubmitted)
            throw AppException.Conflict("Submit the attempt before asking for study guidance.");

        if (attempt.Review is not null)
            return attempt.Review.Content;

        var results = attempt.Answers
            .Select(a => new TopicResult(a.Question.Topic ?? "General", a.IsCorrect))
            .ToList();

        var content = await ai.BuildStudyGuidanceAsync(attempt.Quiz.Subject, results, ct);

        db.AttemptReviews.Add(new AttemptReview { AttemptId = attemptId, Content = content });
        await SaveIgnoringRaceAsync(ct);

        return content;
    }

    // ---------------- helpers ----------------

    private async Task<AttemptResultView> BuildResultAsync(Guid attemptId, CancellationToken ct)
    {
        var attempt = await db.Attempts
            .Include(a => a.Quiz).ThenInclude(q => q.Questions)
            .Include(a => a.Answers)
            .Include(a => a.Review)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw AppException.NotFound("Attempt");

        if (attempt.StudentId != currentUser.Id)
            throw AppException.Forbidden();
        if (!attempt.IsSubmitted)
            throw AppException.Conflict("This attempt has not been submitted yet.");

        var answersByQuestion = attempt.Answers.ToDictionary(a => a.QuestionId);

        var questions = attempt.Quiz.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(q =>
            {
                answersByQuestion.TryGetValue(q.Id, out var answer);
                return new ReviewedQuestion(
                    q.Id, q.Text, q.Options, q.CorrectOptionIndex,
                    answer?.SelectedOptionIndex, answer?.IsCorrect ?? false,
                    q.Explanation, q.Topic);
            })
            .ToList();

        return new AttemptResultView(
            attempt.Id, attempt.QuizId, attempt.Quiz.Title, attempt.Quiz.Subject,
            attempt.ScorePercent, attempt.CorrectCount, attempt.TotalQuestions,
            attempt.StartedAt, attempt.SubmittedAt, attempt.DurationSeconds,
            questions, attempt.Review?.Content);
    }

    private async Task<Attempt> LoadOwnedSubmittedAttemptAsync(Guid attemptId, CancellationToken ct)
    {
        var attempt = await db.Attempts.FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw AppException.NotFound("Attempt");

        if (attempt.StudentId != currentUser.Id)
            throw AppException.Forbidden();
        if (!attempt.IsSubmitted)
            throw AppException.Conflict("Submit the attempt first.");

        return attempt;
    }

    private async Task SaveIgnoringRaceAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A concurrent request cached the same help/review first - the unique index rejected ours. That's fine.
        }
    }
}
