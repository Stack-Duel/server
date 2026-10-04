using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Campaigns.GetMyEnrollments;

public sealed record GetMyEnrollmentsQuery(Guid UserId) : IQuery<IReadOnlyList<UserCampaignProgressDto>>;