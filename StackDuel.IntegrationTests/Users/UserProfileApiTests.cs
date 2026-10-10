using StackDuel.Api.Requests.User;
using StackDuel.Application.Users.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.Users;

public sealed class UserProfileApiTests(IntegrationTestEnvironment environment) : ApiIntegrationTestBase(environment)
{
    [Fact]
    public async Task GetProfile_returns_profile_for_an_existing_username()
    {
        const string sub = "sub-henry-1";
        HttpRequestMessage createRequest = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        createRequest.Content = JsonContent.Create(new UpsertUserRequest("henry9", null, "hello there"));
        HttpResponseMessage createResponse = await Client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        HttpResponseMessage response = await Client.GetAsync("/api/v1/user/profile/henry9");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UserProfileDto? profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal("henry9", profile!.Username);
        Assert.Equal("hello there", profile.Bio);
    }

    [Fact]
    public async Task GetProfile_returns_not_found_for_an_unknown_username()
    {
        HttpResponseMessage response = await Client.GetAsync("/api/v1/user/profile/no-such-user");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}