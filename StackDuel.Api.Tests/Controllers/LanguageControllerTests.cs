using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StackDuel.Api.Controllers;
using StackDuel.Application.Languages.Dtos;
using StackDuel.Application.Services.Languages;

namespace StackDuel.Api.Tests.Controllers;

public class LanguageControllerTests
{
    [Fact]
    public async Task GetLanguages_ReturnsTheServicesLanguages()
    {
        List<LanguageDto> languages = [new(Guid.NewGuid(), "Python", [new LanguageVersionDto(Guid.NewGuid(), "3.12")])];
        Mock<ILanguageService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<LanguageDto>>.Success(languages));

        ActionResult<IReadOnlyList<LanguageDto>> result = await ControllerAssert
            .WithHttpContext(new LanguageController(service.Object))
            .GetLanguages(CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Equal(languages, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetLanguages_PassesTheCancellationTokenThrough()
    {
        using CancellationTokenSource cts = new();
        Mock<ILanguageService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<LanguageDto>>.Success([]));

        await ControllerAssert.WithHttpContext(new LanguageController(service.Object)).GetLanguages(cts.Token);

        service.Verify(x => x.GetAllAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetLanguages_ServiceFailure_IsSurfacedAsAnErrorStatus()
    {
        Mock<ILanguageService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<LanguageDto>>.NotFound());

        ActionResult<IReadOnlyList<LanguageDto>> result = await ControllerAssert
            .WithHttpContext(new LanguageController(service.Object))
            .GetLanguages(CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, ControllerAssert.StatusCodeOf(result));
    }
}