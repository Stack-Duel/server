using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StackDuel.Api.Requests.Problem;
using StackDuel.Api.Requests.User;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.ExecutionPipelines;

namespace StackDuel.IntegrationTests.Problems;

public sealed class EditStudioProblemApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly string ValidQuestion = new('q', 60);

    private static UpdateStudioProblemMetadataRequest MetadataRequest(
        string title = "Edited Title",
        int difficulty = 900,
        int timeLimitMs = 1500,
        int memoryLimitMb = 128,
        IReadOnlyCollection<string>? tags = null
    ) => new(title, ValidQuestion, difficulty, timeLimitMs, memoryLimitMb, tags ?? ["arrays"]);

    [Fact]
    public async Task UpdateMetadata_Owner_PersistsTheNewValues()
    {
        const string sub = "sub-studio-edit-owner";
        await CreateAccountAsync(sub, "studioeditowner");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-edit-1", "Studio Edit Track 1");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Studio Edit Problem");

        HttpResponseMessage response = await SendMetadataAsync(sub, problemId, MetadataRequest());

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        StudioProblemDetailsDto details = await GetDetailsAsync(sub, problemId);

        Assert.Equal("Edited Title", details.Title);
        Assert.Equal(900, details.DifficultyValue);
        Assert.Equal(1500, details.TimeLimitMs);
        Assert.Equal(128, details.MemoryLimitMb);
    }

    [Fact]
    public async Task UpdateMetadata_NotOwner_ReturnsNotFoundAndLeavesTheProblemAlone()
    {
        const string ownerSub = "sub-studio-edit-owner2";
        const string otherSub = "sub-studio-edit-other2";
        await CreateAccountAsync(ownerSub, "studioeditowner2");
        await CreateAccountAsync(otherSub, "studioeditother2");
        await GrantStudioEditPermissionsAsync(ownerSub);
        await GrantStudioEditPermissionsAsync(otherSub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-edit-2", "Studio Edit Track 2");
        Guid problemId = await CreateProblemAsync(ownerSub, trackId, "Not Yours To Edit");

        HttpResponseMessage response = await SendMetadataAsync(otherSub, problemId, MetadataRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        StudioProblemDetailsDto details = await GetDetailsAsync(ownerSub, problemId);

        Assert.Equal("Not Yours To Edit", details.Title);
    }

    [Fact]
    public async Task UpdateMetadata_WithoutTheUpdatePermission_ReturnsForbidden()
    {
        const string sub = "sub-studio-edit-readonly";
        await CreateAccountAsync(sub, "studioeditreadonly");
        await GrantStudioEditPermissionsAsync(sub, includeUpdate: false);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-edit-3", "Studio Edit Track 3");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Read Only Problem");

        HttpResponseMessage response = await SendMetadataAsync(sub, problemId, MetadataRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMetadata_Unauthenticated_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/api/v1/studio/problems/{Guid.NewGuid()}",
            MetadataRequest()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMetadata_ReplacesTheTagsWithTheOnesSent()
    {
        const string sub = "sub-studio-edit-tags";
        await CreateAccountAsync(sub, "studioedittags");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-edit-tags", "Studio Edit Tags Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Tagged Problem");

        HttpResponseMessage response = await SendMetadataAsync(
            sub,
            problemId,
            MetadataRequest(tags: ["graphs", "bfs"])
        );

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        IReadOnlyList<string> tags = await Seeder.GetProblemTagNamesAsync(problemId);

        Assert.Equal(["bfs", "graphs"], tags);
    }

    [Fact]
    public async Task UpdateMetadata_ReturnsTheTagsOnTheProblemDetails()
    {
        const string sub = "sub-studio-edit-tags-read";
        await CreateAccountAsync(sub, "studioedittagsread");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-tags-read", "Studio Tags Read Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Tag Read Problem");

        StudioProblemDetailsDto details = await GetDetailsAsync(sub, problemId);

        Assert.NotNull(details.Tags);
        Assert.Equal(["arrays"], details.Tags);
        Assert.Equal(trackId, details.TrackId);
    }

    [Fact]
    public async Task UpdateMetadata_InvalidTitle_ReturnsBadRequest()
    {
        const string sub = "sub-studio-edit-invalid";
        await CreateAccountAsync(sub, "studioeditinvalid");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-edit-4", "Studio Edit Track 4");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Invalid Edit Problem");

        HttpResponseMessage response = await SendMetadataAsync(sub, problemId, MetadataRequest(title: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddSetup_Owner_AddsTheLanguage()
    {
        const string sub = "sub-studio-add-setup";
        await CreateAccountAsync(sub, "studioaddsetup");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-add-setup", "Studio Add Setup Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Add Setup Problem");

        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid languageVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(
            trackId,
            "studio-add-setup-python",
            judge0Id: 71
        );

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/studio/problems/{problemId}/setups",
            sub
        );
        request.Content = JsonContent.Create(new AddProblemSetupRequest(languageVersionId));

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        StudioProblemDetailsDto details = await GetDetailsAsync(sub, problemId);

        Assert.Equal(1, details.SetupCount);
    }

    [Fact]
    public async Task AddSetup_LanguageFromAnotherTrack_ReturnsBadRequestAndAddsNothing()
    {
        const string sub = "sub-studio-add-setup-offtrack";
        await CreateAccountAsync(sub, "studiooffstrack");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-sql", "Studio SQL Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "SQL Track Problem");

        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid otherTrackId = await Seeder.CreateTrackAsync("track-studio-general", "Studio General Track");
        Guid offTrackVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(
            otherTrackId,
            "studio-offtrack-python",
            judge0Id: 71
        );

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/studio/problems/{problemId}/setups",
            sub
        );
        request.Content = JsonContent.Create(new AddProblemSetupRequest(offTrackVersionId));

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        StudioProblemDetailsDto details = await GetDetailsAsync(sub, problemId);

        Assert.Equal(0, details.SetupCount);
    }

    [Fact]
    public async Task UpsertSetupSolution_LanguageFromAnotherTrack_ReturnsBadRequest()
    {
        const string sub = "sub-studio-upsert-offtrack";
        await CreateAccountAsync(sub, "studioupsertoff");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-upsert-sql", "Studio Upsert SQL Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Upsert Off Track Problem");

        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid otherTrackId = await Seeder.CreateTrackAsync("track-studio-upsert-general", "Studio Upsert General");
        Guid offTrackVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(
            otherTrackId,
            "studio-upsert-offtrack-python",
            judge0Id: 71
        );

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/studio/problems/{problemId}/setups/reference-solution",
            sub
        );
        request.Content = JsonContent.Create(
            new UpsertStudioProblemSetupSolutionRequest(
                offTrackVersionId,
                StarterCode: "def solve():",
                FunctionName: "solve",
                ReferenceSolution: "def solve(): return 1"
            )
        );

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddSetup_AdminRoute_StillAllowsAnyLanguageRegardlessOfTrack()
    {
        const string sub = "sub-admin-add-setup-offtrack";
        await CreateAccountAsync(sub, "adminoffstrack");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-admin-sql", "Admin SQL Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Admin Off Track Problem");

        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid otherTrackId = await Seeder.CreateTrackAsync("track-admin-general", "Admin General Track");
        Guid offTrackVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(
            otherTrackId,
            "admin-offtrack-python",
            judge0Id: 71
        );

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/problem/admin/{problemId}/setups",
            sub
        );
        request.Content = JsonContent.Create(new AddProblemSetupRequest(offTrackVersionId));

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AddSetup_NotOwner_ReturnsNotFound()
    {
        const string ownerSub = "sub-studio-add-setup-owner";
        const string otherSub = "sub-studio-add-setup-other";
        await CreateAccountAsync(ownerSub, "studioaddsetupowner");
        await CreateAccountAsync(otherSub, "studioaddsetupother");
        await GrantStudioEditPermissionsAsync(ownerSub);
        await GrantStudioEditPermissionsAsync(otherSub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-add-setup-2", "Studio Add Setup Track 2");
        Guid problemId = await CreateProblemAsync(ownerSub, trackId, "Add Setup Not Yours");

        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid languageVersionId = await Seeder.CreateLanguageVersionAsync("javascript", judge0Id: 93);

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/studio/problems/{problemId}/setups",
            otherSub
        );
        request.Content = JsonContent.Create(new AddProblemSetupRequest(languageVersionId));

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpsertSetupSolution_Owner_StoresTheStarterCodeAndSolution()
    {
        const string sub = "sub-studio-upsert-solution";
        await CreateAccountAsync(sub, "studioupsertsolution");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-upsert", "Studio Upsert Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Upsert Solution Problem");

        await Seeder.EnsureExecutionPipelineAsync(WellKnownExecutionPipelines.DefaultName);
        Guid languageVersionId = await Seeder.CreateLanguageVersionOnTrackAsync(
            trackId,
            "studio-upsert-python",
            judge0Id: 71
        );

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/studio/problems/{problemId}/setups/reference-solution",
            sub
        );
        request.Content = JsonContent.Create(
            new UpsertStudioProblemSetupSolutionRequest(
                languageVersionId,
                StarterCode: "def solve(nums):",
                FunctionName: "solve",
                ReferenceSolution: "def solve(nums): return sum(nums)"
            )
        );

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        HttpRequestMessage setupsRequest = AuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/studio/problems/{problemId}/setups",
            sub
        );
        HttpResponseMessage setupsResponse = await Client.SendAsync(setupsRequest);
        IReadOnlyList<StudioProblemDetailsSetupDto>? setups = await setupsResponse.Content.ReadFromJsonAsync<
            IReadOnlyList<StudioProblemDetailsSetupDto>
        >(JsonOptions);

        Assert.NotNull(setups);
        StudioProblemDetailsSetupDto setup = Assert.Single(setups);
        Assert.Equal("def solve(nums):", setup.StarterCode);
        Assert.Equal("solve", setup.FunctionName);
        Assert.Equal("def solve(nums): return sum(nums)", setup.ReferenceSolution);
    }

    [Fact]
    public async Task SetGenerationParameters_Owner_RoundTripsThroughTheStudioDetails()
    {
        const string sub = "sub-studio-generation";
        await CreateAccountAsync(sub, "studiogeneration");
        await GrantStudioEditPermissionsAsync(sub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-generation", "Studio Generation Track");
        Guid problemId = await CreateProblemAsync(sub, trackId, "Generation Problem");

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/studio/problems/{problemId}/generation-parameters",
            sub
        );
        request.Content = JsonContent.Create(
            new SetProblemGenerationParametersRequest(
                [new GenerationParameterRequestItem("nums", "integer_array", 0, 100, 1, 50, null)],
                OutputValueType: "integer",
                TargetCaseCount: 25,
                Seed: 4242
            )
        );

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        StudioProblemDetailsDto details = await GetDetailsAsync(sub, problemId);

        Assert.Equal("integer", details.GenerationOutputValueType);
        Assert.Equal(25, details.GenerationTargetCaseCount);
        Assert.Equal(4242, details.GenerationSeed);
        Assert.NotNull(details.GenerationParameters);
        Assert.Equal("nums", Assert.Single(details.GenerationParameters).Name);
    }

    [Fact]
    public async Task SetGenerationParameters_NotOwner_ReturnsNotFound()
    {
        const string ownerSub = "sub-studio-generation-owner";
        const string otherSub = "sub-studio-generation-other";
        await CreateAccountAsync(ownerSub, "studiogenowner");
        await CreateAccountAsync(otherSub, "studiogenother");
        await GrantStudioEditPermissionsAsync(ownerSub);
        await GrantStudioEditPermissionsAsync(otherSub);
        Guid trackId = await Seeder.CreateTrackAsync("track-studio-generation-2", "Studio Generation Track 2");
        Guid problemId = await CreateProblemAsync(ownerSub, trackId, "Generation Not Yours");

        HttpRequestMessage request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/studio/problems/{problemId}/generation-parameters",
            otherSub
        );
        request.Content = JsonContent.Create(
            new SetProblemGenerationParametersRequest(
                [new GenerationParameterRequestItem("nums", "integer", 0, 10, null, null, null)],
                OutputValueType: "integer",
                TargetCaseCount: 10,
                Seed: 1
            )
        );

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendMetadataAsync(
        string sub,
        Guid problemId,
        UpdateStudioProblemMetadataRequest body
    )
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, $"/api/v1/studio/problems/{problemId}", sub);
        request.Content = JsonContent.Create(body);

        return await Client.SendAsync(request);
    }

    private async Task<StudioProblemDetailsDto> GetDetailsAsync(string sub, Guid problemId)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Get, $"/api/v1/studio/problems/{problemId}", sub);
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        StudioProblemDetailsDto? details = await response.Content.ReadFromJsonAsync<StudioProblemDetailsDto>(
            JsonOptions
        );

        Assert.NotNull(details);
        return details;
    }

    private async Task GrantStudioEditPermissionsAsync(string sub, bool includeUpdate = true)
    {
        Guid userId = await Seeder.GetUserIdBySubAsync(sub);
        Guid createPermissionId = await Seeder.EnsurePermissionAsync(
            WellKnownAuthorization.CreateAdminProblemsPermission
        );
        Guid readStudioPermissionId = await Seeder.EnsurePermissionAsync(
            WellKnownAuthorization.ReadStudioProblemsPermission
        );
        Guid roleId = await Seeder.CreateRoleAsync($"role-{sub}");
        await Seeder.GrantRolePermissionAsync(roleId, createPermissionId, DecisionEffect.Allow);
        await Seeder.GrantRolePermissionAsync(roleId, readStudioPermissionId, DecisionEffect.Allow);

        if (includeUpdate)
        {
            Guid updateStudioPermissionId = await Seeder.EnsurePermissionAsync(
                WellKnownAuthorization.UpdateStudioProblemsPermission
            );
            await Seeder.GrantRolePermissionAsync(roleId, updateStudioPermissionId, DecisionEffect.Allow);
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
