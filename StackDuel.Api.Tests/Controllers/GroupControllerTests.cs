using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StackDuel.Api.Controllers;
using StackDuel.Application.Groups.Dtos;
using StackDuel.Application.Services.Groups;

namespace StackDuel.Api.Tests.Controllers;

public class GroupControllerTests
{
    [Fact]
    public async Task GetGroups_ReturnsTheServicesGroups()
    {
        List<GroupDto> groups = [new(Guid.NewGuid(), "admin")];
        Mock<IGroupService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GroupDto>>.Success(groups));

        ActionResult<IReadOnlyList<GroupDto>> result = await ControllerAssert
            .WithHttpContext(new GroupController(service.Object))
            .GetGroups(CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Equal(groups, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetGroups_PassesTheCancellationTokenThrough()
    {
        using CancellationTokenSource cts = new();
        Mock<IGroupService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GroupDto>>.Success([]));

        await ControllerAssert.WithHttpContext(new GroupController(service.Object)).GetGroups(cts.Token);

        service.Verify(x => x.GetAllAsync(cts.Token), Times.Once);
    }
}