using StackDuel.Api.Authorization;
using StackDuel.Api.Extensions;
using StackDuel.Api.Hubs;
using StackDuel.Api.LanguageServer;
using StackDuel.Api.Middleware;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Settings;
using StackDuel.Application.Notifications;
using StackDuel.Infrastructure;
using Asp.Versioning;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

namespace StackDuel.Api;

public static class ApiServiceRegistration
{
    private const string CorsPolicyName = "AllowedOrigins";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
        });
        services.AddOpenApi();

        var corsOptions =
            configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{CorsOptions.SectionName}' is missing or invalid."
            );

        services.AddSingleton(corsOptions);
        services.AddCors(options =>
        {
            options.AddPolicy(
                CorsPolicyName,
                policy =>
                {
                    policy.WithOrigins(corsOptions.AllowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                }
            );
        });

        services.AddAuth0(configuration);
        services.AddAppInsights(configuration);
        services.AddScoped<AccountContextMiddleware>();
        services.AddAttributeRateLimiting();

        services
            .AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        services.AddScoped<IGameNotificationService, SignalRGameNotificationService>();
        services.AddScoped<ISubmissionNotificationService, SignalRSubmissionNotificationService>();

        return services;
    }

    // Applied before UseGlobalExceptionHandler in Program.cs: the exception handler
    // clears the response (including headers) when it catches an unhandled exception,
    // so CORS must already be registered upstream or error responses come back with
    // no Access-Control-Allow-Origin header and the browser blocks them outright.
    public static IApplicationBuilder UseApiCors(this IApplicationBuilder app) => app.UseCors(CorsPolicyName);

    public static async Task UseApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            await app.Services.MigrateAsync();
        }

        app.UseHttpsRedirection();
        app.UseWebSockets();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<AccountContextMiddleware>();
        app.MapControllers();
        app.MapHub<GameHub>("/hubs/game");
        app.MapHub<SubmissionHub>("/hubs/submission");
        app.Map("/hubs/language-server", LanguageServerBridgeEndpoint.HandleAsync).RequireAuthorization();
    }
}