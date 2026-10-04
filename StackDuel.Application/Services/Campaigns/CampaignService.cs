using Ardalis.Result;
using MediatR;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Commands.Campaigns.AddCampaignModule;
using StackDuel.Application.Commands.Campaigns.AddCampaignUnit;
using StackDuel.Application.Commands.Campaigns.ArchiveCampaign;
using StackDuel.Application.Commands.Campaigns.CompleteUnit;
using StackDuel.Application.Commands.Campaigns.CreateCampaign;
using StackDuel.Application.Commands.Campaigns.EnrollInCampaign;
using StackDuel.Application.Commands.Campaigns.PublishCampaign;
using StackDuel.Application.Commands.Campaigns.SetCampaignPrerequisites;
using StackDuel.Application.Commands.Campaigns.SetUnitProblems;
using StackDuel.Application.Commands.Campaigns.UpdateCampaignDetails;
using StackDuel.Application.Commands.Campaigns.UpdateCampaignModule;
using StackDuel.Application.Commands.Campaigns.UpdateCampaignUnit;
using StackDuel.Application.Pagination;
using StackDuel.Application.Queries.Campaigns.GetAdminCampaignDetail;
using StackDuel.Application.Queries.Campaigns.GetAdminCampaignsPageable;
using StackDuel.Application.Queries.Campaigns.GetCampaignBySlug;
using StackDuel.Application.Queries.Campaigns.GetCampaignPath;
using StackDuel.Application.Queries.Campaigns.GetMyCampaignProgress;
using StackDuel.Application.Queries.Campaigns.GetMyEnrollments;
using StackDuel.Application.Queries.Campaigns.GetMyLearningStats;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Services.Campaigns;

public interface ICampaignService
{
    Task<Result<IReadOnlyList<CampaignSummaryDto>>> GetCampaignPathAsync(CancellationToken cancellationToken);

    Task<Result<CampaignDto>> GetCampaignBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<Result<PageResult<AdminCampaignListItemDto>>> GetAdminCampaignsPageableAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken
    );

    Task<Result<CampaignDto>> GetAdminCampaignDetailAsync(Guid campaignId, CancellationToken cancellationToken);

    Task<Result<UserCampaignProgressDto>> GetMyCampaignProgressAsync(
        Guid campaignId,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result<IReadOnlyList<UserCampaignProgressDto>>> GetMyEnrollmentsAsync(
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result<UserLearningStatsDto>> GetMyLearningStatsAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<Guid>> CreateCampaignAsync(
        string title,
        string description,
        CampaignDifficulty difficulty,
        CancellationToken cancellationToken
    );

    Task<Result> UpdateCampaignDetailsAsync(
        Guid campaignId,
        string title,
        string description,
        CampaignDifficulty difficulty,
        string? iconKey,
        CancellationToken cancellationToken
    );

    Task<Result> PublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken);

    Task<Result> ArchiveCampaignAsync(Guid campaignId, CancellationToken cancellationToken);

    Task<Result> SetCampaignPrerequisitesAsync(
        Guid campaignId,
        IReadOnlyList<Guid> requiredCampaignIds,
        CancellationToken cancellationToken
    );

    Task<Result<Guid>> AddCampaignModuleAsync(
        Guid campaignId,
        string title,
        string description,
        CancellationToken cancellationToken
    );

    Task<Result> UpdateCampaignModuleAsync(
        Guid campaignId,
        Guid moduleId,
        string title,
        string description,
        CancellationToken cancellationToken
    );

    Task<Result<Guid>> AddCampaignUnitAsync(
        Guid campaignId,
        Guid moduleId,
        string title,
        string content,
        UnitType unitType,
        int estimatedMinutes,
        CancellationToken cancellationToken
    );

    Task<Result> UpdateCampaignUnitAsync(
        Guid campaignId,
        Guid moduleId,
        Guid unitId,
        string title,
        string content,
        UnitType unitType,
        int estimatedMinutes,
        CancellationToken cancellationToken
    );

    Task<Result> SetUnitProblemsAsync(
        Guid campaignId,
        Guid moduleId,
        Guid unitId,
        IReadOnlyList<Guid> problemIds,
        CancellationToken cancellationToken
    );

    Task<Result<Guid>> EnrollInCampaignAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken);

    Task<Result> CompleteUnitAsync(Guid unitId, Guid userId, CancellationToken cancellationToken);
}

internal sealed class CampaignService(IMediator mediator) : ICampaignService
{
    public async Task<Result<IReadOnlyList<CampaignSummaryDto>>> GetCampaignPathAsync(
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetCampaignPathQuery(), cancellationToken);

    public async Task<Result<CampaignDto>> GetCampaignBySlugAsync(string slug, CancellationToken cancellationToken) =>
        await mediator.Send(new GetCampaignBySlugQuery(slug), cancellationToken);

    public async Task<Result<PageResult<AdminCampaignListItemDto>>> GetAdminCampaignsPageableAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetAdminCampaignsPageableQuery(paginationRequest, search), cancellationToken);

    public async Task<Result<CampaignDto>> GetAdminCampaignDetailAsync(
        Guid campaignId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetAdminCampaignDetailQuery(campaignId), cancellationToken);

    public async Task<Result<UserCampaignProgressDto>> GetMyCampaignProgressAsync(
        Guid campaignId,
        Guid userId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetMyCampaignProgressQuery(campaignId, userId), cancellationToken);

    public async Task<Result<IReadOnlyList<UserCampaignProgressDto>>> GetMyEnrollmentsAsync(
        Guid userId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetMyEnrollmentsQuery(userId), cancellationToken);

    public async Task<Result<UserLearningStatsDto>> GetMyLearningStatsAsync(
        Guid userId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetMyLearningStatsQuery(userId), cancellationToken);

    public async Task<Result<Guid>> CreateCampaignAsync(
        string title,
        string description,
        CampaignDifficulty difficulty,
        CancellationToken cancellationToken
    ) => await mediator.Send(new CreateCampaignCommand(title, description, difficulty), cancellationToken);

    public async Task<Result> UpdateCampaignDetailsAsync(
        Guid campaignId,
        string title,
        string description,
        CampaignDifficulty difficulty,
        string? iconKey,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new UpdateCampaignDetailsCommand(campaignId, title, description, difficulty, iconKey),
            cancellationToken
        );

    public async Task<Result> PublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        await mediator.Send(new PublishCampaignCommand(campaignId), cancellationToken);

    public async Task<Result> ArchiveCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        await mediator.Send(new ArchiveCampaignCommand(campaignId), cancellationToken);

    public async Task<Result> SetCampaignPrerequisitesAsync(
        Guid campaignId,
        IReadOnlyList<Guid> requiredCampaignIds,
        CancellationToken cancellationToken
    ) => await mediator.Send(new SetCampaignPrerequisitesCommand(campaignId, requiredCampaignIds), cancellationToken);

    public async Task<Result<Guid>> AddCampaignModuleAsync(
        Guid campaignId,
        string title,
        string description,
        CancellationToken cancellationToken
    ) => await mediator.Send(new AddCampaignModuleCommand(campaignId, title, description), cancellationToken);

    public async Task<Result> UpdateCampaignModuleAsync(
        Guid campaignId,
        Guid moduleId,
        string title,
        string description,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new UpdateCampaignModuleCommand(campaignId, moduleId, title, description),
            cancellationToken
        );

    public async Task<Result<Guid>> AddCampaignUnitAsync(
        Guid campaignId,
        Guid moduleId,
        string title,
        string content,
        UnitType unitType,
        int estimatedMinutes,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new AddCampaignUnitCommand(campaignId, moduleId, title, content, unitType, estimatedMinutes),
            cancellationToken
        );

    public async Task<Result> UpdateCampaignUnitAsync(
        Guid campaignId,
        Guid moduleId,
        Guid unitId,
        string title,
        string content,
        UnitType unitType,
        int estimatedMinutes,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new UpdateCampaignUnitCommand(campaignId, moduleId, unitId, title, content, unitType, estimatedMinutes),
            cancellationToken
        );

    public async Task<Result> SetUnitProblemsAsync(
        Guid campaignId,
        Guid moduleId,
        Guid unitId,
        IReadOnlyList<Guid> problemIds,
        CancellationToken cancellationToken
    ) => await mediator.Send(new SetUnitProblemsCommand(campaignId, moduleId, unitId, problemIds), cancellationToken);

    public async Task<Result<Guid>> EnrollInCampaignAsync(
        Guid campaignId,
        Guid userId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new EnrollInCampaignCommand(campaignId, userId), cancellationToken);

    public async Task<Result> CompleteUnitAsync(Guid unitId, Guid userId, CancellationToken cancellationToken) =>
        await mediator.Send(new CompleteUnitCommand(unitId, userId), cancellationToken);
}