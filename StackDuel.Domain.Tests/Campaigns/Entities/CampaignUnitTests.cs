using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Domain.Tests.Campaigns.Entities;

public class CampaignUnitTests
{
    private static readonly int[] SequentialSortOrders = [0, 1, 2];

    private static CampaignUnit CreateUnit(
        UnitType unitType = UnitType.Lesson,
        int estimatedMinutes = 15,
        int sortOrder = 0
    ) => new(Guid.NewGuid(), "Unit 1", "Lesson body.", unitType, estimatedMinutes, sortOrder);

    [Fact]
    public void Constructor_SetsOwnerDetailsAndSortOrder()
    {
        Guid moduleId = Guid.NewGuid();

        CampaignUnit unit = new(moduleId, "Unit 1", "Lesson body.", UnitType.Quiz, 20, 2);

        Assert.Equal(moduleId, unit.CampaignModuleId);
        Assert.Equal("Unit 1", unit.Title);
        Assert.Equal("Lesson body.", unit.Content);
        Assert.Equal(UnitType.Quiz, unit.UnitType);
        Assert.Equal(20, unit.EstimatedMinutes);
        Assert.Equal(2, unit.SortOrder);
        Assert.Empty(unit.Problems);
    }

    [Fact]
    public void Constructor_EmptyModuleId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignUnit(Guid.Empty, "Unit 1", "body", UnitType.Lesson, 5, 0));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankTitle_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => new CampaignUnit(Guid.NewGuid(), title!, "body", UnitType.Lesson, 5, 0));
    }

    [Fact]
    public void Constructor_TrimsTitle()
    {
        CampaignUnit unit = new(Guid.NewGuid(), "  Padded  ", "body", UnitType.Lesson, 5, 0);
        Assert.Equal("Padded", unit.Title);
    }

    [Fact]
    public void Constructor_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CampaignUnit(Guid.NewGuid(), "Unit 1", null!, UnitType.Lesson, 5, 0)
        );
    }

    [Fact]
    public void Constructor_EmptyContent_IsAllowed()
    {
        CampaignUnit unit = new(Guid.NewGuid(), "Unit 1", "", UnitType.Lesson, 5, 0);
        Assert.Empty(unit.Content);
    }

    [Fact]
    public void Constructor_NegativeEstimatedMinutes_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateUnit(estimatedMinutes: -1));
    }

    [Fact]
    public void Constructor_ZeroEstimatedMinutes_IsAllowed()
    {
        Assert.Equal(0, CreateUnit(estimatedMinutes: 0).EstimatedMinutes);
    }

    [Fact]
    public void UpdateDetails_ReplacesTitleContentTypeAndEstimate()
    {
        CampaignUnit unit = CreateUnit();

        unit.UpdateDetails("Renamed", "New body.", UnitType.Challenge, 45);

        Assert.Equal("Renamed", unit.Title);
        Assert.Equal("New body.", unit.Content);
        Assert.Equal(UnitType.Challenge, unit.UnitType);
        Assert.Equal(45, unit.EstimatedMinutes);
    }

    [Fact]
    public void UpdateDetails_NegativeEstimate_ThrowsWithoutMutating()
    {
        CampaignUnit unit = CreateUnit(estimatedMinutes: 15);

        Assert.Throws<ArgumentException>(() => unit.UpdateDetails("Renamed", "body", UnitType.Lesson, -5));
        Assert.Equal(15, unit.EstimatedMinutes);
    }

    [Fact]
    public void UpdateDetails_KeepsSortOrder()
    {
        CampaignUnit unit = CreateUnit(sortOrder: 3);

        unit.UpdateDetails("Renamed", "body", UnitType.Lesson, 5);

        Assert.Equal(3, unit.SortOrder);
    }

    [Fact]
    public void SetProblems_AddsOneProblemPerIdOwnedByTheUnit()
    {
        CampaignUnit unit = CreateUnit();
        Guid problemA = Guid.NewGuid();
        Guid problemB = Guid.NewGuid();

        unit.SetProblems([problemA, problemB]);

        Assert.Equal(new[] { problemA, problemB }, unit.Problems.Select(p => p.ProblemId));
        Assert.All(unit.Problems.Select(p => p.CampaignUnitId), item => Assert.Equal(unit.Id, item));
    }

    [Fact]
    public void SetProblems_AssignsSequentialSortOrdersInCallOrder()
    {
        CampaignUnit unit = CreateUnit();

        unit.SetProblems([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);

        Assert.Equal(SequentialSortOrders, unit.Problems.Select(p => p.SortOrder));
    }

    [Fact]
    public void SetProblems_DeduplicatesIdsKeepingTheFirstPosition()
    {
        CampaignUnit unit = CreateUnit();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        unit.SetProblems([first, second, first]);

        Assert.Equal(new[] { first, second }, unit.Problems.Select(p => p.ProblemId));
    }

    [Fact]
    public void SetProblems_IgnoresEmptyGuids()
    {
        CampaignUnit unit = CreateUnit();
        Guid problemId = Guid.NewGuid();

        unit.SetProblems([Guid.Empty, problemId, Guid.Empty]);

        Assert.Equal(problemId, unit.Problems.Single().ProblemId);
    }

    [Fact]
    public void SetProblems_ReplacesAnyPreviousSetAndRestartsSortOrder()
    {
        CampaignUnit unit = CreateUnit();
        unit.SetProblems([Guid.NewGuid(), Guid.NewGuid()]);
        Guid replacement = Guid.NewGuid();

        unit.SetProblems([replacement]);

        Assert.Equal(replacement, unit.Problems.Single().ProblemId);
        Assert.Equal(0, unit.Problems.Single().SortOrder);
    }

    [Fact]
    public void SetProblems_EmptyCollection_ClearsProblems()
    {
        CampaignUnit unit = CreateUnit();
        unit.SetProblems([Guid.NewGuid()]);

        unit.SetProblems([]);

        Assert.Empty(unit.Problems);
    }

    [Fact]
    public void SetProblems_Null_Throws()
    {
        CampaignUnit unit = CreateUnit();

        Assert.Throws<ArgumentNullException>(() => unit.SetProblems(null!));
    }
}