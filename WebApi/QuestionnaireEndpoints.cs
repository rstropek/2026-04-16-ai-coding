using DataAccess;

namespace WebApi;

public static class QuestionnaireEndpoints
{
    public static IEndpointRouteBuilder MapQuestionnaireEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/questionnaires")
            .WithTags("Questionnaires");

        group.MapPost("/", (CreateQuestionnaireDto dto, IQuestionnaireRepository repo) =>
        {
            var result = repo.CreateQuestionnaire(dto);
            return Results.Created($"/api/questionnaires/{result.Id}", result);
        })
        .WithName("CreateQuestionnaire")
        .WithDescription("Creates a new questionnaire")
        .Produces<QuestionnaireDto>(StatusCodes.Status201Created);

        group.MapGet("/", (bool? includeDeleted, IQuestionnaireRepository repo) =>
            Results.Ok(repo.ListQuestionnaires(includeDeleted ?? false)))
        .WithName("ListQuestionnaires")
        .WithDescription("Lists questionnaires, optionally including soft-deleted ones")
        .Produces<List<QuestionnaireDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", (Guid id, IQuestionnaireRepository repo) =>
        {
            var result = repo.GetQuestionnaire(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetQuestionnaire")
        .WithDescription("Gets the latest version of a questionnaire")
        .Produces<QuestionnaireDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/versions/{version:int}", (Guid id, int version, IQuestionnaireRepository repo) =>
        {
            var result = repo.GetQuestionnaire(id, version);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetQuestionnaireVersion")
        .WithDescription("Gets a specific version of a questionnaire")
        .Produces<QuestionnaireDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", (Guid id, UpdateQuestionnaireDto dto, IQuestionnaireRepository repo) =>
        {
            var result = repo.UpdateQuestionnaire(id, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("UpdateQuestionnaire")
        .WithDescription("Updates a questionnaire, creating a new version")
        .Produces<QuestionnaireDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", (Guid id, IQuestionnaireRepository repo) =>
        {
            var deleted = repo.DeleteQuestionnaire(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteQuestionnaire")
        .WithDescription("Soft-deletes a questionnaire")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/restore", (Guid id, IQuestionnaireRepository repo) =>
        {
            var restored = repo.RestoreQuestionnaire(id);
            return restored ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RestoreQuestionnaire")
        .WithDescription("Restores a soft-deleted questionnaire")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
