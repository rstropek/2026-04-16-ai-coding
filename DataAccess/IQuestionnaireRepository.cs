namespace DataAccess;

public interface IQuestionnaireRepository
{
    QuestionnaireDto CreateQuestionnaire(CreateQuestionnaireDto dto);
    QuestionnaireDto? GetQuestionnaire(Guid id);
    QuestionnaireDto? GetQuestionnaire(Guid id, int version);
    List<QuestionnaireDto> ListQuestionnaires();
    QuestionnaireDto? UpdateQuestionnaire(Guid id, UpdateQuestionnaireDto dto);
    bool DeleteQuestionnaire(Guid id);
    AnswerSubmissionDto SubmitAnswers(Guid questionnaireId, int version, SubmitAnswersDto dto);
    List<AnswerSubmissionDto> ListAnswers(Guid questionnaireId);
}
