using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Submissions.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StackDuel.IntegrationTests.Problems;

public sealed class GetMyProblemsMetadataApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private const string MetadataUrl = "/api/v1/problem/mine/metadata";

    [Fact]
    public async Task GetMyProblemsMetadata_HappyPath_AggregatesAcrossOwnedProblems()
    {
        const string ownerSub = "sub-metadata-owner";
        await CreateAccountAsync(ownerSub, "metaowner1");

        Dictionary<string, Guid> permissions = [];
        await GrantPermissionsToNewGroupAsync(
            ownerSub,
            permissions,
            WellKnownAuthorization.CreateAdminProblemsPermission,
            WellKnownAuthorization.ReadAdminProblemsPermission
        );

        Guid problemId = await CreateProblemAsync(ownerSub, "Metadata Problem");
        Guid setupId = await AddSetupAsync(ownerSub, problemId);
        Guid ownerUserId = await Seeder.GetUserIdBySubAsync(ownerSub);

        await Seeder.CreateSubmissionAsync(ownerUserId, setupId, SubmissionStatus.Accepted);
        await Seeder.CreateSubmissionAsync(ownerUserId, setupId, SubmissionStatus.Accepted);
        await Seeder.CreateSubmissionAsync(ownerUserId, setupId, SubmissionStatus.WrongAnswer);
        await Seeder.CreateSubmissionAsync(ownerUserId, setupId, SubmissionStatus.Accepted, SubmissionType.Run);

        const string reactor1Sub = "sub-metadata-reactor1";
        const string reactor2Sub = "sub-metadata-reactor2";
        await CreateAccountAsync(reactor1Sub, "metareactor1");
        await CreateAccountAsync(reactor2Sub, "metareactor2");
        await Seeder.EnsureReactionTypeAsync("like", "Like", sortOrder: 1);
        await Seeder.EnsureReactionTypeAsync("dislike", "Dislike", sortOrder: 2);
        await ReactAsync(reactor1Sub, problemId, "like");
        await ReactAsync(reactor2Sub, problemId, "dislike");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Get, MetadataUrl, ownerSub);
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        MyProblemsMetadataDto? dto = await response.Content.ReadFromJsonAsync<MyProblemsMetadataDto>(JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(3, dto.TotalSubmissions);
        Assert.Equal(2, dto.TotalReactions);
        Assert.Equal(2.0 / 3.0, dto.AverageCompletionRate, 5);
        Assert.Equal(2, dto.ReactionBreakdown.Count);
        Assert.Contains(dto.ReactionBreakdown, x => x.Key == "like" && x.Count == 1);
        Assert.Contains(dto.ReactionBreakdown, x => x.Key == "dislike" && x.Count == 1);

        ProblemStatusCountDto statusCount = Assert.Single(dto.StatusBreakdown);
        Assert.Equal(ProblemStatus.Draft, statusCount.Status);
        Assert.Equal(1, statusCount.Count);
    }

    [Fact]
    public async Task GetMyProblemsMetadata_ScopesToCurrentUsersProblemsOnly()
    {
        const string ownerSub = "sub-metadata-scoped-owner";
        const string otherSub = "sub-metadata-scoped-other";
        await CreateAccountAsync(ownerSub, "scopedowner1");
        await CreateAccountAsync(otherSub, "scopedother1");

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

        Guid ownerProblemId = await CreateProblemAsync(ownerSub, "Scoped Owner Problem");
        Guid ownerSetupId = await AddSetupAsync(ownerSub, ownerProblemId);
        Guid ownerUserId = await Seeder.GetUserIdBySubAsync(ownerSub);
        await Seeder.CreateSubmissionAsync(ownerUserId, ownerSetupId, SubmissionStatus.Accepted);

        Guid otherProblemId = await CreateProblemAsync(otherSub, "Scoped Other Problem");
        Guid otherSetupId = await AddSetupAsync(otherSub, otherProblemId);
        Guid otherUserId = await Seeder.GetUserIdBySubAsync(otherSub);
        await Seeder.CreateSubmissionAsync(otherUserId, otherSetupId, SubmissionStatus.Accepted);
        await Seeder.CreateSubmissionAsync(otherUserId, otherSetupId, SubmissionStatus.Accepted);
        await Seeder.EnsureReactionTypeAsync("like", "Like", sortOrder: 1);
        await ReactAsync(otherSub, otherProblemId, "like");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Get, MetadataUrl, ownerSub);
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        MyProblemsMetadataDto? dto = await response.Content.ReadFromJsonAsync<MyProblemsMetadataDto>(JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(1, dto.TotalSubmissions);
        Assert.Equal(0, dto.TotalReactions);
        Assert.Empty(dto.ReactionBreakdown);
    }

    [Fact]
    public async Task GetMyProblemsMetadata_Unauthenticated_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await Client.GetAsync(MetadataUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyProblemsMetadata_WithoutPermission_ReturnsForbidden()
    {
        const string sub = "sub-metadata-noperm";
        await CreateAccountAsync(sub, "metanoperm1");

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Get, MetadataUrl, sub);
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> CreateProblemAsync(string sub, string title)
    {
        Guid trackId = await Seeder.CreateTrackAsync($"track-{Guid.NewGuid():N}", "Metadata Track");
        CreateDraftProblemRequest body = new(title, new string('q', 60), ["arrays"], 100, 1000, 256, trackId);

        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/problem", sub);
        request.Content = JsonContent.Create(body);

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
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task ReactAsync(string sub, Guid problemId, string reactionTypeKey)
    {
        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/problem/{problemId}/reaction",
            sub
        );
        request.Content = JsonContent.Create(new SetProblemReactionRequest(reactionTypeKey));

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
}