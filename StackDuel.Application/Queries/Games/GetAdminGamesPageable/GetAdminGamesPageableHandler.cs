using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Games.GetAdminGamesPageable;

internal sealed class GetAdminGamesPageableHandler(
    IGameReadRepository gameReadRepository,
    IUserReadRepository userReadRepository
) : IQueryHandler<GetAdminGamesPageableQuery, PageResult<AdminGameListItemDto>>
{
    public async Task<Result<PageResult<AdminGameListItemDto>>> Handle(
        GetAdminGamesPageableQuery request,
        CancellationToken cancellationToken
    )
    {
        PageResult<Game> page = await gameReadRepository.GetAdminGamesPagedAsync(
            request.Status,
            request.PaginationRequest,
            cancellationToken
        );

        var gameModesById = new Dictionary<Guid, GameMode>();
        foreach (Guid gameModeId in page.Results.Select(g => g.GameModeId).Distinct())
        {
            var gameMode = await gameReadRepository.FindGameModeByIdIncludingInactiveAsync(
                gameModeId,
                cancellationToken
            );
            if (gameMode is not null)
                gameModesById[gameModeId] = gameMode;
        }

        var participantUserIds = page.Results.SelectMany(g => g.Participants.Select(p => p.UserId)).Distinct();
        var userMap = await userReadRepository.FindByIdsAsync(participantUserIds, cancellationToken);

        var items = page.Results.Select(game => BuildListItem(game, gameModesById, userMap)).ToList();

        return Result.Success(
            new PageResult<AdminGameListItemDto>
            {
                Results = items,
                Total = page.Total,
                Page = page.Page,
                Size = page.Size,
            }
        );
    }

    private static AdminGameListItemDto BuildListItem(
        Game game,
        IReadOnlyDictionary<Guid, GameMode> gameModesById,
        IReadOnlyDictionary<Guid, UserDto> userMap
    )
    {
        gameModesById.TryGetValue(game.GameModeId, out GameMode? gameMode);

        var participants = game
            .Participants.Select(p =>
            {
                userMap.TryGetValue(p.UserId, out var user);
                return new AdminGameParticipantSummaryDto(
                    p.UserId,
                    user?.Username ?? string.Empty,
                    p.Score,
                    p.HasForfeited,
                    p.HasFinishedProblems
                );
            })
            .ToArray();

        return new AdminGameListItemDto(
            game.Id,
            gameMode?.Key ?? "unknown",
            gameMode?.Name ?? "Unknown",
            game.Status,
            game.TimeLimitInSeconds,
            game.CreatedAt,
            game.StartedAt,
            game.EndedAt,
            participants
        );
    }
}