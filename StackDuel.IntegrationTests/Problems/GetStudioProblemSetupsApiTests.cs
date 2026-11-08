using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.ExecutionPipelines;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StackDuel.IntegrationTests.Problems;

public sealed class GetStudioProblemSetupsApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CreateDraftProblemRequest ValidRequest(Guid trackId, string title) =>
        new(title, new string('q', 60), ["arrays"], 100, 1000, 256, trackId);

    [Fact]
    public async Task GetSetups_OwnerWithSetup_ReturnsSetupDetails()
    {
        const string sub = "sub-studio-setups-owner";
        await CreateAccountAsync(sub, "studiosetupsowner1");
        await GrantCreateProblemsPermissionAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-setups-1", "Studio Setups Track 1");

        Guid problemId = await CreateProblemAsync(sub, trackId, "Studio Setups Owner Problem");
        Guid languageVersionId = await AddSetupAsync(sub, problemId);

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/studio/problems/{problemId}/setups",
            sub
        );
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        List<StudioProblemDetailsSetupDto>? setups = await response.Content.ReadFromJsonAsync<
            List<StudioProblemDetailsSetupDto>
        >(JsonOptions);

        Assert.NotNull(setups);
        StudioProblemDetailsSetupDto setup = Assert.Single(setups);
        Assert.Equal(languageVersionId, setup.LanguageVersionId);
        Assert.Equal("python", setup.Language);
        Assert.Equal("1.0", setup.LanguageVersion);
        Assert.Equal("0", setup.AmountOfTestSuites);
        Assert.Equal("0", setup.AmountOfTestCases);
    }

    [Fact]
    public async Task GetSetups_OwnerWithNoSetups_ReturnsEmptyList()
    {
        const string sub = "sub-studio-setups-owner-empty";
        await CreateAccountAsync(sub, "studiosetupsowner2");
        await GrantCreateProblemsPermissionAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-setups-2", "Studio Setups Track 2");

        Guid problemId = await CreateProblemAsync(sub, trackId, "Studio Setups No Setups");

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/studio/problems/{problemId}/setups",
            sub
        );
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        List<StudioProblemDetailsSetupDto>? setups = await response.Content.ReadFromJsonAsync<
            List<StudioProblemDetailsSetupDto>
        >(JsonOptions);

        Assert.NotNull(setups);
        Assert.Empty(setups);
    }

    [Fact]
    public async Task GetSetups_NotOwner_ReturnsNotFound()
    {
        const string ownerSub = "sub-studio-setups-owner3";
        const string otherSub = "sub-studio-setups-other3";
        await CreateAccountAsync(ownerSub, "studiosetupsowner3");
        await CreateAccountAsync(otherSub, "studiosetupsother3");
        await GrantCreateProblemsPermissionAsync(ownerSub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-setups-3", "Studio Setups Track 3");

        Guid problemId = await CreateProblemAsync(ownerSub, trackId, "Not Yours Setups Problem");

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/studio/problems/{problemId}/setups",
            otherSub
        );
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSetups_Unauthenticated_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await Client.GetAsync($"/api/v1/studio/problems/{Guid.NewGuid()}/setups");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSetups_DoesNotExist_ReturnsNotFound()
    {
        const string sub = "sub-studio-setups-missing";
        await CreateAccountAsync(sub, "studiosetupsmissing1");

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/studio/problems/{Guid.NewGuid()}/setups",
            sub
        );
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task GrantCreateProblemsPermissionAsync(string sub)
    {
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);
        Guid permissionId = await Seeder.CreatePermissionAsync(WellKnownAuthorization.CreateAdminProblemsPermission);
        Guid roleId = await Seeder.CreateRoleAsync($"role-{sub}");
        await Seeder.GrantRolePermissionAsync(roleId, permissionId, DecisionEffect.Allow);
        Guid groupId = await Seeder.CreateGroupAsync($"group-{sub}");
        await Seeder.GrantGroupRoleAsync(groupId, roleId);
        await Seeder.AddUserToGroupAsync(userId, groupId);
    }

    private async Task<Guid> CreateProblemAsync(string sub, Guid trackId, string title)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/problem", sub);
        request.Content = JsonContent.Create(ValidRequest(trackId, title));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> AddSetupAsync(string sub, Guid problemId)
    {
        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid languageVersionId = await Seeder.CreateLanguageVersionAsync("python", judge0Id: 71);

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/problem/admin/{problemId}/setups",
            sub
        );
        request.Content = JsonContent.Create(new AddProblemSetupRequest(languageVersionId));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return languageVersionId;
    }

    private async Task CreateAccountAsync(string sub, string username)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        request.Content = JsonContent.Create(new UpsertUserRequest(username, null, null));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}
