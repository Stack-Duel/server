using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StackDuel.Api.Requests.User;
using StackDuel.Application.Games;
using StackDuel.Application.Users;
using StackDuel.Infrastructure.Announcements;
using System.Net;
using System.Net.Http.Json;

namespace StackDuel.IntegrationTests.Games;

public sealed class GameAnnouncementApiTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private sealed class RecordingWebhookSender : IDiscordWebhookSender
    {
        public List<Embed> Sent { get; } = [];

        public Task SendAsync(Embed embed, CancellationToken cancellationToken = default)
        {
            Sent.Add(embed);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AnnounceGameCreated_MultiplayerMode_SendsAnEmbedNamingTheHostAndMode()
    {
        const string sub = "sub-announce-duel";
        await CreateAccountAsync(sub, "duelhost");
        Guid hostUserId = await Seeder.GetUserIdBySubAsync(sub);

        Guid poolId = await Seeder.CreateProblemPoolAsync("announce-pool-duel");
        Guid gameModeId = await Seeder.CreateGameModeAsync("duel-announce", "Duel", 2, 2, poolId);
        Guid gameId = await Seeder.CreatePendingGameAsync(gameModeId, poolId, hostUserId);

        RecordingWebhookSender sender = await AnnounceAsync(gameId);

        Embed embed = Assert.Single(sender.Sent);
        Assert.Equal("New game created", embed.Title);
        Assert.Equal("duelhost", embed.Fields.Single(f => f.Name == "Host").Value);
        Assert.Equal("Duel", embed.Fields.Single(f => f.Name == "Mode").Value);
        Assert.Equal("10 min", embed.Fields.Single(f => f.Name == "Time limit").Value);
    }

    [Fact]
    public async Task AnnounceGameCreated_SinglePlayerMode_SendsNothing()
    {
        const string sub = "sub-announce-solo";
        await CreateAccountAsync(sub, "solohost");
        Guid hostUserId = await Seeder.GetUserIdBySubAsync(sub);

        Guid poolId = await Seeder.CreateProblemPoolAsync("announce-pool-solo");
        Guid gameModeId = await Seeder.CreateGameModeAsync("solo-rush-announce", "Solo Rush", 1, 1, poolId);
        Guid gameId = await Seeder.CreatePendingGameAsync(gameModeId, poolId, hostUserId);

        RecordingWebhookSender sender = await AnnounceAsync(gameId);

        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task AnnounceGameCreated_GameNoLongerExists_SendsNothing()
    {
        RecordingWebhookSender sender = await AnnounceAsync(Guid.NewGuid());

        Assert.Empty(sender.Sent);
    }

    private async Task<RecordingWebhookSender> AnnounceAsync(Guid gameId)
    {
        RecordingWebhookSender sender = new();

        using IServiceScope scope = environment.Factory.Services.CreateScope();
        DiscordGameAnnouncer announcer = new(
            sender,
            scope.ServiceProvider.GetRequiredService<IGameReadRepository>(),
            scope.ServiceProvider.GetRequiredService<IUserReadRepository>(),
            NullLogger<DiscordGameAnnouncer>.Instance
        );

        await announcer.AnnounceGameCreatedAsync(gameId);

        return sender;
    }

    private async Task CreateAccountAsync(string sub, string username)
    {
        HttpRequestMessage request = AuthenticatedRequest(HttpMethod.Put, "/api/v1/user", sub);
        request.Content = JsonContent.Create(new UpsertUserRequest(username, null, null));

        HttpResponseMessage response = await Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}