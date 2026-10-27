using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.Problems;

public sealed class CreateDraftProblemApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static CreateDraftProblemRequest ValidRequest(Guid trackId) =>
        new("Two Sum", new string('q', 60), ["arrays"], 100, 1000, 256, trackId);

    [Fact]
    public async Task CreateProblem_ValidRequest_CreatesProblem()
    {
        const string sub = "sub-create-problem-1";
        await CreateAccountAsync(sub, "creator1");
        await GrantPermissionToNewGroupAsync(sub, WellKnownAuthorization.CreateAdminProblemsPermission);
        Guid trackId = await Seeder.CreateTrackAsync("track-create-1", "Create Track 1");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/problem", sub);
        request.Content = JsonContent.Create(ValidRequest(trackId));

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Guid problemId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, problemId);
    }

    [Fact]
    public async Task CreateProblem_Unauthenticated_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/problem", ValidRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProblem_WithoutPermission_ReturnsForbidden()
    {
        const string sub = "sub-create-problem-2";
        await CreateAccountAsync(sub, "creator2");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/problem", sub);
        request.Content = JsonContent.Create(ValidRequest(Guid.NewGuid()));

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProblem_InactiveTrack_ReturnsBadRequest()
    {
        const string sub = "sub-create-problem-3";
        await CreateAccountAsync(sub, "creator3");
        await GrantPermissionToNewGroupAsync(sub, WellKnownAuthorization.CreateAdminProblemsPermission);
        Guid trackId = await Seeder.CreateTrackAsync("track-create-3", "Create Track 3", isActive: false);

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/problem", sub);
        request.Content = JsonContent.Create(ValidRequest(trackId));

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProblem_NoTags_ReturnsBadRequest()
    {
        const string sub = "sub-create-problem-4";
        await CreateAccountAsync(sub, "creator4");
        await GrantPermissionToNewGroupAsync(sub, WellKnownAuthorization.CreateAdminProblemsPermission);
        Guid trackId = await Seeder.CreateTrackAsync("track-create-4", "Create Track 4");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/problem", sub);
        request.Content = JsonContent.Create(ValidRequest(trackId) with { Tags = [] });

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task CreateAccountAsync(string sub, string username)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        request.Content = JsonContent.Create(new UpsertUserRequest(username, null, null));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task GrantPermissionToNewGroupAsync(string sub, string permissionCode)
    {
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);
        Guid permissionId = await Seeder.CreatePermissionAsync(permissionCode);
        Guid roleId = await Seeder.CreateRoleAsync($"role-for-{permissionCode}");
        await Seeder.GrantRolePermissionAsync(roleId, permissionId, DecisionEffect.Allow);
        Guid groupId = await Seeder.CreateGroupAsync($"group-for-{permissionCode}");
        await Seeder.GrantGroupRoleAsync(groupId, roleId);
        await Seeder.AddUserToGroupAsync(userId, groupId);
    }
}