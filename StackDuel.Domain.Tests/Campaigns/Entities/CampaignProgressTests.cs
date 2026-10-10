using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Domain.Tests.Campaigns.Entities;

public class CampaignEnrollmentTests
{
    [Fact]
    public void Constructor_SetsUserCampaignAndInProgressStatus()
    {
        Guid userId = Guid.NewGuid();
        Guid campaignId = Guid.NewGuid();

        CampaignEnrollment enrollment = new(userId, campaignId);

        Assert.Equal(userId, enrollment.UserId);
        Assert.Equal(campaignId, enrollment.CampaignId);
        Assert.Equal(EnrollmentStatus.InProgress, enrollment.Status);
        Assert.Null(enrollment.CompletedAt);
    }

    [Fact]
    public void Constructor_StampsEnrolledAt()
    {
        DateTime before = DateTime.UtcNow;
        Assert.True(new CampaignEnrollment(Guid.NewGuid(), Guid.NewGuid()).EnrolledAt >= before);
    }

    [Fact]
    public void Constructor_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignEnrollment(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_EmptyCampaignId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignEnrollment(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Complete_SetsStatusAndCompletedAt()
    {
        CampaignEnrollment enrollment = new(Guid.NewGuid(), Guid.NewGuid());
        DateTime before = DateTime.UtcNow;

        enrollment.Complete();

        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
        Assert.True(enrollment.CompletedAt >= before);
    }

    [Fact]
    public void Complete_AlreadyCompleted_Throws()
    {
        CampaignEnrollment enrollment = new(Guid.NewGuid(), Guid.NewGuid());
        enrollment.Complete();

        Assert.Throws<InvalidOperationException>(enrollment.Complete);
    }
}

public class UnitCompletionTests
{
    [Fact]
    public void Constructor_SetsUserAndUnit()
    {
        Guid userId = Guid.NewGuid();
        Guid unitId = Guid.NewGuid();

        UnitCompletion completion = new(userId, unitId);

        Assert.Equal(userId, completion.UserId);
        Assert.Equal(unitId, completion.CampaignUnitId);
    }

    [Fact]
    public void Constructor_StampsCompletedAt()
    {
        DateTime before = DateTime.UtcNow;
        Assert.True(new UnitCompletion(Guid.NewGuid(), Guid.NewGuid()).CompletedAt >= before);
    }

    [Fact]
    public void Constructor_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UnitCompletion(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_EmptyUnitId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UnitCompletion(Guid.NewGuid(), Guid.Empty));
    }
}

public class UnitProblemTests
{
    [Fact]
    public void Constructor_SetsUnitProblemAndSortOrder()
    {
        Guid unitId = Guid.NewGuid();
        Guid problemId = Guid.NewGuid();

        UnitProblem unitProblem = new(unitId, problemId, 4);

        Assert.Equal(unitId, unitProblem.CampaignUnitId);
        Assert.Equal(problemId, unitProblem.ProblemId);
        Assert.Equal(4, unitProblem.SortOrder);
    }

    [Fact]
    public void Constructor_EmptyUnitId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UnitProblem(Guid.Empty, Guid.NewGuid(), 0));
    }

    [Fact]
    public void Constructor_EmptyProblemId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UnitProblem(Guid.NewGuid(), Guid.Empty, 0));
    }
}

public class CampaignPrerequisiteTests
{
    [Fact]
    public void Constructor_SetsBothCampaignIds()
    {
        Guid campaignId = Guid.NewGuid();
        Guid requiredCampaignId = Guid.NewGuid();

        CampaignPrerequisite prerequisite = new(campaignId, requiredCampaignId);

        Assert.Equal(campaignId, prerequisite.CampaignId);
        Assert.Equal(requiredCampaignId, prerequisite.RequiredCampaignId);
    }

    [Fact]
    public void Constructor_EmptyCampaignId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignPrerequisite(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_EmptyRequiredCampaignId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignPrerequisite(Guid.NewGuid(), Guid.Empty));
    }
}