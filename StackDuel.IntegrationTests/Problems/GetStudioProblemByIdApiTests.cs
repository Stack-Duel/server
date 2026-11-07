using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StackDuel.IntegrationTests.Problems;

public sealed class GetStudioProblemByIdApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CreateDraftProblemRequest ValidRequest(Guid trackId, string title) =>
        new(title, new string('q', 60), ["arrays"], 100, 1000, 256, trackId);

    [Fact]
    public async Task GetById_Owner_ReturnsDetails()
    {
        const string sub = "sub-studio-problem-owner";
        await CreateAccountAsync(sub, "studioowner1");
        await GrantCreateProblemsPermissionAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-1", "Studio Track 1");

        Guid problemId = await CreateProblemAsync(sub, trackId, "Studio Owner Problem");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Get, $"/api/v1/studio/problems/{problemId}", sub);
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        StudioProblemDetailsDto? details = await response.Content.ReadFromJsonAsync<StudioProblemDetailsDto>(
            JsonOptions
        );

        Assert.NotNull(details);
        Assert.Equal(problemId, details.ProblemId);
        Assert.Equal("Studio Owner Problem", details.Title);
        Assert.Equal(0, details.AttemptCount);
        Assert.Equal(0, details.SolveRatePercent);
        Assert.Equal(0, details.ReactionCount);
    }

    [Fact]
    public async Task GetById_NotOwner_ReturnsNotFound()
    {
        const string ownerSub = "sub-studio-problem-owner2";
        const string otherSub = "sub-studio-problem-other2";
        await CreateAccountAsync(ownerSub, "studioowner2");
        await CreateAccountAsync(otherSub, "studioother2");
        await GrantCreateProblemsPermissionAsync(ownerSub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-2", "Studio Track 2");

        Guid problemId = await CreateProblemAsync(ownerSub, trackId, "Not Yours Problem");

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/studio/problems/{problemId}",
            otherSub
        );
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Unauthenticated_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await Client.GetAsync($"/api/v1/studio/problems/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_DoesNotExist_ReturnsNotFound()
    {
        const string sub = "sub-studio-problem-missing";
        await CreateAccountAsync(sub, "studiomissing1");

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/studio/problems/{Guid.NewGuid()}",
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

    private async Task CreateAccountAsync(string sub, string username)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        request.Content = JsonContent.Create(new UpsertUserRequest(username, null, null));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}
