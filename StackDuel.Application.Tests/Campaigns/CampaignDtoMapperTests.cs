using StackDuel.Application.Campaigns;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Tests.Campaigns;

public class CampaignDtoMapperTests
{
    private static Campaign CreateCampaignWithTwoModules()
    {
        Campaign campaign = new("arrays", "Arrays", "All about arrays", CampaignDifficulty.Beginner);

        CampaignModule first = campaign.AddModule("Basics", "Getting started");
        CampaignUnit intro = first.AddUnit("Intro", "content", UnitType.Lesson, estimatedMinutes: 10);
        first.AddUnit("Practice", "content", UnitType.Challenge, estimatedMinutes: 20);

        CampaignModule second = campaign.AddModule("Advanced", "Going deeper");
        second.AddUnit("Two pointers", "content", UnitType.Lesson, estimatedMinutes: 15);

        intro.SetProblems([Guid.NewGuid(), Guid.NewGuid()]);

        return campaign;
    }

    [Fact]
    public void ToDto_CarriesTheCampaignAndItsNestedModulesUnitsAndProblems()
    {
        Campaign campaign = CreateCampaignWithTwoModules();

        CampaignDto dto = CampaignDtoMapper.ToDto(campaign);

        Assert.Equal(campaign.Id, dto.Id);
        Assert.Equal("arrays", dto.Slug);
        Assert.Equal("Arrays", dto.Title);
        Assert.Equal(2, dto.Modules.Count);
        Assert.Equal(2, dto.Modules[0].Units.Count);
        Assert.Equal(2, dto.Modules[0].Units[0].Problems.Count);
    }

    [Fact]
    public void ToDto_OrdersModulesUnitsAndProblemsBySortOrder()
    {
        Campaign campaign = CreateCampaignWithTwoModules();

        CampaignDto dto = CampaignDtoMapper.ToDto(campaign);

        Assert.Equal(["Basics", "Advanced"], dto.Modules.Select(m => m.Title));
        Assert.Equal(["Intro", "Practice"], dto.Modules[0].Units.Select(u => u.Title));
        Assert.Equal([0, 1], dto.Modules[0].Units[0].Problems.Select(p => p.SortOrder));
    }

    [Fact]
    public void ToDto_IncludesPrerequisiteCampaignIds()
    {
        Campaign campaign = CreateCampaignWithTwoModules();
        Guid required = Guid.NewGuid();
        campaign.SetPrerequisites([required]);

        CampaignDto dto = CampaignDtoMapper.ToDto(campaign);

        Assert.Equal([required], dto.PrerequisiteCampaignIds);
    }

    [Fact]
    public void ToSummaryDto_CountsModulesAndUnitsAndSumsEstimatedMinutes()
    {
        Campaign campaign = CreateCampaignWithTwoModules();

        CampaignSummaryDto dto = CampaignDtoMapper.ToSummaryDto(campaign);

        Assert.Equal(2, dto.ModuleCount);
        Assert.Equal(3, dto.UnitCount);
        Assert.Equal(45, dto.EstimatedMinutes);
    }

    [Fact]
    public void ToSummaryDto_CampaignWithNoModules_ReportsZeroes()
    {
        Campaign campaign = new("empty", "Empty", "Nothing here", CampaignDifficulty.Beginner);

        CampaignSummaryDto dto = CampaignDtoMapper.ToSummaryDto(campaign);

        Assert.Equal(0, dto.ModuleCount);
        Assert.Equal(0, dto.UnitCount);
        Assert.Equal(0, dto.EstimatedMinutes);
    }

    [Fact]
    public void ToAdminListItemDto_CarriesStatusAndTheModuleAndUnitCounts()
    {
        Campaign campaign = CreateCampaignWithTwoModules();

        AdminCampaignListItemDto dto = CampaignDtoMapper.ToAdminListItemDto(campaign);

        Assert.Equal(campaign.Id, dto.Id);
        Assert.Equal(campaign.Status, dto.Status);
        Assert.Equal(2, dto.ModuleCount);
        Assert.Equal(3, dto.UnitCount);
    }
}