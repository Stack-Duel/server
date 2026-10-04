using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StackDuel.Application.Users;

namespace StackDuel.Application.DependencyInjection;

/// <summary>
/// Registers everything in the Application layer except Mediator itself: Mediator.SourceGenerator
/// must live in the outermost executable project (StackDuel.Api), so `AddMediator(...)` is called
/// there, not here. Call this from the Api's composition root alongside that call.
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly, includeInternalTypes: true);

        services.AddScoped<IUserService, UserService>();

        return services;
    }
}