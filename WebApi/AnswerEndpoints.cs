using DataAccess;

namespace WebApi;

public static class AnswerEndpoints
{
    public static IEndpointRouteBuilder MapAnswerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/questionnaires")
            .WithTags("Answers");

        group.MapPost("/{id:guid}/versions/{version:int}/answers",
            (Guid id, int version, SubmitAnswersDto dto, IQuestionnaireRepository repo) =>
        {
            try
            {
                var result = repo.SubmitAnswers(id, version, dto);
                return Results.Created($"/api/questionnaires/{id}/answers/{result.Id}", result);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("SubmitAnswers")
        .WithDescription("Submits answers for a specific questionnaire version")
        .Produces<AnswerSubmissionDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}/answers", (Guid id, IQuestionnaireRepository repo) =>
            Results.Ok(repo.ListAnswers(id)))
        .WithName("ListAnswers")
        .WithDescription("Lists all answer submissions for a questionnaire")
        .Produces<List<AnswerSubmissionDto>>(StatusCodes.Status200OK);

        return endpoints;
    }
}
