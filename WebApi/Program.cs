var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));

app.MapGet("/ping", () => Results.Ok("pong"))
    .WithName("Ping")
    .WithDescription("Returns pong")
    .WithTags("Health")
    .Produces<string>(StatusCodes.Status200OK);

app.Run();

public partial class Program;
