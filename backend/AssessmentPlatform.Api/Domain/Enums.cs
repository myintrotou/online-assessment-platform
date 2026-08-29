namespace AssessmentPlatform.Api.Domain;

/// <summary>Determines what a signed-in user is allowed to do.</summary>
public enum UserRole
{
    Student = 0,
    Instructor = 1
}

/// <summary>Where a question came from - typed by hand or produced by the AI helper.</summary>
public enum QuestionSource
{
    Manual = 0,
    AiGenerated = 1
}

/// <summary>The three kinds of on-demand AI help a student can ask for on a question.</summary>
public enum HelpKind
{
    /// <summary>Step-by-step working that leads to the correct answer.</summary>
    Solution = 0,
    /// <summary>The underlying concept / theory the question is testing.</summary>
    Theory = 1,
    /// <summary>Study guidance - what to revise and how to approach similar questions.</summary>
    Guidance = 2
}
