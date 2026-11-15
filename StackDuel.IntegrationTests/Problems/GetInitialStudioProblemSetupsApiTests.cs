using StackDuel.Api.Requests.User;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Tracks.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StackDuel.IntegrationTests.Problems;

public sealed class GetInitialStudioProblemSetupsApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string InitialValuesUrl = "/api/v1/studio/problems/initial-values";

    [Fact]
    public async Task GetInitialValues_WithActiveTrackAndLanguage_ReturnsTrackWithLanguage()
    {
        const string sub = "sub-studio-initial-owner";
        await CreateAccountAsync(sub, "studioinitialowner1");
        await GrantReadStudioProblemsPermissionAsync(sub);
        await Seeder.CreateLanguageVersionAsync("python", judge0Id: 71);

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Get, InitialValuesUrl, sub);
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        InitialStudioProblemSetupDto? dto = await response.Content.ReadFromJsonAsync<InitialStudioProblemSetupDto>(
            JsonOptions
        );

        Assert.NotNull(dto);
        TrackDto track = Assert.Single(dto.AvailableTracks, t => t.Name == "python");
        TrackLanguageDto language = Assert.Single(track.Languages);
        Assert.Equal("python", language.Name);
    }

    [Fact]
    public async Task GetInitialValues_Unauthenticated_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await Client.GetAsync(InitialValuesUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInitialValues_WithoutPermission_ReturnsForbidden()
    {
        const string sub = "sub-studio-initial-noperm";
        await CreateAccountAsync(sub, "studioinitialnoperm1");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Get, InitialValuesUrl, sub);
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task GrantReadStudioProblemsPermissionAsync(string sub, Guid? existingPermissionId = null)
    {
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);
        Guid permissionId =
            existingPermissionId
            ?? await Seeder.CreatePermissionAsync(WellKnownAuthorization.ReadStudioProblemsPermission);
        Guid roleId = await Seeder.CreateRoleAsync($"role-{sub}");
        await Seeder.GrantRolePermissionAsync(roleId, permissionId, DecisionEffect.Allow);
        Guid groupId = await Seeder.CreateGroupAsync($"group-{sub}");
        await Seeder.GrantGroupRoleAsync(groupId, roleId);
        await Seeder.AddUserToGroupAsync(userId, groupId);
    }

    private async Task CreateAccountAsync(string sub, string username)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        request.Content = JsonContent.Create(new UpsertUserRequest(username, null, null));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}