using System.Collections.Concurrent;

namespace DataAccess;

public class QuestionnaireRepository : IQuestionnaireRepository
{
    private readonly ConcurrentDictionary<(Guid Id, int Version), Questionnaire> _questionnaires = new();
    private readonly ConcurrentDictionary<Guid, int> _latestVersions = new();
    private readonly ConcurrentDictionary<Guid, AnswerSubmission> _answers = new();
    private readonly Lock _lock = new();

    public QuestionnaireDto CreateQuestionnaire(CreateQuestionnaireDto dto)
    {
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();

        var questionnaire = new Questionnaire
        {
            Id = id,
            Title = dto.Title,
            Questions = [.. dto.Questions.Select(q => new Question(Guid.NewGuid(), q.Text, q.Type, q.IsRequired))],
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        lock (_lock)
        {
            _questionnaires[(id, 1)] = questionnaire;
            _latestVersions[id] = 1;
        }

        return ToDto(questionnaire);
    }

    public QuestionnaireDto? GetQuestionnaire(Guid id)
    {
        if (!_latestVersions.TryGetValue(id, out var version))
        {
            return null;
        }

        if (!_questionnaires.TryGetValue((id, version), out var questionnaire) || questionnaire.IsDeleted)
        {
            return null;
        }

        return ToDto(questionnaire);
    }

    public QuestionnaireDto? GetQuestionnaire(Guid id, int version)
    {
        return _questionnaires.TryGetValue((id, version), out var questionnaire)
            ? ToDto(questionnaire)
            : null;
    }

    public List<QuestionnaireDto> ListQuestionnaires(bool includeDeleted = false)
    {
        var result = new List<QuestionnaireDto>();

        foreach (var (id, version) in _latestVersions)
        {
            if (_questionnaires.TryGetValue((id, version), out var questionnaire)
                && (includeDeleted || !questionnaire.IsDeleted))
            {
                result.Add(ToDto(questionnaire));
            }
        }

        return result;
    }

    public QuestionnaireDto? UpdateQuestionnaire(Guid id, UpdateQuestionnaireDto dto)
    {
        lock (_lock)
        {
            if (!_latestVersions.TryGetValue(id, out var currentVersion))
            {
                return null;
            }

            if (!_questionnaires.TryGetValue((id, currentVersion), out var current) || current.IsDeleted)
            {
                return null;
            }

            var newVersion = currentVersion + 1;
            var questionnaire = new Questionnaire
            {
                Id = id,
                Title = dto.Title,
                Questions = [.. dto.Questions.Select(q => new Question(Guid.NewGuid(), q.Text, q.Type, q.IsRequired))],
                Version = newVersion,
                CreatedAt = current.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _questionnaires[(id, newVersion)] = questionnaire;
            _latestVersions[id] = newVersion;

            return ToDto(questionnaire);
        }
    }

    public bool DeleteQuestionnaire(Guid id)
    {
        lock (_lock)
        {
            if (!_latestVersions.TryGetValue(id, out var version))
            {
                return false;
            }

            if (!_questionnaires.TryGetValue((id, version), out var questionnaire) || questionnaire.IsDeleted)
            {
                return false;
            }

            _questionnaires[(id, version)] = questionnaire with { IsDeleted = true, UpdatedAt = DateTime.UtcNow };
            return true;
        }
    }

    public bool RestoreQuestionnaire(Guid id)
    {
        lock (_lock)
        {
            if (!_latestVersions.TryGetValue(id, out var version))
            {
                return false;
            }

            if (!_questionnaires.TryGetValue((id, version), out var questionnaire) || !questionnaire.IsDeleted)
            {
                return false;
            }

            _questionnaires[(id, version)] = questionnaire with { IsDeleted = false, UpdatedAt = DateTime.UtcNow };
            return true;
        }
    }

    public AnswerSubmissionDto SubmitAnswers(Guid questionnaireId, int version, SubmitAnswersDto dto)
    {
        if (!_questionnaires.TryGetValue((questionnaireId, version), out var questionnaire))
        {
            throw new ValidationException($"Questionnaire with ID '{questionnaireId}' version {version} not found.");
        }

        var questionIds = questionnaire.Questions.Select(q => q.Id).ToHashSet();
        var submittedIds = dto.Answers.Select(a => a.QuestionId).ToList();

        // Check for duplicate question IDs
        if (submittedIds.Count != submittedIds.Distinct().Count())
        {
            throw new ValidationException("Duplicate question IDs in submission.");
        }

        // Check for unknown question IDs
        var unknownIds = submittedIds.Where(id => !questionIds.Contains(id)).ToList();
        if (unknownIds.Count > 0)
        {
            throw new ValidationException($"Unknown question IDs: {string.Join(", ", unknownIds)}");
        }

        // Check all required questions are answered
        var answeredIds = submittedIds.ToHashSet();
        var missingRequired = questionnaire.Questions
            .Where(q => q.IsRequired && !answeredIds.Contains(q.Id))
            .ToList();
        if (missingRequired.Count > 0)
        {
            throw new ValidationException($"Missing required answers for questions: {string.Join(", ", missingRequired.Select(q => q.Text))}");
        }

        var submission = new AnswerSubmission
        {
            Id = Guid.NewGuid(),
            QuestionnaireId = questionnaireId,
            Version = version,
            Answers = [.. dto.Answers.Select(a => new Answer(a.QuestionId, a.Value))],
            SubmittedAt = DateTime.UtcNow
        };

        _answers[submission.Id] = submission;

        return ToSubmissionDto(submission);
    }

    public List<AnswerSubmissionDto> ListAnswers(Guid questionnaireId)
    {
        return [.. _answers.Values
            .Where(a => a.QuestionnaireId == questionnaireId)
            .Select(ToSubmissionDto)];
    }

    private static QuestionnaireDto ToDto(Questionnaire q) =>
        new(q.Id, q.Title, [.. q.Questions.Select(qu => new QuestionDto(qu.Id, qu.Text, qu.Type, qu.IsRequired))],
            q.Version, q.IsDeleted, q.CreatedAt, q.UpdatedAt);

    private static AnswerSubmissionDto ToSubmissionDto(AnswerSubmission a) =>
        new(a.Id, a.QuestionnaireId, a.Version,
            [.. a.Answers.Select(an => new AnswerDto(an.QuestionId, an.Value))], a.SubmittedAt);
}
