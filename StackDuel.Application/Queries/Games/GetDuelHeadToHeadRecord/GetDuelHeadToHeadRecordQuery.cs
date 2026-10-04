using StackDuel.Application.Games.Dtos;

namespace StackDuel.Application.Queries.Games.GetDuelHeadToHeadRecord;

public sealed record GetDuelHeadToHeadRecordQuery(Guid UserId, Guid OpponentId) : IQuery<DuelHeadToHeadRecordDto>;