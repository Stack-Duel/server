using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Domain.Tests.Campaigns.Entities;

public class CampaignModuleTests
{
    private static CampaignModule CreateModule(int sortOrder = 0) =>
        new(Guid.NewGuid(), "Module 1", "First module.", sortOrder);

    [Test]
    public void Constructor_SetsOwnerDetailsAndSortOrder()
    {
        Guid campaignId = Guid.NewGuid();

        CampaignModule module = new(campaignId, "Module 1", "First module.", 3);

        Assert.Multiple(() =>
        {
            Assert.That(module.CampaignId, Is.EqualTo(campaignId));
            Assert.That(module.Title, Is.EqualTo("Module 1"));
            Assert.That(module.Description, Is.EqualTo("First module."));
            Assert.That(module.SortOrder, Is.EqualTo(3));
            Assert.That(module.Units, Is.Empty);
        });
    }

    [Test]
    public void Constructor_EmptyCampaignId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignModule(Guid.Empty, "Module 1", "desc", 0));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankTitle_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => new CampaignModule(Guid.NewGuid(), title!, "desc", 0));
    }

    [Test]
    public void Constructor_TrimsTitle()
    {
        CampaignModule module = new(Guid.NewGuid(), "  Padded  ", "desc", 0);
        Assert.That(module.Title, Is.EqualTo("Padded"));
    }

    [Test]
    public void Constructor_NullDescription_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CampaignModule(Guid.NewGuid(), "Module 1", null!, 0));
    }

    [Test]
    public void UpdateDetails_ReplacesTitleAndDescription()
    {
        CampaignModule module = CreateModule();

        module.UpdateDetails("Renamed", "New description.");

        Assert.Multiple(() =>
        {
            Assert.That(module.Title, Is.EqualTo("Renamed"));
            Assert.That(module.Description, Is.EqualTo("New description."));
        });
    }

    [Test]
    public void UpdateDetails_BlankTitle_ThrowsWithoutMutating()
    {
        CampaignModule module = CreateModule();

        Assert.Throws<ArgumentException>(() => module.UpdateDetails("  ", "New description."));
        Assert.That(module.Title, Is.EqualTo("Module 1"));
    }

    [Test]
    public void UpdateDetails_KeepsSortOrder()
    {
        CampaignModule module = CreateModule(sortOrder: 4);

        module.UpdateDetails("Renamed", "desc");

        Assert.That(module.SortOrder, Is.EqualTo(4));
    }

    [Test]
    public void AddUnit_AppendsUnitOwnedByTheModule()
    {
        CampaignModule module = CreateModule();

        CampaignUnit unit = module.AddUnit("Unit 1", "Lesson body.", UnitType.Lesson, 15);

        Assert.Multiple(() =>
        {
            Assert.That(module.Units, Has.Count.EqualTo(1));
            Assert.That(unit.CampaignModuleId, Is.EqualTo(module.Id));
            Assert.That(unit.Title, Is.EqualTo("Unit 1"));
            Assert.That(unit.UnitType, Is.EqualTo(UnitType.Lesson));
            Assert.That(unit.EstimatedMinutes, Is.EqualTo(15));
        });
    }

    [Test]
    public void AddUnit_AssignsSequentialSortOrders()
    {
        CampaignModule module = CreateModule();

        CampaignUnit first = module.AddUnit("Unit 1", "", UnitType.Lesson, 5);
        CampaignUnit second = module.AddUnit("Unit 2", "", UnitType.Challenge, 5);
        CampaignUnit third = module.AddUnit("Unit 3", "", UnitType.Quiz, 5);

        Assert.That(new[] { first.SortOrder, second.SortOrder, third.SortOrder }, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void AddUnit_InvalidUnit_DoesNotLeaveAPartialUnitBehind()
    {
        CampaignModule module = CreateModule();

        Assert.Throws<ArgumentException>(() => module.AddUnit("  ", "body", UnitType.Lesson, 5));
        Assert.That(module.Units, Is.Empty);
    }
}