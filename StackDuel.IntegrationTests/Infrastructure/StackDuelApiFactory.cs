using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StackDuel.IntegrationTests.Infrastructure;

public sealed class StackDuelApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            RemoveHostedService(
                services,
                "StackDuel.Infrastructure.Messaging.Consumers.RabbitMqConsumerService, StackDuel.Infrastructure"
            );
            RemoveHostedService(
                services,
                "StackDuel.Infrastructure.Messaging.Consumers.AzureServiceBusConsumerService, StackDuel.Infrastructure"
            );
        });
    }

    private static void RemoveHostedService(IServiceCollection services, string assemblyQualifiedTypeName)
    {
        Type? implementationType = Type.GetType(assemblyQualifiedTypeName);
        if (implementationType is null)
            return;

        ServiceDescriptor? descriptor = services.SingleOrDefault(d =>
            d.ServiceType == typeof(IHostedService) && d.ImplementationType == implementationType
        );

        if (descriptor is not null)
            services.Remove(descriptor);
    }
}