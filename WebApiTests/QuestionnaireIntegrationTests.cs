using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataAccess;

namespace WebApiTests;

public class QuestionnaireIntegrationTests(WebApiTestFixture fixture) : IClassFixture<WebApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private HttpClient Client => fixture.HttpClient;

    [Fact]
    public async Task FullScenario_CreateUpdateSubmitAnswersListAnswers()
    {
        // 1. Create questionnaire "Customer Feedback Q2" with 4 questions
        var createDto = new CreateQuestionnaireDto("Customer Feedback Q2",
        [
            new("How did you hear about us?", QuestionType.Text, true),
            new("Any additional comments?", QuestionType.Text, false),
            new("Would you recommend us?", QuestionType.Boolean, true),
            new("May we contact you for follow-up?", QuestionType.Boolean, false)
        ]);

        var createResponse = await Client.PostAsJsonAsync("/api/questionnaires", createDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var v1 = await createResponse.Content.ReadFromJsonAsync<QuestionnaireDto>(JsonOptions);
        Assert.NotNull(v1);
        Assert.Equal(1, v1.Version);
        Assert.Equal(4, v1.Questions.Count);

        // 2. Update: add 5th question, tweak Q1 text → version 2
        var updateDto = new UpdateQuestionnaireDto("Customer Feedback Q2",
        [
            new("How did you first hear about us?", QuestionType.Text, true),
            new("Any additional comments?", QuestionType.Text, false),
            new("Would you recommend us?", QuestionType.Boolean, true),
            new("May we contact you for follow-up?", QuestionType.Boolean, false),
            new("Did you find our service satisfactory?", QuestionType.Boolean, false)
        ]);

        var updateResponse = await Client.PutAsJsonAsync($"/api/questionnaires/{v1.Id}", updateDto, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var v2 = await updateResponse.Content.ReadFromJsonAsync<QuestionnaireDto>(JsonOptions);
        Assert.NotNull(v2);
        Assert.Equal(2, v2.Version);
        Assert.Equal(5, v2.Questions.Count);

        // 3. Submit answers for version 1 (4 answers)
        var answersV1 = new SubmitAnswersDto(
        [
            new(v1.Questions[0].Id, "Google search"),
            new(v1.Questions[1].Id, "Great service!"),
            new(v1.Questions[2].Id, "true"),
            new(v1.Questions[3].Id, "false")
        ]);

        var submitV1Response = await Client.PostAsJsonAsync(
            $"/api/questionnaires/{v1.Id}/versions/1/answers", answersV1, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, submitV1Response.StatusCode);

        // 4. Submit answers for version 2 (5 answers)
        var answersV2 = new SubmitAnswersDto(
        [
            new(v2.Questions[0].Id, "Friend referral"),
            new(v2.Questions[1].Id, ""),
            new(v2.Questions[2].Id, "true"),
            new(v2.Questions[3].Id, "true"),
            new(v2.Questions[4].Id, "true")
        ]);

        var submitV2Response = await Client.PostAsJsonAsync(
            $"/api/questionnaires/{v1.Id}/versions/2/answers", answersV2, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, submitV2Response.StatusCode);

        // 5. List answers, verify both sets returned
        var listResponse = await Client.GetAsync($"/api/questionnaires/{v1.Id}/answers");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var allAnswers = await listResponse.Content.ReadFromJsonAsync<List<AnswerSubmissionDto>>(JsonOptions);
        Assert.NotNull(allAnswers);
        Assert.Equal(2, allAnswers.Count);
    }

    [Fact]
    public async Task SubmitAnswers_MissingRequired_ReturnsBadRequest()
    {
        var createDto = new CreateQuestionnaireDto("Required Test",
            [new("Required Q", QuestionType.Text, true)]);
        var createResponse = await Client.PostAsJsonAsync("/api/questionnaires", createDto, JsonOptions);
        var q = await createResponse.Content.ReadFromJsonAsync<QuestionnaireDto>(JsonOptions);

        var submitResponse = await Client.PostAsJsonAsync(
            $"/api/questionnaires/{q!.Id}/versions/1/answers",
            new SubmitAnswersDto([]), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, submitResponse.StatusCode);
    }

    [Fact]
    public async Task SubmitAnswers_InvalidQuestionId_ReturnsBadRequest()
    {
        var createDto = new CreateQuestionnaireDto("Invalid ID Test",
            [new("Q1", QuestionType.Text, false)]);
        var createResponse = await Client.PostAsJsonAsync("/api/questionnaires", createDto, JsonOptions);
        var q = await createResponse.Content.ReadFromJsonAsync<QuestionnaireDto>(JsonOptions);

        var submitResponse = await Client.PostAsJsonAsync(
            $"/api/questionnaires/{q!.Id}/versions/1/answers",
            new SubmitAnswersDto([new(Guid.NewGuid(), "value")]), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, submitResponse.StatusCode);
    }

    [Fact]
    public async Task SubmitAnswers_NonExistentQuestionnaire_ReturnsBadRequest()
    {
        var submitResponse = await Client.PostAsJsonAsync(
            $"/api/questionnaires/{Guid.NewGuid()}/versions/1/answers",
            new SubmitAnswersDto([]), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, submitResponse.StatusCode);
    }

    [Fact]
    public async Task GetQuestionnaire_NonExistent_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/questionnaires/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuestionnaire_ThenGetReturnsNotFound()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/questionnaires",
            new CreateQuestionnaireDto("ToDelete", [new("Q", QuestionType.Text, false)]), JsonOptions);
        var q = await createResponse.Content.ReadFromJsonAsync<QuestionnaireDto>(JsonOptions);

        var deleteResponse = await Client.DeleteAsync($"/api/questionnaires/{q!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/questionnaires/{q.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task ListQuestionnaires_ExcludesDeleted()
    {
        var r1 = await Client.PostAsJsonAsync("/api/questionnaires",
            new CreateQuestionnaireDto("Keep", [new("Q", QuestionType.Text, false)]), JsonOptions);
        var q1 = await r1.Content.ReadFromJsonAsync<QuestionnaireDto>(JsonOptions);

        var r2 = await Client.PostAsJsonAsync("/api/questionnaires",
            new CreateQuestionnaireDto("Delete", [new("Q", QuestionType.Text, false)]), JsonOptions);
        var q2 = await r2.Content.ReadFromJsonAsync<QuestionnaireDto>(JsonOptions);

        await Client.DeleteAsync($"/api/questionnaires/{q2!.Id}");

        var listResponse = await Client.GetAsync("/api/questionnaires");
        var list = await listResponse.Content.ReadFromJsonAsync<List<QuestionnaireDto>>(JsonOptions);

        Assert.DoesNotContain(list!, q => q.Id == q2.Id);
        Assert.Contains(list!, q => q.Id == q1!.Id);
    }

    [Fact]
    public async Task GetSpecificVersion_ReturnsOldVersion()
    {
        var createDto = new CreateQuestionnaireDto("Versioned",
            [new("Original Q", QuestionType.Text, true)]);
        var r = await Client.PostAsJsonAsync("/api/questionnaires", createDto, JsonOptions);
        var v1 = await r.Content.ReadFromJsonAsync<QuestionnaireDto>(JsonOptions);

        await Client.PutAsJsonAsync($"/api/questionnaires/{v1!.Id}",
            new UpdateQuestionnaireDto("Versioned", [new("Updated Q", QuestionType.Text, true)]), JsonOptions);

        var v1Response = await Client.GetAsync($"/api/questionnaires/{v1.Id}/versions/1");
        var v1Result = await v1Response.Content.ReadFromJsonAsync<QuestionnaireDto>(JsonOptions);
        Assert.Equal("Original Q", v1Result!.Questions[0].Text);
    }
}
