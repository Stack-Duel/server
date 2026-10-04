using Mediator;
using StackDuel.Application.Behaviors;
using StackDuel.Application.DependencyInjection;

namespace StackDuel.Api;

public static class ApiServiceRegistration
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddMediatorDispatch();
        services.AddApplicationLayer();

        return services;
    }

    private static void AddMediatorDispatch(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(LoggingBehavior<,>)];
        });
    }

    private static void AddApplicationLayer(this IServiceCollection services)
    {
        services.AddApplication();
    }
}