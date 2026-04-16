using DataAccess;

namespace DataAccessTests;

public class QuestionnaireRepositoryTests
{
    private readonly QuestionnaireRepository _repo = new();

    private static CreateQuestionnaireDto SampleDto(int questionCount = 2) =>
        new("Test Questionnaire",
            [.. Enumerable.Range(1, questionCount)
                .Select(i => new CreateQuestionDto($"Question {i}", i % 2 == 0 ? QuestionType.Boolean : QuestionType.Text, i <= questionCount / 2 + 1))]);

    // === Create ===

    [Fact]
    public void CreateQuestionnaire_ReturnsWithIdAndVersion1()
    {
        var result = _repo.CreateQuestionnaire(SampleDto());

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(1, result.Version);
        Assert.Equal("Test Questionnaire", result.Title);
        Assert.Equal(2, result.Questions.Count);
    }

    [Fact]
    public void CreateQuestionnaire_GeneratesUniqueQuestionIds()
    {
        var result = _repo.CreateQuestionnaire(SampleDto(3));

        var ids = result.Questions.Select(q => q.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, id => Assert.NotEqual(Guid.Empty, id));
    }

    // === Get ===

    [Fact]
    public void GetQuestionnaire_ReturnsLatestVersion()
    {
        var created = _repo.CreateQuestionnaire(SampleDto());
        _repo.UpdateQuestionnaire(created.Id, new UpdateQuestionnaireDto("Updated", [new("New Q", QuestionType.Text, true)]));

        var result = _repo.GetQuestionnaire(created.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.Equal("Updated", result.Title);
    }

    [Fact]
    public void GetQuestionnaire_ReturnsNullForNonExistent()
    {
        var result = _repo.GetQuestionnaire(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetQuestionnaire_ReturnsNullForDeleted()
    {
        var created = _repo.CreateQuestionnaire(SampleDto());
        _repo.DeleteQuestionnaire(created.Id);

        var result = _repo.GetQuestionnaire(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public void GetQuestionnaireByVersion_ReturnsSpecificVersion()
    {
        var created = _repo.CreateQuestionnaire(SampleDto());
        _repo.UpdateQuestionnaire(created.Id, new UpdateQuestionnaireDto("V2", [new("V2 Q", QuestionType.Boolean, false)]));

        var v1 = _repo.GetQuestionnaire(created.Id, 1);
        var v2 = _repo.GetQuestionnaire(created.Id, 2);

        Assert.NotNull(v1);
        Assert.NotNull(v2);
        Assert.Equal("Test Questionnaire", v1.Title);
        Assert.Equal("V2", v2.Title);
    }

    [Fact]
    public void GetQuestionnaireByVersion_ReturnsNullForNonExistentVersion()
    {
        var created = _repo.CreateQuestionnaire(SampleDto());
        var result = _repo.GetQuestionnaire(created.Id, 99);
        Assert.Null(result);
    }

    // === List ===

    [Fact]
    public void ListQuestionnaires_ReturnsOnlyNonDeleted()
    {
        var q1 = _repo.CreateQuestionnaire(new CreateQuestionnaireDto("Keep", [new("Q", QuestionType.Text, false)]));
        var q2 = _repo.CreateQuestionnaire(new CreateQuestionnaireDto("Delete", [new("Q", QuestionType.Text, false)]));
        _repo.DeleteQuestionnaire(q2.Id);

        var result = _repo.ListQuestionnaires();

        Assert.Contains(result, q => q.Id == q1.Id);
        Assert.DoesNotContain(result, q => q.Id == q2.Id);
    }

    [Fact]
    public void ListQuestionnaires_ReturnsEmptyWhenNoneExist()
    {
        var result = _repo.ListQuestionnaires();
        Assert.Empty(result);
    }

    // === Update ===

    [Fact]
    public void UpdateQuestionnaire_IncrementsVersion()
    {
        var created = _repo.CreateQuestionnaire(SampleDto());
        var updated = _repo.UpdateQuestionnaire(created.Id, new UpdateQuestionnaireDto("V2", [new("Q", QuestionType.Text, true)]));

        Assert.NotNull(updated);
        Assert.Equal(2, updated.Version);
        Assert.Equal("V2", updated.Title);
    }

    [Fact]
    public void UpdateQuestionnaire_PreservesOldVersion()
    {
        var created = _repo.CreateQuestionnaire(SampleDto());
        _repo.UpdateQuestionnaire(created.Id, new UpdateQuestionnaireDto("V2", [new("New Q", QuestionType.Text, true)]));

        var v1 = _repo.GetQuestionnaire(created.Id, 1);
        Assert.NotNull(v1);
        Assert.Equal(1, v1.Version);
        Assert.Equal("Test Questionnaire", v1.Title);
    }

    [Fact]
    public void UpdateQuestionnaire_ReturnsNullForNonExistent()
    {
        var result = _repo.UpdateQuestionnaire(Guid.NewGuid(), new UpdateQuestionnaireDto("X", [new("Q", QuestionType.Text, true)]));
        Assert.Null(result);
    }

    [Fact]
    public void UpdateQuestionnaire_ReturnsNullForDeleted()
    {
        var created = _repo.CreateQuestionnaire(SampleDto());
        _repo.DeleteQuestionnaire(created.Id);

        var result = _repo.UpdateQuestionnaire(created.Id, new UpdateQuestionnaireDto("X", [new("Q", QuestionType.Text, true)]));
        Assert.Null(result);
    }

    // === Delete ===

    [Fact]
    public void DeleteQuestionnaire_ReturnsTrueAndSoftDeletes()
    {
        var created = _repo.CreateQuestionnaire(SampleDto());
        var deleted = _repo.DeleteQuestionnaire(created.Id);

        Assert.True(deleted);
        Assert.Null(_repo.GetQuestionnaire(created.Id));
    }

    [Fact]
    public void DeleteQuestionnaire_ReturnsFalseForNonExistent()
    {
        Assert.False(_repo.DeleteQuestionnaire(Guid.NewGuid()));
    }

    [Fact]
    public void DeleteQuestionnaire_ReturnsFalseForAlreadyDeleted()
    {
        var created = _repo.CreateQuestionnaire(SampleDto());
        _repo.DeleteQuestionnaire(created.Id);

        Assert.False(_repo.DeleteQuestionnaire(created.Id));
    }

    // === Submit Answers ===

    [Fact]
    public void SubmitAnswers_ReturnsSubmissionWithId()
    {
        var q = _repo.CreateQuestionnaire(new CreateQuestionnaireDto("Q", [new("Q1", QuestionType.Text, true)]));
        var result = _repo.SubmitAnswers(q.Id, 1, new SubmitAnswersDto([new(q.Questions[0].Id, "answer")]));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(q.Id, result.QuestionnaireId);
        Assert.Equal(1, result.Version);
        Assert.Single(result.Answers);
    }

    [Fact]
    public void SubmitAnswers_AcceptsOptionalQuestionsOmitted()
    {
        var q = _repo.CreateQuestionnaire(new CreateQuestionnaireDto("Q",
        [
            new("Required", QuestionType.Text, true),
            new("Optional", QuestionType.Text, false)
        ]));

        var result = _repo.SubmitAnswers(q.Id, 1, new SubmitAnswersDto([new(q.Questions[0].Id, "answer")]));
        Assert.Single(result.Answers);
    }

    [Fact]
    public void SubmitAnswers_ThrowsForNonExistentQuestionnaire()
    {
        Assert.Throws<ValidationException>(() =>
            _repo.SubmitAnswers(Guid.NewGuid(), 1, new SubmitAnswersDto([])));
    }

    [Fact]
    public void SubmitAnswers_ThrowsForNonExistentVersion()
    {
        var q = _repo.CreateQuestionnaire(SampleDto());
        Assert.Throws<ValidationException>(() =>
            _repo.SubmitAnswers(q.Id, 99, new SubmitAnswersDto([])));
    }

    [Fact]
    public void SubmitAnswers_ThrowsForMissingRequiredAnswer()
    {
        var q = _repo.CreateQuestionnaire(new CreateQuestionnaireDto("Q",
        [
            new("Required", QuestionType.Text, true),
            new("Optional", QuestionType.Text, false)
        ]));

        var ex = Assert.Throws<ValidationException>(() =>
            _repo.SubmitAnswers(q.Id, 1, new SubmitAnswersDto([])));
        Assert.Contains("Required", ex.Message);
    }

    [Fact]
    public void SubmitAnswers_ThrowsForUnknownQuestionId()
    {
        var q = _repo.CreateQuestionnaire(new CreateQuestionnaireDto("Q", [new("Q1", QuestionType.Text, false)]));

        Assert.Throws<ValidationException>(() =>
            _repo.SubmitAnswers(q.Id, 1, new SubmitAnswersDto([new(Guid.NewGuid(), "value")])));
    }

    [Fact]
    public void SubmitAnswers_ThrowsForDuplicateQuestionIds()
    {
        var q = _repo.CreateQuestionnaire(new CreateQuestionnaireDto("Q", [new("Q1", QuestionType.Text, false)]));
        var qId = q.Questions[0].Id;

        Assert.Throws<ValidationException>(() =>
            _repo.SubmitAnswers(q.Id, 1, new SubmitAnswersDto([new(qId, "a"), new(qId, "b")])));
    }

    // === List Answers ===

    [Fact]
    public void ListAnswers_ReturnsAllSubmissionsForQuestionnaire()
    {
        var q = _repo.CreateQuestionnaire(new CreateQuestionnaireDto("Q", [new("Q1", QuestionType.Text, false)]));
        _repo.SubmitAnswers(q.Id, 1, new SubmitAnswersDto([new(q.Questions[0].Id, "a1")]));
        _repo.SubmitAnswers(q.Id, 1, new SubmitAnswersDto([new(q.Questions[0].Id, "a2")]));

        var result = _repo.ListAnswers(q.Id);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ListAnswers_ReturnsEmptyForNoSubmissions()
    {
        var result = _repo.ListAnswers(Guid.NewGuid());
        Assert.Empty(result);
    }
}
