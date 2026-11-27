using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackDuel.Api.Middleware;
using StackDuel.Domain.SeedWork;
using System.Net;
using System.Text.Json;

namespace StackDuel.Api.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    private sealed class TestDomainException(string message) : DomainException(message) { }

    private static async Task<IHost> StartHostThrowing(Exception exception)
    {
        IHost host = await new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseGlobalExceptionHandler();
                        app.Run(_ => throw exception);
                    })
                    .ConfigureServices(services => services.AddLogging());
            })
            .StartAsync();

        return host;
    }

    private static async Task<(HttpStatusCode Status, string? Message)> GetAsync(Exception exception)
    {
        using IHost host = await StartHostThrowing(exception);
        using HttpClient client = host.GetTestClient();

        HttpResponseMessage response = await client.GetAsync("/boom");
        string body = await response.Content.ReadAsStringAsync();

        string? message = JsonDocument.Parse(body).RootElement.TryGetProperty("message", out JsonElement value)
            ? value.GetString()
            : null;

        return (response.StatusCode, message);
    }

    [Fact]
    public async Task DomainInvariantViolation_IsReportedAsABadRequestWithItsMessage()
    {
        (HttpStatusCode status, string? message) = await GetAsync(
            new TestDomainException("Username is invalid: bad characters.")
        );

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("Username is invalid: bad characters.", message);
    }

    [Fact]
    public async Task ValidationException_IsReportedAsABadRequest()
    {
        (HttpStatusCode status, string? message) = await GetAsync(new ValidationException("invalid"));

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("Validation failed", message);
    }

    [Fact]
    public async Task AnyOtherException_IsReportedAsAServerErrorWithoutLeakingTheMessage()
    {
        (HttpStatusCode status, string? message) = await GetAsync(
            new InvalidOperationException("connection string: secret")
        );

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal("An unexpected error occurred.", message);
    }
}