using Microsoft.AspNetCore.SignalR;
using Moq;
using StackDuel.Api.Hubs;

namespace StackDuel.Api.Tests.Hubs;

public class SignalRSubmissionNotificationServiceTests
{
    [Fact]
    public async Task NotifySubmissionCompletedAsync_SendsToTheCallersPersonalGroup()
    {
        Mock<IHubClients> clients = new();
        Mock<IClientProxy> group = new();
        clients.Setup(x => x.Group(It.IsAny<string>())).Returns(group.Object);

        Mock<IHubContext<SubmissionHub>> hubContext = new();
        hubContext.SetupGet(x => x.Clients).Returns(clients.Object);

        Guid userId = Guid.NewGuid();
        Guid submissionId = Guid.NewGuid();
        SignalRSubmissionNotificationService service = new(hubContext.Object);

        await service.NotifySubmissionCompletedAsync(userId, submissionId);

        clients.Verify(x => x.Group($"user:{userId}"), Times.Once);
        group.Verify(
            x =>
                x.SendCoreAsync(
                    SignalRSubmissionNotificationService.SubmissionCompletedMethod,
                    It.Is<object?[]>(args => args.Length == 1),
                    CancellationToken.None
                ),
            Times.Once
        );
    }
}
