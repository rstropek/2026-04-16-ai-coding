namespace DataAccess;

// --- Request DTOs ---

public record CreateQuestionDto(string Text, QuestionType Type, bool IsRequired);

public record CreateQuestionnaireDto(string Title, List<CreateQuestionDto> Questions);

public record UpdateQuestionnaireDto(string Title, List<CreateQuestionDto> Questions);

public record SubmitAnswerDto(Guid QuestionId, string Value);

public record SubmitAnswersDto(List<SubmitAnswerDto> Answers);

// --- Response DTOs ---

public record QuestionDto(Guid Id, string Text, QuestionType Type, bool IsRequired);

public record QuestionnaireDto(
    Guid Id,
    string Title,
    List<QuestionDto> Questions,
    int Version,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record AnswerDto(Guid QuestionId, string Value);

public record AnswerSubmissionDto(
    Guid Id,
    Guid QuestionnaireId,
    int Version,
    List<AnswerDto> Answers,
    DateTime SubmittedAt);
