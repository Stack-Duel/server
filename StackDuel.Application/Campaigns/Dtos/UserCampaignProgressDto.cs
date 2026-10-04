using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Campaigns.Dtos;

public sealed record UserCampaignProgressDto(
    Guid CampaignId,
    bool Enrolled,
    EnrollmentStatus? Status,
    int TotalUnits,
    int CompletedUnits,
    IReadOnlyList<Guid> CompletedUnitIds
);