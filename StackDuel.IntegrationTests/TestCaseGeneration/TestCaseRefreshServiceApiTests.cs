using Microsoft.Extensions.DependencyInjection;
using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.ExecutionPipelines;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.TestCaseGeneration;

public sealed class TestCaseRefreshServiceApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private readonly IntegrationTestEnvironment _environment = environment;

    private static readonly string ValidQuestion = new('q', 60);

    [Fact]
    public async Task WeeklyRefreshSkipsRetiredSetups()
    {
        const string sub = "sub-refresh-retired";
        await CreateAccountAsync(sub, "refreshretired");
        await GrantStudioPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-refresh-retired", "Refresh Retired Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Refresh Retired Problem");

        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid keptVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(trackId, "refresh-python", judge0Id: 71);
        Guid retiredVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(trackId, "refresh-java", judge0Id: 89);

        Guid keptSetupId = await AddSetupWithSolutionAsync(sub, problemId, keptVersionId, "min_coins");
        Guid retiredSetupId = await AddSetupWithSolutionAsync(sub, problemId, retiredVersionId, "minCoins");

        await Seeder.AttachGenerationSpecAsync(keptSetupId);
        await Seeder.AttachGenerationSpecAsync(retiredSetupId);
        await SetGenerationParametersAsync(sub, problemId);
        await Seeder.MarkProblemPublishedAsync(problemId);

        await RetireSetupAsync(sub, problemId, retiredVersionId);

        int enqueued = await EnqueueRefreshesAsync();

        Assert.Equal(1, enqueued);
        IReadOnlyList<Guid> jobSetupIds = await Seeder.GetGenerationJobSetupIdsAsync();
        Assert.Contains(keptSetupId, jobSetupIds);
        Assert.DoesNotContain(retiredSetupId, jobSetupIds);
    }

    private async Task<int> EnqueueRefreshesAsync()
    {
        using IServiceScope scope = _environment.Factory.Services.CreateScope();
        var refreshService = scope.ServiceProvider.GetRequiredService<ITestCaseRefreshService>();

        return await refreshService.EnqueueRefreshesAsync();
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
            new UpsertStudioProblemSetupSolutionRequest(
                languageVersionId,
                "starter",
                functionName,
                "reference solution"
            )
        );
        HttpResponseMessage solutionResponse = await Client.SendAsync(solution);
        Assert.True(
            solutionResponse.StatusCode == HttpStatusCode.OK,
            await solutionResponse.Content.ReadAsStringAsync()
        );

        return await solutionResponse.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task SetGenerationParametersAsync(string sub, Guid problemId)
    {
        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/studio/problems/{problemId}/generation-parameters",
            sub
        );
        request.Content = JsonContent.Create(
            new SetProblemGenerationParametersRequest(
                [new GenerationParameterRequestItem("amount", "integer", 0, 100, null, null, null)],
                "integer",
                TargetCaseCount: 5,
                Seed: 7
            )
        );

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
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