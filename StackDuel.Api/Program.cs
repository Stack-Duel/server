using StackDuel.Api;
using StackDuel.Api.Attributes;
using StackDuel.Api.Middleware;
using StackDuel.Application;
using StackDuel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseApiCors();
app.UseGlobalExceptionHandler();

await app.UseApi();

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous()
    .ExcludeFromDescription()
    .WithMetadata(new SkipAccountContextAttribute());

app.MapHealthChecks("/api/v1/health/ready")
    .AllowAnonymous()
    .ExcludeFromDescription()
    .WithMetadata(new SkipAccountContextAttribute());

app.MapGet("/", () => Results.Ok())
    .AllowAnonymous()
    .ExcludeFromDescription()
    .WithMetadata(new SkipAccountContextAttribute());

app.Run();