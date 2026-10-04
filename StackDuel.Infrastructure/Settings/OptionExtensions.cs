using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackDuel.Application.Settings;

namespace StackDuel.Infrastructure.Settings;

public static class OptionExtensions
{
    public static IServiceCollection AddOption<T>(this IServiceCollection services, IConfiguration configuration)
        where T : class, IOption
    {
        var instance =
            configuration.GetSection(T.SectionName).Get<T>()
            ?? throw new InvalidOperationException($"Configuration section '{T.SectionName}' is missing or invalid.");

        services.AddSingleton(instance);
        return services;
    }
}