using StackDuel.Api.Requests.User;
using StackDuel.Api.Responses.User;
using StackDuel.Domain.Authorization.Rbac.Enums;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.Authorization;

public sealed class UserAccessApiTests : ApiIntegrationTestBase
{
    [Test]
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

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        UserResponse? body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Username, Is.EqualTo("alice123"));
        Assert.That(body.Permissions, Is.EquivalentTo(new[] { "problems.read" }));
        Assert.That(body.Roles, Is.EquivalentTo(new[] { "Member" }));
    }

    [Test]
    public async Task GetAccount_returns_not_found_when_unauthenticated()
    {
        HttpResponseMessage response = await Client.GetAsync("/api/v1/user");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private async Task CreateAccountAsync(string sub, string username)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        request.Content = JsonContent.Create(new UpsertUserRequest(username, null, null));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
    }
}