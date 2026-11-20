using System.Net;
using System.Net.Http.Json;
using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.IntegrationTests.Submissions;

public sealed class SubmissionProblemSnapshotApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static readonly string PublishedQuestion = new('p', 60);
    private static readonly string EditedQuestion = new('e', 60);

    [Fact]
    public async Task ProblemSnapshot_PinnedSubmission_ReturnsTheProblemAsItWasPublished()
    {
        const string sub = "sub-snapshot-pinned";
        await CreateAccountAsync(sub, "snapshotpinned");
        await GrantStudioPermissionsAsync(sub);
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);

        Guid trackId = await Seeder.CreateTrackAsync("track-snapshot-pinned", "Snapshot Pinned Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Published Title");
        Guid setupId = await AddSetupAsync(sub, problemId, trackId, "snapshot-pinned-python");

        Guid versionId = await Seeder.PublishProblemVersionAsync(problemId, "Published Title", PublishedQuestion);

        Guid submissionId = await Seeder.CreateSubmissionAsync(userId, setupId, SubmissionStatus.Accepted);
        await Seeder.PinSubmissionToVersionAsync(submissionId, versionId);

        await EditProblemAsync(sub, problemId, "Edited Title", EditedQuestion);

        SubmissionProblemSnapshotDto snapshot = await GetSnapshotAsync(sub, submissionId);

        Assert.Equal(1, snapshot.VersionNumber);
        Assert.Equal("Published Title", snapshot.Title);
        Assert.Equal(PublishedQuestion, snapshot.Question);
        Assert.Equal(problemId, snapshot.ProblemId);
    }

    [Fact]
    public async Task ProblemSnapshot_UnpinnedSubmission_FallsBackToTheLiveProblem()
    {
        const string sub = "sub-snapshot-unpinned";
        await CreateAccountAsync(sub, "snapshotunpinned");
        await GrantStudioPermissionsAsync(sub);
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);

        Guid trackId = await Seeder.CreateTrackAsync("track-snapshot-unpinned", "Snapshot Unpinned Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Live Title");
        Guid setupId = await AddSetupAsync(sub, problemId, trackId, "snapshot-unpinned-python");

        Guid submissionId = await Seeder.CreateSubmissionAsync(userId, setupId, SubmissionStatus.Accepted);

        SubmissionProblemSnapshotDto snapshot = await GetSnapshotAsync(sub, submissionId);

        Assert.Null(snapshot.VersionNumber);
        Assert.False(snapshot.IsPinnedToAVersion);
        Assert.Equal("Live Title", snapshot.Title);
    }

    [Fact]
    public async Task ProblemSnapshot_AnotherUsersSubmission_ReturnsNotFound()
    {
        const string ownerSub = "sub-snapshot-owner";
        const string otherSub = "sub-snapshot-other";
        await CreateAccountAsync(ownerSub, "snapshotowner");
        await CreateAccountAsync(otherSub, "snapshotother");
        await GrantStudioPermissionsAsync(ownerSub);
        await GrantStudioPermissionsAsync(otherSub);
        Guid ownerId = await Seeder.GetUserIdBySubAsync(ownerSub);

        Guid trackId = await Seeder.CreateTrackAsync("track-snapshot-other", "Snapshot Other Track");
        Guid problemId = await CreateProblemAsync(ownerSub, trackId, "Hidden Title");
        Guid setupId = await AddSetupAsync(ownerSub, problemId, trackId, "snapshot-other-python");

        Guid submissionId = await Seeder.CreateSubmissionAsync(ownerId, setupId, SubmissionStatus.Accepted);

        HttpResponseMessage response = await Client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, $"/api/v1/submission/{submissionId}/problem-snapshot", otherSub)
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProblemSnapshot_Unauthenticated_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await Client.GetAsync($"/api/v1/submission/{Guid.NewGuid()}/problem-snapshot");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<SubmissionProblemSnapshotDto> GetSnapshotAsync(string sub, Guid submissionId)
    {
        HttpResponseMessage response = await Client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, $"/api/v1/submission/{submissionId}/problem-snapshot", sub)
        );

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        SubmissionProblemSnapshotDto? snapshot =
            await response.Content.ReadFromJsonAsync<SubmissionProblemSnapshotDto>();

        Assert.NotNull(snapshot);

        return snapshot;
    }

    private async Task<Guid> AddSetupAsync(string sub, Guid problemId, Guid trackId, string languageName)
    {
        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid languageVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(trackId, languageName, judge0Id: 71);

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/studio/problems/{problemId}/setups",
            sub
        );
        request.Content = JsonContent.Create(new AddProblemSetupRequest(languageVersionId));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return await Seeder.GetFirstProblemSetupIdAsync(problemId);
    }

    private async Task EditProblemAsync(string sub, Guid problemId, string title, string question)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, $"/api/v1/studio/problems/{problemId}", sub);
        request.Content = JsonContent.Create(
            new UpdateStudioProblemMetadataRequest(title, question, 900, 1500, 128, ["arrays"])
        );

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task<Guid> CreateProblemAsync(string sub, Guid trackId, string title)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/problem", sub);
        request.Content = JsonContent.Create(
            new CreateDraftProblemRequest(title, PublishedQuestion, ["arrays"], 100, 1000, 256, trackId)
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

    private async Task GrantStudioPermissionsAsync(string sub)
    {
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);
        Guid createPermissionId = await Seeder.EnsurePermissionAsync(
            WellKnownAuthorization.CreateAdminProblemsPermission
        );
        Guid readStudioPermissionId = await Seeder.EnsurePermissionAsync(
            WellKnownAuthorization.ReadStudioProblemsPermission
        );
        Guid updateStudioPermissionId = await Seeder.EnsurePermissionAsync(
            WellKnownAuthorization.UpdateStudioProblemsPermission
        );

        Guid roleId = await Seeder.CreateRoleAsync($"role-{sub}");
        await Seeder.GrantRolePermissionAsync(roleId, createPermissionId, DecisionEffect.Allow);
        await Seeder.GrantRolePermissionAsync(roleId, readStudioPermissionId, DecisionEffect.Allow);
        await Seeder.GrantRolePermissionAsync(roleId, updateStudioPermissionId, DecisionEffect.Allow);

        Guid groupId = await Seeder.CreateGroupAsync($"group-{sub}");
        await Seeder.GrantGroupRoleAsync(groupId, roleId);
        await Seeder.AddUserToGroupAsync(userId, groupId);
    }
}
