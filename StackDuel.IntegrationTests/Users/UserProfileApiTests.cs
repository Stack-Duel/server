using StackDuel.Api.Requests.User;
using StackDuel.Application.Users.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.Users;

public sealed class UserProfileApiTests : ApiIntegrationTestBase
{
    [Test]
    public async Task GetProfile_returns_profile_for_an_existing_username()
    {
        const string sub = "sub-henry-1";
        HttpRequestMessage createRequest = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        createRequest.Content = JsonContent.Create(new UpsertUserRequest("henry9", null, "hello there"));
        HttpResponseMessage createResponse = await Client.SendAsync(createRequest);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        HttpResponseMessage response = await Client.GetAsync("/api/v1/user/profile/henry9");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        UserProfileDto? profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.Username, Is.EqualTo("henry9"));
        Assert.That(profile.Bio, Is.EqualTo("hello there"));
    }

    [Test]
    public async Task GetProfile_returns_not_found_for_an_unknown_username()
    {
        HttpResponseMessage response = await Client.GetAsync("/api/v1/user/profile/no-such-user");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}