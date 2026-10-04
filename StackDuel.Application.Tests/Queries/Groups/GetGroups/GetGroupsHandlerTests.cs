using Ardalis.Result;
using Moq;
using StackDuel.Application.Groups;
using StackDuel.Application.Groups.Dtos;
using GetGroupsHandler = StackDuel.Application.Queries.Groups.GetGroups.GetGroupsHandler;
using GetGroupsQuery = StackDuel.Application.Queries.Groups.GetGroups.GetGroupsQuery;

namespace StackDuel.Application.Tests.Queries.Groups.GetGroups;

public class GetGroupsHandlerTests
{
    private Mock<IGroupReadRepository> _groupReadRepository = null!;
    private GetGroupsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _groupReadRepository = new Mock<IGroupReadRepository>();
        _handler = new GetGroupsHandler(_groupReadRepository.Object);
    }

    [Test]
    public async Task Handle_NoGroups_ReturnsEmptyList()
    {
        _groupReadRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Result<IReadOnlyList<GroupDto>> result = await _handler.Handle(new GetGroupsQuery(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }

    [Test]
    public async Task Handle_ReturnsGroupsFromRepository()
    {
        var groups = new List<GroupDto> { new(Guid.NewGuid(), "Admins"), new(Guid.NewGuid(), "Beta Testers") };
        _groupReadRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(groups);

        Result<IReadOnlyList<GroupDto>> result = await _handler.Handle(new GetGroupsQuery(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(groups));
    }
}