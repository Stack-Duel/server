using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Domain.Tests.Campaigns.Entities;

public class CampaignUnitTests
{
    private static CampaignUnit CreateUnit(
        UnitType unitType = UnitType.Lesson,
        int estimatedMinutes = 15,
        int sortOrder = 0
    ) => new(Guid.NewGuid(), "Unit 1", "Lesson body.", unitType, estimatedMinutes, sortOrder);

    [Test]
    public void Constructor_SetsOwnerDetailsAndSortOrder()
    {
        Guid moduleId = Guid.NewGuid();

        CampaignUnit unit = new(moduleId, "Unit 1", "Lesson body.", UnitType.Quiz, 20, 2);

        Assert.Multiple(() =>
        {
            Assert.That(unit.CampaignModuleId, Is.EqualTo(moduleId));
            Assert.That(unit.Title, Is.EqualTo("Unit 1"));
            Assert.That(unit.Content, Is.EqualTo("Lesson body."));
            Assert.That(unit.UnitType, Is.EqualTo(UnitType.Quiz));
            Assert.That(unit.EstimatedMinutes, Is.EqualTo(20));
            Assert.That(unit.SortOrder, Is.EqualTo(2));
            Assert.That(unit.Problems, Is.Empty);
        });
    }

    [Test]
    public void Constructor_EmptyModuleId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignUnit(Guid.Empty, "Unit 1", "body", UnitType.Lesson, 5, 0));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankTitle_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => new CampaignUnit(Guid.NewGuid(), title!, "body", UnitType.Lesson, 5, 0));
    }

    [Test]
    public void Constructor_TrimsTitle()
    {
        CampaignUnit unit = new(Guid.NewGuid(), "  Padded  ", "body", UnitType.Lesson, 5, 0);
        Assert.That(unit.Title, Is.EqualTo("Padded"));
    }

    [Test]
    public void Constructor_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CampaignUnit(Guid.NewGuid(), "Unit 1", null!, UnitType.Lesson, 5, 0)
        );
    }

    [Test]
    public void Constructor_EmptyContent_IsAllowed()
    {
        CampaignUnit unit = new(Guid.NewGuid(), "Unit 1", "", UnitType.Lesson, 5, 0);
        Assert.That(unit.Content, Is.Empty);
    }

    [Test]
    public void Constructor_NegativeEstimatedMinutes_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateUnit(estimatedMinutes: -1));
    }

    [Test]
    public void Constructor_ZeroEstimatedMinutes_IsAllowed()
    {
        Assert.That(CreateUnit(estimatedMinutes: 0).EstimatedMinutes, Is.Zero);
    }

    [Test]
    public void UpdateDetails_ReplacesTitleContentTypeAndEstimate()
    {
        CampaignUnit unit = CreateUnit();

        unit.UpdateDetails("Renamed", "New body.", UnitType.Challenge, 45);

        Assert.Multiple(() =>
        {
            Assert.That(unit.Title, Is.EqualTo("Renamed"));
            Assert.That(unit.Content, Is.EqualTo("New body."));
            Assert.That(unit.UnitType, Is.EqualTo(UnitType.Challenge));
            Assert.That(unit.EstimatedMinutes, Is.EqualTo(45));
        });
    }

    [Test]
    public void UpdateDetails_NegativeEstimate_ThrowsWithoutMutating()
    {
        CampaignUnit unit = CreateUnit(estimatedMinutes: 15);

        Assert.Throws<ArgumentException>(() => unit.UpdateDetails("Renamed", "body", UnitType.Lesson, -5));
        Assert.That(unit.EstimatedMinutes, Is.EqualTo(15));
    }

    [Test]
    public void UpdateDetails_KeepsSortOrder()
    {
        CampaignUnit unit = CreateUnit(sortOrder: 3);

        unit.UpdateDetails("Renamed", "body", UnitType.Lesson, 5);

        Assert.That(unit.SortOrder, Is.EqualTo(3));
    }

    [Test]
    public void SetProblems_AddsOneProblemPerIdOwnedByTheUnit()
    {
        CampaignUnit unit = CreateUnit();
        Guid problemA = Guid.NewGuid();
        Guid problemB = Guid.NewGuid();

        unit.SetProblems([problemA, problemB]);

        Assert.That(unit.Problems.Select(p => p.ProblemId), Is.EqualTo(new[] { problemA, problemB }));
        Assert.That(unit.Problems.Select(p => p.CampaignUnitId), Has.All.EqualTo(unit.Id));
    }

    [Test]
    public void SetProblems_AssignsSequentialSortOrdersInCallOrder()
    {
        CampaignUnit unit = CreateUnit();

        unit.SetProblems([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);

        Assert.That(unit.Problems.Select(p => p.SortOrder), Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void SetProblems_DeduplicatesIdsKeepingTheFirstPosition()
    {
        CampaignUnit unit = CreateUnit();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        unit.SetProblems([first, second, first]);

        Assert.That(unit.Problems.Select(p => p.ProblemId), Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void SetProblems_IgnoresEmptyGuids()
    {
        CampaignUnit unit = CreateUnit();
        Guid problemId = Guid.NewGuid();

        unit.SetProblems([Guid.Empty, problemId, Guid.Empty]);

        Assert.That(unit.Problems.Single().ProblemId, Is.EqualTo(problemId));
    }

    [Test]
    public void SetProblems_ReplacesAnyPreviousSetAndRestartsSortOrder()
    {
        CampaignUnit unit = CreateUnit();
        unit.SetProblems([Guid.NewGuid(), Guid.NewGuid()]);
        Guid replacement = Guid.NewGuid();

        unit.SetProblems([replacement]);

        Assert.That(unit.Problems.Single().ProblemId, Is.EqualTo(replacement));
        Assert.That(unit.Problems.Single().SortOrder, Is.Zero);
    }

    [Test]
    public void SetProblems_EmptyCollection_ClearsProblems()
    {
        CampaignUnit unit = CreateUnit();
        unit.SetProblems([Guid.NewGuid()]);

        unit.SetProblems([]);

        Assert.That(unit.Problems, Is.Empty);
    }

    [Test]
    public void SetProblems_Null_Throws()
    {
        CampaignUnit unit = CreateUnit();

        Assert.Throws<ArgumentNullException>(() => unit.SetProblems(null!));
    }
}