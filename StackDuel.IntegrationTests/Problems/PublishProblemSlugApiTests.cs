using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Problems.Enums;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.Problems;

public sealed class PublishProblemSlugApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static readonly string ValidQuestion = new('q', 60);

    [Fact]
    public async Task PublishingAProblemReplacesTheDraftSlugWithOneFromTheTitle()
    {
        const string sub = "sub-publish-slug";
        await CreateAccountAsync(sub, "publishslug");
        await GrantAdminProblemPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-publish-slug", "Publish Slug Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Draft Title");

        string draftSlug = await Seeder.GetProblemSlugAsync(problemId);
        Assert.True(Guid.TryParse(draftSlug, out _), $"expected a draft guid slug, got '{draftSlug}'");

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/problem/admin/{problemId}",
            sub
        );
        request.Content = JsonContent.Create(
            new UpdateProblemRequest(
                "Minimum Coins For Change",
                ValidQuestion,
                500,
                1000,
                64,
                ["greedy"],
                ProblemStatus.Published
            )
        );

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Assert.Equal("minimum-coins-for-change", await Seeder.GetProblemSlugAsync(problemId));
    }

    private async Task GrantAdminProblemPermissionsAsync(string sub)
    {
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);
        Guid roleId = await Seeder.CreateRoleAsync($"role-{sub}");

        foreach (
            string permission in new[]
            {
                WellKnownAuthorization.CreateAdminProblemsPermission,
                WellKnownAuthorization.UpdateAdminProblemsPermission,
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