using Ardalis.Result;
using Moq;
using StackDuel.Application.Campaigns;
using StackDuel.Application.Commands.Campaigns.AddCampaignModule;
using StackDuel.Application.Commands.Campaigns.AddCampaignUnit;
using StackDuel.Application.Commands.Campaigns.SetCampaignPrerequisites;
using StackDuel.Application.Commands.Campaigns.SetUnitProblems;
using StackDuel.Application.Commands.Campaigns.UpdateCampaignModule;
using StackDuel.Application.Commands.Campaigns.UpdateCampaignUnit;
using StackDuel.Application.Problems;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Tests.Commands.Campaigns;

internal static class CampaignFixture
{
    internal static Campaign CreateCampaign() => new("slug", "Title", "desc", CampaignDifficulty.Beginner);

    internal static (Campaign Campaign, CampaignModule Module) CreateCampaignWithModule()
    {
        Campaign campaign = CreateCampaign();
        return (campaign, campaign.AddModule("Module 1", "First module."));
    }

    internal static (Campaign Campaign, CampaignModule Module, CampaignUnit Unit) CreateCampaignWithUnit()
    {
        (Campaign campaign, CampaignModule module) = CreateCampaignWithModule();
        return (campaign, module, module.AddUnit("Unit 1", "Body.", UnitType.Lesson, 10));
    }

    internal static Mock<ICampaignWriteRepository> WriteRepositoryFor(Campaign campaign)
    {
        Mock<ICampaignWriteRepository> repository = new();
        repository.Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);
        return repository;
    }

    internal static Mock<ICampaignWriteRepository> EmptyWriteRepository()
    {
        Mock<ICampaignWriteRepository> repository = new();
        repository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);
        return repository;
    }
}

public class AddCampaignModuleHandlerTests
{
    private static AddCampaignModuleHandler Handler(Mock<ICampaignWriteRepository> repository) =>
        new(repository.Object, new AddCampaignModuleValidator());

    [Fact]
    public async Task Handle_ValidCommand_AddsTheModuleAndReturnsItsId()
    {
        Campaign campaign = CampaignFixture.CreateCampaign();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);

        Result<Guid> result = await Handler(repository)
            .Handle(new AddCampaignModuleCommand(campaign.Id, "Module 1", "desc"), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(campaign.Modules.Single().Id, result.Value);
        repository.Verify(x => x.SaveChangesAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        Result<Guid> result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(new AddCampaignModuleCommand(Guid.NewGuid(), "Module 1", "desc"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_BlankTitle_ReturnsInvalid()
    {
        Mock<ICampaignWriteRepository> repository = CampaignFixture.EmptyWriteRepository();

        Result<Guid> result = await Handler(repository)
            .Handle(new AddCampaignModuleCommand(Guid.NewGuid(), "  ", "desc"), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        repository.Verify(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyCampaignId_ReturnsInvalid()
    {
        Result<Guid> result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(new AddCampaignModuleCommand(Guid.Empty, "Module 1", "desc"), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_SecondModule_IsAppendedAfterTheFirst()
    {
        (Campaign campaign, CampaignModule first) = CampaignFixture.CreateCampaignWithModule();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);

        Result<Guid> result = await Handler(repository)
            .Handle(new AddCampaignModuleCommand(campaign.Id, "Module 2", "desc"), CancellationToken.None);

        Assert.Equal(2, campaign.Modules.Count);
        Assert.NotEqual(first.Id, result.Value);
    }
}

public class AddCampaignUnitHandlerTests
{
    private static AddCampaignUnitHandler Handler(Mock<ICampaignWriteRepository> repository) =>
        new(repository.Object, new AddCampaignUnitValidator());

    private static AddCampaignUnitCommand Command(Guid campaignId, Guid moduleId) =>
        new(campaignId, moduleId, "Unit 1", "Body.", UnitType.Lesson, 10);

    [Fact]
    public async Task Handle_ValidCommand_AddsTheUnitToTheModuleAndReturnsItsId()
    {
        (Campaign campaign, CampaignModule module) = CampaignFixture.CreateCampaignWithModule();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);

        Result<Guid> result = await Handler(repository).Handle(Command(campaign.Id, module.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(module.Units.Single().Id, result.Value);
        repository.Verify(x => x.SaveChangesAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        Result<Guid> result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(Command(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ModuleNotInTheCampaign_ReturnsNotFound()
    {
        Campaign campaign = CampaignFixture.CreateCampaign();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);

        Result<Guid> result = await Handler(repository)
            .Handle(Command(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NegativeEstimatedMinutes_ReturnsInvalid()
    {
        Result<Guid> result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(
                new AddCampaignUnitCommand(Guid.NewGuid(), Guid.NewGuid(), "Unit 1", "Body.", UnitType.Lesson, -1),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_UnitTypeOutsideTheEnum_ReturnsInvalid()
    {
        Result<Guid> result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(
                new AddCampaignUnitCommand(Guid.NewGuid(), Guid.NewGuid(), "Unit 1", "Body.", (UnitType)99, 10),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_EmptyModuleId_ReturnsInvalid()
    {
        Result<Guid> result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(Command(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }
}

public class UpdateCampaignModuleHandlerTests
{
    private static UpdateCampaignModuleHandler Handler(Mock<ICampaignWriteRepository> repository) =>
        new(repository.Object, new UpdateCampaignModuleValidator());

    [Fact]
    public async Task Handle_ValidCommand_UpdatesTheModuleAndSaves()
    {
        (Campaign campaign, CampaignModule module) = CampaignFixture.CreateCampaignWithModule();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);

        Result result = await Handler(repository)
            .Handle(
                new UpdateCampaignModuleCommand(campaign.Id, module.Id, "Renamed", "New description."),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Renamed", module.Title);
        repository.Verify(x => x.SaveChangesAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        Result result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(
                new UpdateCampaignModuleCommand(Guid.NewGuid(), Guid.NewGuid(), "Renamed", "desc"),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ModuleNotInTheCampaign_ReturnsNotFound()
    {
        Campaign campaign = CampaignFixture.CreateCampaign();

        Result result = await Handler(CampaignFixture.WriteRepositoryFor(campaign))
            .Handle(
                new UpdateCampaignModuleCommand(campaign.Id, Guid.NewGuid(), "Renamed", "desc"),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_BlankTitle_ReturnsInvalid()
    {
        Result result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(
                new UpdateCampaignModuleCommand(Guid.NewGuid(), Guid.NewGuid(), "", "desc"),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }
}

public class UpdateCampaignUnitHandlerTests
{
    private static UpdateCampaignUnitHandler Handler(Mock<ICampaignWriteRepository> repository) =>
        new(repository.Object, new UpdateCampaignUnitValidator());

    private static UpdateCampaignUnitCommand Command(Guid campaignId, Guid moduleId, Guid unitId) =>
        new(campaignId, moduleId, unitId, "Renamed", "New body.", UnitType.Quiz, 30);

    [Fact]
    public async Task Handle_ValidCommand_UpdatesTheUnitAndSaves()
    {
        (Campaign campaign, CampaignModule module, CampaignUnit unit) = CampaignFixture.CreateCampaignWithUnit();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);

        Result result = await Handler(repository)
            .Handle(Command(campaign.Id, module.Id, unit.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Renamed", unit.Title);
        Assert.Equal("New body.", unit.Content);
        Assert.Equal(UnitType.Quiz, unit.UnitType);
        Assert.Equal(30, unit.EstimatedMinutes);
        repository.Verify(x => x.SaveChangesAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        Result result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(Command(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ModuleNotInTheCampaign_ReturnsNotFound()
    {
        (Campaign campaign, _, CampaignUnit unit) = CampaignFixture.CreateCampaignWithUnit();

        Result result = await Handler(CampaignFixture.WriteRepositoryFor(campaign))
            .Handle(Command(campaign.Id, Guid.NewGuid(), unit.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_UnitNotInTheModule_ReturnsNotFound()
    {
        (Campaign campaign, CampaignModule module) = CampaignFixture.CreateCampaignWithModule();

        Result result = await Handler(CampaignFixture.WriteRepositoryFor(campaign))
            .Handle(Command(campaign.Id, module.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_NegativeEstimatedMinutes_ReturnsInvalid()
    {
        Result result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(
                new UpdateCampaignUnitCommand(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Renamed",
                    "body",
                    UnitType.Lesson,
                    -1
                ),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }
}

public class SetCampaignPrerequisitesHandlerTests
{
    private Mock<ICampaignReadRepository> _readRepository = null!;

    public SetCampaignPrerequisitesHandlerTests()
    {
        _readRepository = new Mock<ICampaignReadRepository>();
        _readRepository.Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private SetCampaignPrerequisitesHandler Handler(Mock<ICampaignWriteRepository> repository) =>
        new(_readRepository.Object, repository.Object, new SetCampaignPrerequisitesValidator());

    [Fact]
    public async Task Handle_AllPrerequisitesExist_SetsThemAndSaves()
    {
        Campaign campaign = CampaignFixture.CreateCampaign();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);
        Guid required = Guid.NewGuid();

        Result result = await Handler(repository)
            .Handle(new SetCampaignPrerequisitesCommand(campaign.Id, [required]), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(required, campaign.Prerequisites.Single().RequiredCampaignId);
        repository.Verify(x => x.SaveChangesAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyList_ClearsPrerequisites()
    {
        Campaign campaign = CampaignFixture.CreateCampaign();
        campaign.SetPrerequisites([Guid.NewGuid()]);
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);

        Result result = await Handler(repository)
            .Handle(new SetCampaignPrerequisitesCommand(campaign.Id, []), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(campaign.Prerequisites);
    }

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        Result result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(new SetCampaignPrerequisitesCommand(Guid.NewGuid(), [Guid.NewGuid()]), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_PrerequisiteDoesNotExist_ReturnsInvalidNamingTheMissingCampaign()
    {
        Campaign campaign = CampaignFixture.CreateCampaign();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);
        Guid missing = Guid.NewGuid();
        _readRepository.Setup(x => x.ExistsAsync(missing, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Result result = await Handler(repository)
            .Handle(new SetCampaignPrerequisitesCommand(campaign.Id, [missing]), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(missing.ToString(), result.ValidationErrors.Single().ErrorMessage);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateIds_AreOnlyCheckedOnce()
    {
        Campaign campaign = CampaignFixture.CreateCampaign();
        Guid required = Guid.NewGuid();

        await Handler(CampaignFixture.WriteRepositoryFor(campaign))
            .Handle(new SetCampaignPrerequisitesCommand(campaign.Id, [required, required]), CancellationToken.None);

        _readRepository.Verify(x => x.ExistsAsync(required, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CampaignListedAsItsOwnPrerequisite_ReturnsInvalid()
    {
        Campaign campaign = CampaignFixture.CreateCampaign();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);

        Result result = await Handler(repository)
            .Handle(new SetCampaignPrerequisitesCommand(campaign.Id, [campaign.Id]), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullList_ReturnsInvalid()
    {
        Result result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(new SetCampaignPrerequisitesCommand(Guid.NewGuid(), null!), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }
}

public class SetUnitProblemsHandlerTests
{
    private Mock<IProblemReadRepository> _problemReadRepository = null!;

    public SetUnitProblemsHandlerTests()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _problemReadRepository
            .Setup(x => x.ExistsForAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private SetUnitProblemsHandler Handler(Mock<ICampaignWriteRepository> repository) =>
        new(repository.Object, _problemReadRepository.Object, new SetUnitProblemsValidator());

    [Fact]
    public async Task Handle_AllProblemsExist_SetsThemAndSaves()
    {
        (Campaign campaign, CampaignModule module, CampaignUnit unit) = CampaignFixture.CreateCampaignWithUnit();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);
        Guid problemId = Guid.NewGuid();

        Result result = await Handler(repository)
            .Handle(new SetUnitProblemsCommand(campaign.Id, module.Id, unit.Id, [problemId]), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(problemId, unit.Problems.Single().ProblemId);
        repository.Verify(x => x.SaveChangesAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyList_ClearsTheUnitsProblems()
    {
        (Campaign campaign, CampaignModule module, CampaignUnit unit) = CampaignFixture.CreateCampaignWithUnit();
        unit.SetProblems([Guid.NewGuid()]);

        Result result = await Handler(CampaignFixture.WriteRepositoryFor(campaign))
            .Handle(new SetUnitProblemsCommand(campaign.Id, module.Id, unit.Id, []), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(unit.Problems);
    }

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        Result result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(
                new SetUnitProblemsCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()]),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ModuleNotInTheCampaign_ReturnsNotFound()
    {
        (Campaign campaign, _, CampaignUnit unit) = CampaignFixture.CreateCampaignWithUnit();

        Result result = await Handler(CampaignFixture.WriteRepositoryFor(campaign))
            .Handle(
                new SetUnitProblemsCommand(campaign.Id, Guid.NewGuid(), unit.Id, [Guid.NewGuid()]),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_UnitNotInTheModule_ReturnsNotFound()
    {
        (Campaign campaign, CampaignModule module) = CampaignFixture.CreateCampaignWithModule();

        Result result = await Handler(CampaignFixture.WriteRepositoryFor(campaign))
            .Handle(
                new SetUnitProblemsCommand(campaign.Id, module.Id, Guid.NewGuid(), [Guid.NewGuid()]),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemDoesNotExist_ReturnsInvalidNamingTheMissingProblem()
    {
        (Campaign campaign, CampaignModule module, CampaignUnit unit) = CampaignFixture.CreateCampaignWithUnit();
        Mock<ICampaignWriteRepository> repository = CampaignFixture.WriteRepositoryFor(campaign);
        Guid missing = Guid.NewGuid();
        _problemReadRepository
            .Setup(x => x.ExistsForAdminAsync(missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result result = await Handler(repository)
            .Handle(new SetUnitProblemsCommand(campaign.Id, module.Id, unit.Id, [missing]), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(missing.ToString(), result.ValidationErrors.Single().ErrorMessage);
        Assert.Empty(unit.Problems);
    }

    [Fact]
    public async Task Handle_DuplicateProblemIds_AreOnlyCheckedOnce()
    {
        (Campaign campaign, CampaignModule module, CampaignUnit unit) = CampaignFixture.CreateCampaignWithUnit();
        Guid problemId = Guid.NewGuid();

        await Handler(CampaignFixture.WriteRepositoryFor(campaign))
            .Handle(
                new SetUnitProblemsCommand(campaign.Id, module.Id, unit.Id, [problemId, problemId]),
                CancellationToken.None
            );

        _problemReadRepository.Verify(x => x.ExistsForAdminAsync(problemId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullProblemList_ReturnsInvalid()
    {
        Result result = await Handler(CampaignFixture.EmptyWriteRepository())
            .Handle(
                new SetUnitProblemsCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!),
                CancellationToken.None
            );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }
}