using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StackDuel.IntegrationTests.Problems;

public sealed class GetMyProblemsApiTests(IntegrationTestEnvironment environment) : ApiIntegrationTestBase(environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CreateDraftProblemRequest ValidRequest(Guid trackId, string title) =>
        new(title, new string('q', 60), ["arrays"], 100, 1000, 256, trackId);

    private const string MineUrl = "/api/v1/problem/mine?page=1&size=20&timestamp=2026-01-01T00:00:00Z";

    [Fact]
    public async Task GetMyProblems_ReturnsOnlyCurrentUsersProblems()
    {
        const string ownerSub = "sub-my-problems-owner";
        const string otherSub = "sub-my-problems-other";
        await CreateAccountAsync(ownerSub, "owner1");
        await CreateAccountAsync(otherSub, "other1");

        Dictionary<string, Guid> permissions = [];
        await GrantPermissionsToNewGroupAsync(
            ownerSub,
            permissions,
            WellKnownAuthorization.CreateAdminProblemsPermission,
            WellKnownAuthorization.ReadAdminProblemsPermission
        );
        await GrantPermissionsToNewGroupAsync(
            otherSub,
            permissions,
            WellKnownAuthorization.CreateAdminProblemsPermission
        );
        Guid trackId = await Seeder.CreateTrackAsync("track-mine-1", "Mine Track 1");

        await CreateProblemAsync(ownerSub, trackId, "Owner Problem");
        await CreateProblemAsync(otherSub, trackId, "Other Problem");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Get, MineUrl, ownerSub);
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        PageResult<MyProblemListItemDto>? page = await response.Content.ReadFromJsonAsync<
            PageResult<MyProblemListItemDto>
        >(JsonOptions);

        Assert.NotNull(page);
        MyProblemListItemDto item = Assert.Single(page.Results);
        Assert.Equal("Owner Problem", item.Title);
    }

    [Fact]
    public async Task GetMyProblems_Unauthenticated_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await Client.GetAsync(MineUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyProblems_WithoutPermission_ReturnsForbidden()
    {
        const string sub = "sub-my-problems-noperm";
        await CreateAccountAsync(sub, "noperm1");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Get, MineUrl, sub);
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task GrantPermissionsToNewGroupAsync(
        string sub,
        Dictionary<string, Guid> permissionIdsByCode,
        params string[] permissionCodes
    )
    {
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);
        Guid roleId = await Seeder.CreateRoleAsync($"role-{sub}");

        foreach (string code in permissionCodes)
        {
            if (!permissionIdsByCode.TryGetValue(code, out Guid permissionId))
            {
                permissionId = await Seeder.CreatePermissionAsync(code);
                permissionIdsByCode[code] = permissionId;
            }

            await Seeder.GrantRolePermissionAsync(roleId, permissionId, DecisionEffect.Allow);
        }

        Guid groupId = await Seeder.CreateGroupAsync($"group-{sub}");
        await Seeder.GrantGroupRoleAsync(groupId, roleId);
        await Seeder.AddUserToGroupAsync(userId, groupId);
    }

    private async Task CreateProblemAsync(string sub, Guid trackId, string title)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/problem", sub);
        request.Content = JsonContent.Create(ValidRequest(trackId, title));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task CreateAccountAsync(string sub, string username)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        request.Content = JsonContent.Create(new UpsertUserRequest(username, null, null));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}