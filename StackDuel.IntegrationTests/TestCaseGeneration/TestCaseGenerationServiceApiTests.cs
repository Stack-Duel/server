using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.TestCaseGeneration;

public sealed class TestCaseGenerationServiceApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private readonly IntegrationTestEnvironment _environment = environment;

    private static readonly string ValidQuestion = new('q', 60);

    private sealed class AlwaysAcceptedEngine : IExecutionEngineStrategy
    {
        public Task<IReadOnlyList<ExecutionEngineResult>> SubmitBatchAsync(
            IReadOnlyList<ExecutionEngineSubmission> submissions,
            Guid? submissionId = null,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Results(submissions.Count));

        public Task<IReadOnlyList<ExecutionEngineResult>> PollBatchAsync(
            IReadOnlyList<string> tokens,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Results(tokens.Count));

        private static IReadOnlyList<ExecutionEngineResult> Results(int count) =>
            [
                .. Enumerable
                    .Range(0, count)
                    .Select(index => new ExecutionEngineResult(
                        Token: $"token-{index}",
                        Stdout: "0",
                        Stderr: null,
                        CompileOutput: null,
                        RuntimeMs: 1,
                        MemoryUsedKb: 1,
                        Status: ExecutionEngineResultStatus.Accepted
                    )),
            ];
    }

    [Fact]
    public async Task GenerationBuildsTheRequestedNumberOfCases()
    {
        const string sub = "sub-generate-active";
        await CreateAccountAsync(sub, "generateactive");
        await GrantStudioPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-generate-active", "Generate Active Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Generate Active Problem");

        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid keptVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(trackId, "python", judge0Id: 71);
        Guid retiredVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(trackId, "java", judge0Id: 89);

        Guid keptSetupId = await AddSetupWithSolutionAsync(sub, problemId, keptVersionId, "min_coins");
        await AddSetupWithSolutionAsync(sub, problemId, retiredVersionId, "minCoins");
        await RetireSetupAsync(sub, problemId, retiredVersionId);

        GenerateTestCasesResult result = await GenerateAsync(
            new GenerateTestCasesRequest(
                keptSetupId,
                "def min_coins(amount):\n    return 0",
                [new GenerationParameterSpec("amount", "integer", 0, 100, null, null, null)],
                "integer",
                TargetCaseCount: 3,
                Seed: 7
            )
        );

        Assert.Equal(3, result.Requested);
        Assert.Equal(3, result.Generated);
        Assert.Empty(result.Warnings);
    }

    private async Task<GenerateTestCasesResult> GenerateAsync(GenerateTestCasesRequest request)
    {
        using var factory = _environment.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IExecutionEngineStrategy, AlwaysAcceptedEngine>()
            )
        );

        using IServiceScope scope = factory.Services.CreateScope();
        var generationService = scope.ServiceProvider.GetRequiredService<ITestCaseGenerationService>();

        return await generationService.GenerateAsync(request);
    }

    private async Task<Guid> AddSetupWithSolutionAsync(
        string sub,
        Guid problemId,
        Guid languageVersionId,
        string functionName
    )
    {
        HttpRequestMessage addSetup = AuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/studio/problems/{problemId}/setups",
            sub
        );
        addSetup.Content = JsonContent.Create(new AddProblemSetupRequest(languageVersionId));
        HttpResponseMessage addResponse = await Client.SendAsync(addSetup);
        Assert.True(addResponse.StatusCode == HttpStatusCode.OK, await addResponse.Content.ReadAsStringAsync());

        HttpRequestMessage solution = AuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/studio/problems/{problemId}/setups/reference-solution",
            sub
        );
        solution.Content = JsonContent.Create(
            new UpsertStudioProblemSetupSolutionRequest(languageVersionId, "starter", functionName, "reference")
        );
        HttpResponseMessage solutionResponse = await Client.SendAsync(solution);
        Assert.True(
            solutionResponse.StatusCode == HttpStatusCode.OK,
            await solutionResponse.Content.ReadAsStringAsync()
        );

        return await solutionResponse.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task RetireSetupAsync(string sub, Guid problemId, Guid languageVersionId)
    {
        HttpResponseMessage response = await Client.SendAsync(
            AuthenticatedRequest(
                HttpMethod.Delete,
                $"/api/v1/studio/problems/{problemId}/setups/{languageVersionId}",
                sub
            )
        );

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task GrantStudioPermissionsAsync(string sub)
    {
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);
        Guid roleId = await Seeder.CreateRoleAsync($"role-{sub}");

        foreach (
            string permission in new[]
            {
                WellKnownAuthorization.CreateAdminProblemsPermission,
                WellKnownAuthorization.ReadStudioProblemsPermission,
                WellKnownAuthorization.UpdateStudioProblemsPermission,
            }
        )
        {
            Guid permissionId = await Seeder.EnsurePermissionAsync(permission);
            await Seeder.GrantRolePermissionAsync(roleId, permissionId, DecisionEffect.Allow);
        }

        Guid groupId = await Seeder.CreateGroupAsync($"group-{sub}");
        await Seeder.GrantGroupRoleAsync(groupId, roleId);
        await Seeder.AddUserToGroupAsync(userId, groupId);
    }

    private async Task<Guid> CreateProblemAsync(string sub, Guid trackId, string title)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/problem", sub);
        request.Content = JsonContent.Create(
            new CreateDraftProblemRequest(title, ValidQuestion, ["arrays"], 100, 1000, 256, trackId)
        );

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task CreateAccountAsync(string sub, string username)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        request.Content = JsonContent.Create(new UpsertUserRequest(username, null, null));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}