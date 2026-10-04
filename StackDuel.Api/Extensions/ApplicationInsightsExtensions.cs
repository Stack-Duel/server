using Microsoft.ApplicationInsights.AspNetCore.Extensions;

namespace StackDuel.Api.Extensions;

public static class ApplicationInsightsExtensions
{
    public static IServiceCollection AddAppInsights(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration["ApplicationInsights:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddApplicationInsightsTelemetry(
            new ApplicationInsightsServiceOptions { ConnectionString = connectionString }
        );
        return services;
    }
}