namespace DataAccess;

public enum QuestionType
{
    Text,
    Boolean
}

public record Question(Guid Id, string Text, QuestionType Type, bool IsRequired);

public record Questionnaire
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required List<Question> Questions { get; init; }
    public required int Version { get; init; }
    public bool IsDeleted { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

public record Answer(Guid QuestionId, string Value);

public record AnswerSubmission
{
    public required Guid Id { get; init; }
    public required Guid QuestionnaireId { get; init; }
    public required int Version { get; init; }
    public required List<Answer> Answers { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
