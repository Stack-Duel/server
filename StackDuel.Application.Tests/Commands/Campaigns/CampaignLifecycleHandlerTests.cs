using Ardalis.Result;
using Moq;
using StackDuel.Application.Campaigns;
using StackDuel.Application.Commands.Campaigns.ArchiveCampaign;
using StackDuel.Application.Commands.Campaigns.CreateCampaign;
using StackDuel.Application.Commands.Campaigns.PublishCampaign;
using StackDuel.Application.Commands.Campaigns.UpdateCampaignDetails;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Tests.Commands.Campaigns;

public class CreateCampaignHandlerTests
{
    private Mock<ICampaignReadRepository> _readRepository = null!;
    private Mock<ICampaignWriteRepository> _writeRepository = null!;
    private CreateCampaignHandler _handler = null!;
    private Campaign? _added;

    [SetUp]
    public void SetUp()
    {
        _readRepository = new Mock<ICampaignReadRepository>();
        _writeRepository = new Mock<ICampaignWriteRepository>();
        _added = null;
        _writeRepository
            .Setup(x => x.AddAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()))
            .Callback<Campaign, CancellationToken>((campaign, _) => _added = campaign)
            .Returns(Task.CompletedTask);
        _handler = new CreateCampaignHandler(
            _readRepository.Object,
            _writeRepository.Object,
            new CreateCampaignValidator()
        );
    }

    private Task<Result<Guid>> Handle(string title = "Intro to Algorithms", string description = "Start here.") =>
        _handler.Handle(
            new CreateCampaignCommand(title, description, CampaignDifficulty.Beginner),
            CancellationToken.None
        );

    /// <summary>The campaign handed to the write repository.</summary>
    private Campaign CapturedCampaign()
    {
        Assert.That(_added, Is.Not.Null, "the handler did not add a campaign");
        return _added!;
    }

    [Test]
    public async Task Handle_ValidCommand_AddsTheCampaignAndReturnsItsId()
    {
        Result<Guid> result = await Handle();

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(result.Value, Is.EqualTo(CapturedCampaign().Id));
    }

    [Test]
    public async Task Handle_SlugIsFree_UsesTheSlugifiedTitle()
    {
        _readRepository
            .Setup(x => x.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Handle("Intro to Algorithms");

        Assert.That(CapturedCampaign().Slug, Is.EqualTo("intro-to-algorithms"));
    }

    [TestCase("Data Structures 101", "data-structures-101")]
    [TestCase("  Padded Title  ", "padded-title")]
    [TestCase("C++ & Rust!", "c-rust")]
    [TestCase("Multiple   Spaces", "multiple-spaces")]
    [TestCase("Trailing punctuation!!!", "trailing-punctuation")]
    public async Task Handle_SlugifiesTheTitle(string title, string expectedSlug)
    {
        _readRepository
            .Setup(x => x.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Handle(title);

        Assert.That(CapturedCampaign().Slug, Is.EqualTo(expectedSlug));
    }

    [Test]
    public async Task Handle_TitleWithNoAlphanumerics_FallsBackToALiteralSlug()
    {
        _readRepository
            .Setup(x => x.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Handle("!!!");

        Assert.That(CapturedCampaign().Slug, Is.EqualTo("campaign"));
    }

    [Test]
    public async Task Handle_SlugTaken_AppendsTheFirstFreeNumericSuffix()
    {
        _readRepository
            .SetupSequence(x => x.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        await Handle("Intro to Algorithms");

        Assert.That(CapturedCampaign().Slug, Is.EqualTo("intro-to-algorithms-2"));
    }

    [Test]
    public async Task Handle_SeveralSlugsTaken_KeepsIncrementingTheSuffix()
    {
        _readRepository
            .SetupSequence(x => x.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        await Handle("Intro to Algorithms");

        Assert.That(CapturedCampaign().Slug, Is.EqualTo("intro-to-algorithms-4"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task Handle_BlankTitle_ReturnsInvalid(string title)
    {
        Result<Guid> result = await Handle(title);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _writeRepository.Verify(x => x.AddAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_TitleOverTwoHundredCharacters_ReturnsInvalid()
    {
        Result<Guid> result = await Handle(new string('a', 201));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_NullDescription_ReturnsInvalid()
    {
        Result<Guid> result = await Handle(description: null!);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_DescriptionOverFourThousandCharacters_ReturnsInvalid()
    {
        Result<Guid> result = await Handle(description: new string('a', 4001));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_DifficultyOutsideTheEnum_ReturnsInvalid()
    {
        Result<Guid> result = await _handler.Handle(
            new CreateCampaignCommand("Title", "desc", (CampaignDifficulty)99),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_NewCampaignStartsAsADraft()
    {
        await Handle();

        Assert.That(CapturedCampaign().Status, Is.EqualTo(CampaignStatus.Draft));
    }
}

public class PublishCampaignHandlerTests
{
    private Mock<ICampaignWriteRepository> _writeRepository = null!;
    private PublishCampaignHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _writeRepository = new Mock<ICampaignWriteRepository>();
        _handler = new PublishCampaignHandler(_writeRepository.Object, new PublishCampaignValidator());
    }

    private static Campaign CreateCampaign() => new("slug", "Title", "desc", CampaignDifficulty.Beginner);

    private Task<Result> Handle(Guid campaignId) =>
        _handler.Handle(new PublishCampaignCommand(campaignId), CancellationToken.None);

    [Test]
    public async Task Handle_DraftCampaign_PublishesItAndSaves()
    {
        Campaign campaign = CreateCampaign();
        _writeRepository.Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        Result result = await Handle(campaign.Id);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Published));
        _writeRepository.Verify(x => x.SaveChangesAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        _writeRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        Result result = await Handle(Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_AlreadyPublished_ReturnsInvalidAndDoesNotSave()
    {
        Campaign campaign = CreateCampaign();
        campaign.Publish();
        _writeRepository.Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        Result result = await Handle(campaign.Id);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _writeRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_ArchivedCampaign_ReturnsInvalidCarryingTheDomainMessage()
    {
        Campaign campaign = CreateCampaign();
        campaign.Archive();
        _writeRepository.Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        Result result = await Handle(campaign.Id);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.ValidationErrors.Single().ErrorMessage, Does.Contain("draft"));
    }

    [Test]
    public async Task Handle_EmptyCampaignId_ReturnsInvalid()
    {
        Result result = await Handle(Guid.Empty);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _writeRepository.Verify(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class ArchiveCampaignHandlerTests
{
    private Mock<ICampaignWriteRepository> _writeRepository = null!;
    private ArchiveCampaignHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _writeRepository = new Mock<ICampaignWriteRepository>();
        _handler = new ArchiveCampaignHandler(_writeRepository.Object, new ArchiveCampaignValidator());
    }

    private static Campaign CreateCampaign() => new("slug", "Title", "desc", CampaignDifficulty.Beginner);

    private Task<Result> Handle(Guid campaignId) =>
        _handler.Handle(new ArchiveCampaignCommand(campaignId), CancellationToken.None);

    [Test]
    public async Task Handle_DraftCampaign_ArchivesItAndSaves()
    {
        Campaign campaign = CreateCampaign();
        _writeRepository.Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        Result result = await Handle(campaign.Id);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Archived));
        _writeRepository.Verify(x => x.SaveChangesAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_PublishedCampaign_ArchivesIt()
    {
        Campaign campaign = CreateCampaign();
        campaign.Publish();
        _writeRepository.Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        Result result = await Handle(campaign.Id);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Archived));
    }

    [Test]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        _writeRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        Result result = await Handle(Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_AlreadyArchived_ReturnsInvalidAndDoesNotSave()
    {
        Campaign campaign = CreateCampaign();
        campaign.Archive();
        _writeRepository.Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        Result result = await Handle(campaign.Id);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _writeRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_EmptyCampaignId_ReturnsInvalid()
    {
        Assert.That((await Handle(Guid.Empty)).Status, Is.EqualTo(ResultStatus.Invalid));
    }
}

public class UpdateCampaignDetailsHandlerTests
{
    private Mock<ICampaignWriteRepository> _writeRepository = null!;
    private UpdateCampaignDetailsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _writeRepository = new Mock<ICampaignWriteRepository>();
        _handler = new UpdateCampaignDetailsHandler(_writeRepository.Object, new UpdateCampaignDetailsValidator());
    }

    private static Campaign CreateCampaign() => new("slug", "Title", "desc", CampaignDifficulty.Beginner);

    private Task<Result> Handle(
        Guid campaignId,
        string title = "Renamed",
        string description = "New description.",
        string? iconKey = "icon"
    ) =>
        _handler.Handle(
            new UpdateCampaignDetailsCommand(campaignId, title, description, CampaignDifficulty.Expert, iconKey),
            CancellationToken.None
        );

    [Test]
    public async Task Handle_ValidCommand_UpdatesTheCampaignAndSaves()
    {
        Campaign campaign = CreateCampaign();
        _writeRepository.Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        Result result = await Handle(campaign.Id);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.Multiple(() =>
        {
            Assert.That(campaign.Title, Is.EqualTo("Renamed"));
            Assert.That(campaign.Description, Is.EqualTo("New description."));
            Assert.That(campaign.Difficulty, Is.EqualTo(CampaignDifficulty.Expert));
            Assert.That(campaign.IconKey, Is.EqualTo("icon"));
        });
        _writeRepository.Verify(x => x.SaveChangesAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_NullIconKey_IsAccepted()
    {
        Campaign campaign = CreateCampaign();
        _writeRepository.Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        Result result = await Handle(campaign.Id, iconKey: null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(campaign.IconKey, Is.Null);
    }

    [Test]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        _writeRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        Assert.That((await Handle(Guid.NewGuid())).Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_BlankTitle_ReturnsInvalid()
    {
        Assert.That((await Handle(Guid.NewGuid(), title: "")).Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_IconKeyOverTwoHundredCharacters_ReturnsInvalid()
    {
        Result result = await Handle(Guid.NewGuid(), iconKey: new string('a', 201));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_EmptyCampaignId_ReturnsInvalid()
    {
        Assert.That((await Handle(Guid.Empty)).Status, Is.EqualTo(ResultStatus.Invalid));
    }
}