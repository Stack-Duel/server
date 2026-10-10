using StackDuel.Api.Requests.User;
using StackDuel.Api.Responses.User;
using StackDuel.Domain.Authorization.Rbac.Enums;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.Authorization;

public sealed class UserAccessApiTests(IntegrationTestEnvironment environment) : ApiIntegrationTestBase(environment)
{
    [Fact]
    public async Task GetAccount_returns_permissions_and_roles_derived_from_the_users_groups()
    {
        const string sub = "sub-alice-1";
        await CreateAccountAsync(sub, "alice123");

        Guid userId = await Seeder.GetUserIdBySubAsync(sub);
        Guid readPermissionId = await Seeder.CreatePermissionAsync("problems.read");
        Guid writePermissionId = await Seeder.CreatePermissionAsync("problems.write");
        Guid roleId = await Seeder.CreateRoleAsync("Member");
        await Seeder.GrantRolePermissionAsync(roleId, readPermissionId, DecisionEffect.Allow);
        await Seeder.GrantRolePermissionAsync(roleId, writePermissionId, DecisionEffect.Deny);
        Guid groupId = await Seeder.CreateGroupAsync("Beta Testers");
        await Seeder.GrantGroupRoleAsync(groupId, roleId);
        await Seeder.AddUserToGroupAsync(userId, groupId);

        HttpResponseMessage response = await Client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/user", sub)
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UserResponse? body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(body);
        Assert.Equal("alice123", body!.Username);
        Assert.Equivalent(new[] { "problems.read" }, body.Permissions, strict: true);
        Assert.Equivalent(new[] { "Member" }, body.Roles, strict: true);
    }

    [Fact]
    public async Task GetAccount_returns_not_found_when_unauthenticated()
    {
        HttpResponseMessage response = await Client.GetAsync("/api/v1/user");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task CreateAccountAsync(string sub, string username)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        request.Content = JsonContent.Create(new UpsertUserRequest(username, null, null));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}