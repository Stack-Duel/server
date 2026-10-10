using Ardalis.Result;
using Moq;
using StackDuel.Application.Campaigns;
using StackDuel.Application.Commands.Campaigns.CompleteUnit;
using StackDuel.Application.Commands.Campaigns.EnrollInCampaign;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Tests.Commands.Campaigns;

public class EnrollInCampaignHandlerTests
{
    private Mock<ICampaignReadRepository> _campaignReadRepository = null!;
    private Mock<ICampaignProgressReadRepository> _progressReadRepository = null!;
    private Mock<ICampaignEnrollmentWriteRepository> _enrollmentWriteRepository = null!;
    private EnrollInCampaignHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _campaignReadRepository = new Mock<ICampaignReadRepository>();
        _progressReadRepository = new Mock<ICampaignProgressReadRepository>();
        _enrollmentWriteRepository = new Mock<ICampaignEnrollmentWriteRepository>();
        _handler = new EnrollInCampaignHandler(
            _campaignReadRepository.Object,
            _progressReadRepository.Object,
            _enrollmentWriteRepository.Object,
            new EnrollInCampaignValidator()
        );
    }

    private static Campaign CreatePublishedCampaign()
    {
        Campaign campaign = new("slug", "Title", "desc", CampaignDifficulty.Beginner);
        campaign.Publish();
        return campaign;
    }

    private void GivenCampaign(Campaign campaign) =>
        _campaignReadRepository
            .Setup(x => x.FindByIdAsync(campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

    private Task<Result<Guid>> Handle(Guid campaignId, Guid userId) =>
        _handler.Handle(new EnrollInCampaignCommand(campaignId, userId), CancellationToken.None);

    [Test]
    public async Task Handle_PublishedCampaignAndNoExistingEnrollment_EnrollsTheUser()
    {
        Campaign campaign = CreatePublishedCampaign();
        GivenCampaign(campaign);
        Guid userId = Guid.NewGuid();
        CampaignEnrollment? added = null;
        _enrollmentWriteRepository
            .Setup(x => x.AddAsync(It.IsAny<CampaignEnrollment>(), It.IsAny<CancellationToken>()))
            .Callback<CampaignEnrollment, CancellationToken>((enrollment, _) => added = enrollment)
            .Returns(Task.CompletedTask);

        Result<Guid> result = await Handle(campaign.Id, userId);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(added, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(added!.Id));
            Assert.That(added!.UserId, Is.EqualTo(userId));
            Assert.That(added.CampaignId, Is.EqualTo(campaign.Id));
            Assert.That(added.Status, Is.EqualTo(EnrollmentStatus.InProgress));
        });
    }

    [Test]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        _campaignReadRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        Result<Guid> result = await Handle(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_DraftCampaign_ReturnsInvalid()
    {
        Campaign campaign = new("slug", "Title", "desc", CampaignDifficulty.Beginner);
        GivenCampaign(campaign);

        Result<Guid> result = await Handle(campaign.Id, Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _enrollmentWriteRepository.Verify(
            x => x.AddAsync(It.IsAny<CampaignEnrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_ArchivedCampaign_ReturnsInvalid()
    {
        Campaign campaign = CreatePublishedCampaign();
        campaign.Archive();
        GivenCampaign(campaign);

        Result<Guid> result = await Handle(campaign.Id, Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_AlreadyEnrolled_IsIdempotentAndReturnsTheExistingEnrollmentId()
    {
        Campaign campaign = CreatePublishedCampaign();
        GivenCampaign(campaign);
        Guid userId = Guid.NewGuid();
        CampaignEnrollment existing = new(userId, campaign.Id);
        _progressReadRepository
            .Setup(x => x.FindEnrollmentAsync(userId, campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Result<Guid> result = await Handle(campaign.Id, userId);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(result.Value, Is.EqualTo(existing.Id));
        _enrollmentWriteRepository.Verify(
            x => x.AddAsync(It.IsAny<CampaignEnrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_EmptyCampaignId_ReturnsInvalid()
    {
        Assert.That((await Handle(Guid.Empty, Guid.NewGuid())).Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_EmptyUserId_ReturnsInvalid()
    {
        Assert.That((await Handle(Guid.NewGuid(), Guid.Empty)).Status, Is.EqualTo(ResultStatus.Invalid));
    }
}

public class CompleteUnitHandlerTests
{
    private Mock<ICampaignReadRepository> _campaignReadRepository = null!;
    private Mock<ICampaignProgressReadRepository> _progressReadRepository = null!;
    private Mock<ICampaignEnrollmentWriteRepository> _enrollmentWriteRepository = null!;
    private Mock<IUnitCompletionWriteRepository> _unitCompletionWriteRepository = null!;
    private CompleteUnitHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _campaignReadRepository = new Mock<ICampaignReadRepository>();
        _progressReadRepository = new Mock<ICampaignProgressReadRepository>();
        _enrollmentWriteRepository = new Mock<ICampaignEnrollmentWriteRepository>();
        _unitCompletionWriteRepository = new Mock<IUnitCompletionWriteRepository>();
        _handler = new CompleteUnitHandler(
            _campaignReadRepository.Object,
            _progressReadRepository.Object,
            _enrollmentWriteRepository.Object,
            _unitCompletionWriteRepository.Object,
            new CompleteUnitValidator()
        );
    }

    /// <summary>A published campaign with one module holding <paramref name="unitCount"/> units.</summary>
    private static (Campaign Campaign, IReadOnlyList<CampaignUnit> Units) CreateCampaign(int unitCount)
    {
        Campaign campaign = new("slug", "Title", "desc", CampaignDifficulty.Beginner);
        campaign.Publish();
        CampaignModule module = campaign.AddModule("Module 1", "desc");

        List<CampaignUnit> units = [];
        for (int i = 0; i < unitCount; i++)
            units.Add(module.AddUnit($"Unit {i + 1}", "body", UnitType.Lesson, 10));

        return (campaign, units);
    }

    private void GivenUnitBelongsTo(Campaign campaign, Guid unitId) =>
        _campaignReadRepository
            .Setup(x => x.FindByUnitIdAsync(unitId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

    private Task<Result> Handle(Guid unitId, Guid userId) =>
        _handler.Handle(new CompleteUnitCommand(unitId, userId), CancellationToken.None);

    [Test]
    public async Task Handle_FirstCompletionOfOneOfSeveralUnits_RecordsItWithoutCompletingTheCampaign()
    {
        (Campaign campaign, IReadOnlyList<CampaignUnit> units) = CreateCampaign(unitCount: 2);
        Guid userId = Guid.NewGuid();
        GivenUnitBelongsTo(campaign, units[0].Id);
        _progressReadRepository
            .Setup(x => x.GetCompletedUnitIdsAsync(userId, campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([units[0].Id]);

        Result result = await Handle(units[0].Id, userId);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        _unitCompletionWriteRepository.Verify(
            x =>
                x.AddAsync(
                    It.Is<UnitCompletion>(c => c.UserId == userId && c.CampaignUnitId == units[0].Id),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _enrollmentWriteRepository.Verify(
            x => x.AddAsync(It.IsAny<CampaignEnrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_UnitAlreadyCompleted_IsIdempotentAndRecordsNothing()
    {
        Guid userId = Guid.NewGuid();
        Guid unitId = Guid.NewGuid();
        _progressReadRepository
            .Setup(x => x.HasCompletedUnitAsync(userId, unitId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result result = await Handle(unitId, userId);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        _unitCompletionWriteRepository.Verify(
            x => x.AddAsync(It.IsAny<UnitCompletion>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _campaignReadRepository.Verify(
            x => x.FindByUnitIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_UnitBelongsToNoCampaign_ReturnsNotFound()
    {
        _campaignReadRepository
            .Setup(x => x.FindByUnitIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        Result result = await Handle(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
        _unitCompletionWriteRepository.Verify(
            x => x.AddAsync(It.IsAny<UnitCompletion>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_LastUnitAndNoEnrollmentYet_AddsACompletedEnrollment()
    {
        (Campaign campaign, IReadOnlyList<CampaignUnit> units) = CreateCampaign(unitCount: 1);
        Guid userId = Guid.NewGuid();
        GivenUnitBelongsTo(campaign, units[0].Id);
        _progressReadRepository
            .Setup(x => x.GetCompletedUnitIdsAsync(userId, campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([units[0].Id]);
        _progressReadRepository
            .Setup(x => x.FindEnrollmentAsync(userId, campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampaignEnrollment?)null);

        Result result = await Handle(units[0].Id, userId);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        _enrollmentWriteRepository.Verify(
            x =>
                x.AddAsync(
                    It.Is<CampaignEnrollment>(e => e.Status == EnrollmentStatus.Completed && e.UserId == userId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_LastUnitWithAnInProgressEnrollment_CompletesAndSavesThatEnrollment()
    {
        (Campaign campaign, IReadOnlyList<CampaignUnit> units) = CreateCampaign(unitCount: 1);
        Guid userId = Guid.NewGuid();
        CampaignEnrollment enrollment = new(userId, campaign.Id);
        GivenUnitBelongsTo(campaign, units[0].Id);
        _progressReadRepository
            .Setup(x => x.GetCompletedUnitIdsAsync(userId, campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([units[0].Id]);
        _progressReadRepository
            .Setup(x => x.FindEnrollmentAsync(userId, campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        Result result = await Handle(units[0].Id, userId);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(enrollment.Status, Is.EqualTo(EnrollmentStatus.Completed));
        _enrollmentWriteRepository.Verify(
            x => x.SaveChangesAsync(enrollment, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_LastUnitWithAnAlreadyCompletedEnrollment_LeavesItAlone()
    {
        (Campaign campaign, IReadOnlyList<CampaignUnit> units) = CreateCampaign(unitCount: 1);
        Guid userId = Guid.NewGuid();
        CampaignEnrollment enrollment = new(userId, campaign.Id);
        enrollment.Complete();
        DateTime? completedAt = enrollment.CompletedAt;
        GivenUnitBelongsTo(campaign, units[0].Id);
        _progressReadRepository
            .Setup(x => x.GetCompletedUnitIdsAsync(userId, campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([units[0].Id]);
        _progressReadRepository
            .Setup(x => x.FindEnrollmentAsync(userId, campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        Result result = await Handle(units[0].Id, userId);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(enrollment.CompletedAt, Is.EqualTo(completedAt));
        _enrollmentWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CampaignEnrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_CampaignWithNoUnits_RecordsTheCompletionWithoutCompletingTheCampaign()
    {
        (Campaign campaign, _) = CreateCampaign(unitCount: 0);
        Guid userId = Guid.NewGuid();
        Guid unitId = Guid.NewGuid();
        GivenUnitBelongsTo(campaign, unitId);

        Result result = await Handle(unitId, userId);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        _enrollmentWriteRepository.Verify(
            x => x.AddAsync(It.IsAny<CampaignEnrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_SomeUnitsStillOutstanding_DoesNotCompleteTheCampaign()
    {
        (Campaign campaign, IReadOnlyList<CampaignUnit> units) = CreateCampaign(unitCount: 3);
        Guid userId = Guid.NewGuid();
        GivenUnitBelongsTo(campaign, units[0].Id);
        _progressReadRepository
            .Setup(x => x.GetCompletedUnitIdsAsync(userId, campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([units[0].Id, units[1].Id]);

        await Handle(units[0].Id, userId);

        _progressReadRepository.Verify(
            x => x.FindEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_EmptyUnitId_ReturnsInvalid()
    {
        Assert.That((await Handle(Guid.Empty, Guid.NewGuid())).Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_EmptyUserId_ReturnsInvalid()
    {
        Assert.That((await Handle(Guid.NewGuid(), Guid.Empty)).Status, Is.EqualTo(ResultStatus.Invalid));
    }
}

public class UnitTypeXpTests
{
    [TestCase(UnitType.Lesson, 10)]
    [TestCase(UnitType.Quiz, 15)]
    [TestCase(UnitType.Challenge, 25)]
    public void For_ReturnsTheXpAwardedForEachUnitType(UnitType unitType, int expectedXp)
    {
        Assert.That(UnitTypeXp.For(unitType), Is.EqualTo(expectedXp));
    }

    [Test]
    public void For_UnknownUnitType_FallsBackToTheLessonAward()
    {
        Assert.That(UnitTypeXp.For((UnitType)99), Is.EqualTo(10));
    }
}