using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Submissions.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StackDuel.IntegrationTests.Problems;

public sealed class PublishedProblemReadsApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly string PublishedQuestion = new('p', 60);
    private static readonly string EditedQuestion = new('e', 60);

    [Fact]
    public async Task ProblemList_ShowsThePublishedTitleRatherThanTheUnpublishedEdit()
    {
        const string sub = "sub-published-reads-list";
        await CreateAccountAsync(sub, "publishedreadslist");
        await GrantStudioPermissionsAsync(sub);

        Guid trackId = await Seeder.CreateTrackAsync("track-published-reads", "Published Reads Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Vowel Counter Published");
        await AddSetupAsync(sub, problemId, trackId, "published-reads-python");

        await Seeder.PublishProblemVersionAsync(problemId, "Vowel Counter Published", PublishedQuestion);
        await Seeder.MarkProblemPublishedAsync(problemId);

        await EditProblemAsync(sub, problemId, "Vowel Counter Edited", EditedQuestion);

        ProblemListRowDto row = await FindListRowAsync("Vowel Counter");

        Assert.Equal("Vowel Counter Published", row.Title);
    }

    [Fact]
    public async Task ProblemList_SearchMatchesThePublishedTitle()
    {
        const string sub = "sub-published-reads-search";
        await CreateAccountAsync(sub, "publishedreadssearch");
        await GrantStudioPermissionsAsync(sub);

        Guid trackId = await Seeder.CreateTrackAsync("track-published-search", "Published Search Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Anagram Checker Published");
        await AddSetupAsync(sub, problemId, trackId, "published-search-python");

        await Seeder.PublishProblemVersionAsync(problemId, "Anagram Checker Published", PublishedQuestion);
        await Seeder.MarkProblemPublishedAsync(problemId);

        await EditProblemAsync(sub, problemId, "Totally Different Draft Name", EditedQuestion);

        PageResult<ProblemListRowDto> published = await GetListAsync("Anagram Checker Published");
        PageResult<ProblemListRowDto> draftName = await GetListAsync("Totally Different Draft Name");

        Assert.Contains(published.Results, row => row.Id == problemId);
        Assert.DoesNotContain(draftName.Results, row => row.Id == problemId);
    }

    [Fact]
    public async Task ProblemList_UnversionedProblem_StillShowsItsOwnTitle()
    {
        const string sub = "sub-published-reads-legacy";
        await CreateAccountAsync(sub, "publishedreadslegacy");
        await GrantStudioPermissionsAsync(sub);

        Guid trackId = await Seeder.CreateTrackAsync("track-published-legacy", "Published Legacy Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Legacy Unversioned Problem");
        await AddSetupAsync(sub, problemId, trackId, "published-legacy-python");

        await Seeder.MarkProblemPublishedAsync(problemId);

        ProblemListRowDto row = await FindListRowAsync("Legacy Unversioned Problem");

        Assert.Equal("Legacy Unversioned Problem", row.Title);
    }

    private async Task<ProblemListRowDto> FindListRowAsync(string search)
    {
        PageResult<ProblemListRowDto> page = await GetListAsync(search);

        return Assert.Single(page.Results);
    }

    private async Task<PageResult<ProblemListRowDto>> GetListAsync(string search)
    {
        HttpResponseMessage response = await Client.GetAsync(
            $"/api/v1/problem?page=1&size=20&search={Uri.EscapeDataString(search)}"
        );

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        PageResult<ProblemListRowDto>? page = await response.Content.ReadFromJsonAsync<PageResult<ProblemListRowDto>>();

        Assert.NotNull(page);

        return page;
    }

    private async Task AddSetupAsync(string sub, Guid problemId, Guid trackId, string languageName)
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

    [Fact]
    public async Task Profile_ShowsTheProblemTitleTheSubmissionRanAgainst()
    {
        const string sub = "sub-published-reads-profile";
        const string username = "pubreadsprofile";
        await CreateAccountAsync(sub, username);
        await GrantStudioPermissionsAsync(sub);
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);

        Guid trackId = await Seeder.CreateTrackAsync("track-published-profile", "Published Profile Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Profile Title As Published");
        await AddSetupAsync(sub, problemId, trackId, "published-profile-python");
        Guid setupId = await Seeder.GetFirstProblemSetupIdAsync(problemId);

        Guid versionId = await Seeder.PublishProblemVersionAsync(
            problemId,
            "Profile Title As Published",
            PublishedQuestion
        );
        await Seeder.MarkProblemPublishedAsync(problemId);

        Guid submissionId = await Seeder.CreateSubmissionAsync(userId, setupId, SubmissionStatus.Accepted);
        await Seeder.PinSubmissionToVersionAsync(submissionId, versionId);

        await EditProblemAsync(sub, problemId, "Profile Title Renamed Later", EditedQuestion);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/user/profile/{username}");

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        UserProfileDto? profile = await response.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);

        Assert.NotNull(profile);

        ProfileSubmissionDto submission = Assert.Single(profile.RecentSubmissions, s => s.Id == submissionId);

        Assert.Equal("Profile Title As Published", submission.ProblemTitle);
    }
}