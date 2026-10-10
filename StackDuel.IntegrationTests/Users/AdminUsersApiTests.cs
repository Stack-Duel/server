using StackDuel.Api.Requests.User;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users.Dtos.Admin;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.Users;

public sealed class AdminUsersApiTests(IntegrationTestEnvironment environment) : ApiIntegrationTestBase(environment)
{
    [Fact]
    public async Task GetAdminUsers_returns_users_with_group_memberships_for_a_permitted_caller()
    {
        const string adminSub = "sub-admin-1";
        await CreateAccountAsync(adminSub, "admin1");
        await GrantPermissionToNewGroupAsync(adminSub, WellKnownAuthorization.ReadAdminUsersPermission);

        const string memberSub = "sub-iris-1";
        await CreateAccountAsync(memberSub, "iris22");
        Guid memberUserId = await Seeder.GetUserIdBySubAsync(memberSub);
        Guid testersGroupId = await Seeder.CreateGroupAsync("Testers");
        await Seeder.AddUserToGroupAsync(memberUserId, testersGroupId);

        HttpResponseMessage response = await Client.SendAsync(
            AuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/user/admin?Page=1&Size=10&Timestamp=2026-01-01T00:00:00Z",
                adminSub
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PageResult<AdminUserDto>? page = await response.Content.ReadFromJsonAsync<PageResult<AdminUserDto>>();
        Assert.NotNull(page);
        AdminUserDto member = page!.Results.Single(u => u.Id == memberUserId);
        Assert.Equivalent(new[] { "Testers" }, member.Groups.Select(g => g.Name), strict: true);
    }

    [Fact]
    public async Task GetAdminUsers_filters_by_username_search()
    {
        const string adminSub = "sub-admin-2";
        await CreateAccountAsync(adminSub, "admin2");
        await GrantPermissionToNewGroupAsync(adminSub, WellKnownAuthorization.ReadAdminUsersPermission);

        const string matchingSub = "sub-match-1";
        await CreateAccountAsync(matchingSub, "searchable_user");
        Guid matchingUserId = await Seeder.GetUserIdBySubAsync(matchingSub);

        const string otherSub = "sub-other-1";
        await CreateAccountAsync(otherSub, "unrelated_user");

        HttpResponseMessage response = await Client.SendAsync(
            AuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/user/admin?Page=1&Size=10&Timestamp=2026-01-01T00:00:00Z&Search=searchable",
                adminSub
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PageResult<AdminUserDto>? page = await response.Content.ReadFromJsonAsync<PageResult<AdminUserDto>>();
        Assert.NotNull(page);
        Assert.Equivalent(new[] { matchingUserId }, page!.Results.Select(u => u.Id), strict: true);
    }

    [Fact]
    public async Task GetAdminUserDetail_returns_the_user_for_a_permitted_caller()
    {
        const string adminSub = "sub-admin-3";
        await CreateAccountAsync(adminSub, "admin3");
        await GrantPermissionToNewGroupAsync(adminSub, WellKnownAuthorization.ReadAdminUsersPermission);

        const string memberSub = "sub-iris-2";
        await CreateAccountAsync(memberSub, "iris23");
        Guid memberUserId = await Seeder.GetUserIdBySubAsync(memberSub);
        Guid testersGroupId = await Seeder.CreateGroupAsync("Testers2");
        await Seeder.AddUserToGroupAsync(memberUserId, testersGroupId);

        HttpResponseMessage response = await Client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, $"/api/v1/user/admin/{memberUserId}", adminSub)
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AdminUserDetailDto? detail = await response.Content.ReadFromJsonAsync<AdminUserDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal("iris23", detail!.Username);
        Assert.Equivalent(new[] { "Testers2" }, detail.Groups.Select(g => g.Name), strict: true);
    }

    [Fact]
    public async Task GetAdminUserDetail_returns_not_found_for_an_unknown_user()
    {
        const string adminSub = "sub-admin-4";
        await CreateAccountAsync(adminSub, "admin4");
        await GrantPermissionToNewGroupAsync(adminSub, WellKnownAuthorization.ReadAdminUsersPermission);

        HttpResponseMessage response = await Client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, $"/api/v1/user/admin/{Guid.NewGuid()}", adminSub)
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminUsers_returns_forbidden_for_a_caller_without_the_permission()
    {
        const string sub = "sub-plain-1";
        await CreateAccountAsync(sub, "plainuser1");

        HttpResponseMessage response = await Client.SendAsync(
            AuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/user/admin?Page=1&Size=10&Timestamp=2026-01-01T00:00:00Z",
                sub
            )
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminUsers_returns_unauthorized_when_unauthenticated()
    {
        HttpResponseMessage response = await Client.GetAsync(
            "/api/v1/user/admin?Page=1&Size=10&Timestamp=2026-01-01T00:00:00Z"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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