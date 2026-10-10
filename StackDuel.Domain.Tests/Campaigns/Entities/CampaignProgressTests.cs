using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Domain.Tests.Campaigns.Entities;

public class CampaignEnrollmentTests
{
    [Test]
    public void Constructor_SetsUserCampaignAndInProgressStatus()
    {
        Guid userId = Guid.NewGuid();
        Guid campaignId = Guid.NewGuid();

        CampaignEnrollment enrollment = new(userId, campaignId);

        Assert.Multiple(() =>
        {
            Assert.That(enrollment.UserId, Is.EqualTo(userId));
            Assert.That(enrollment.CampaignId, Is.EqualTo(campaignId));
            Assert.That(enrollment.Status, Is.EqualTo(EnrollmentStatus.InProgress));
            Assert.That(enrollment.CompletedAt, Is.Null);
        });
    }

    [Test]
    public void Constructor_StampsEnrolledAt()
    {
        DateTime before = DateTime.UtcNow;
        Assert.That(new CampaignEnrollment(Guid.NewGuid(), Guid.NewGuid()).EnrolledAt, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void Constructor_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignEnrollment(Guid.Empty, Guid.NewGuid()));
    }

    [Test]
    public void Constructor_EmptyCampaignId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignEnrollment(Guid.NewGuid(), Guid.Empty));
    }

    [Test]
    public void Complete_SetsStatusAndCompletedAt()
    {
        CampaignEnrollment enrollment = new(Guid.NewGuid(), Guid.NewGuid());
        DateTime before = DateTime.UtcNow;

        enrollment.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(enrollment.Status, Is.EqualTo(EnrollmentStatus.Completed));
            Assert.That(enrollment.CompletedAt, Is.GreaterThanOrEqualTo(before));
        });
    }

    [Test]
    public void Complete_AlreadyCompleted_Throws()
    {
        CampaignEnrollment enrollment = new(Guid.NewGuid(), Guid.NewGuid());
        enrollment.Complete();

        Assert.Throws<InvalidOperationException>(enrollment.Complete);
    }
}

public class UnitCompletionTests
{
    [Test]
    public void Constructor_SetsUserAndUnit()
    {
        Guid userId = Guid.NewGuid();
        Guid unitId = Guid.NewGuid();

        UnitCompletion completion = new(userId, unitId);

        Assert.Multiple(() =>
        {
            Assert.That(completion.UserId, Is.EqualTo(userId));
            Assert.That(completion.CampaignUnitId, Is.EqualTo(unitId));
        });
    }

    [Test]
    public void Constructor_StampsCompletedAt()
    {
        DateTime before = DateTime.UtcNow;
        Assert.That(new UnitCompletion(Guid.NewGuid(), Guid.NewGuid()).CompletedAt, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void Constructor_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UnitCompletion(Guid.Empty, Guid.NewGuid()));
    }

    [Test]
    public void Constructor_EmptyUnitId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UnitCompletion(Guid.NewGuid(), Guid.Empty));
    }
}

public class UnitProblemTests
{
    [Test]
    public void Constructor_SetsUnitProblemAndSortOrder()
    {
        Guid unitId = Guid.NewGuid();
        Guid problemId = Guid.NewGuid();

        UnitProblem unitProblem = new(unitId, problemId, 4);

        Assert.Multiple(() =>
        {
            Assert.That(unitProblem.CampaignUnitId, Is.EqualTo(unitId));
            Assert.That(unitProblem.ProblemId, Is.EqualTo(problemId));
            Assert.That(unitProblem.SortOrder, Is.EqualTo(4));
        });
    }

    [Test]
    public void Constructor_EmptyUnitId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UnitProblem(Guid.Empty, Guid.NewGuid(), 0));
    }

    [Test]
    public void Constructor_EmptyProblemId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UnitProblem(Guid.NewGuid(), Guid.Empty, 0));
    }
}

public class CampaignPrerequisiteTests
{
    [Test]
    public void Constructor_SetsBothCampaignIds()
    {
        Guid campaignId = Guid.NewGuid();
        Guid requiredCampaignId = Guid.NewGuid();

        CampaignPrerequisite prerequisite = new(campaignId, requiredCampaignId);

        Assert.Multiple(() =>
        {
            Assert.That(prerequisite.CampaignId, Is.EqualTo(campaignId));
            Assert.That(prerequisite.RequiredCampaignId, Is.EqualTo(requiredCampaignId));
        });
    }

    [Test]
    public void Constructor_EmptyCampaignId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignPrerequisite(Guid.Empty, Guid.NewGuid()));
    }

    [Test]
    public void Constructor_EmptyRequiredCampaignId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignPrerequisite(Guid.NewGuid(), Guid.Empty));
    }
}