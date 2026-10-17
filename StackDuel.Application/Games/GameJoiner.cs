using Ardalis.Result;
using StackDuel.Application.Events;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Games;

internal sealed class GameJoiner(
    IGameReadRepository gameReadRepository,
    IUnitOfWork unitOfWork,
    UserContext userContext,
    IDomainEventDispatcher domainEventDispatcher
) : IGameJoiner
{
    public async Task<Result> JoinAsync(
        Guid gameId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(gameId, cancellationToken);

        if (game is null)
            return Result.NotFound($"Game '{gameId}' was not found.");

        if (game.Status != GameStatus.Pending)
            return Result.Invalid(new ValidationError(nameof(game.Status), "Only pending games can be joined."));

        GameMode? gameMode = await gameReadRepository.FindGameModeByIdAsync(game.GameModeId, cancellationToken);
        if (gameMode is null)
            return Result.NotFound($"Game mode '{game.GameModeId}' was not found.");

        string? requiredPermission = GameModePermissions.For(gameMode.Key);
        bool hasAccess = requiredPermission is not null && userContext.HasPermission(requiredPermission);

        if (!hasAccess)
            return Result.Forbidden();

        if (game.Participants.Any(p => p.UserId == requestedByUserId))
            return Result.Invalid(new ValidationError(nameof(requestedByUserId), "You have already joined this game."));

        if (game.Participants.Count >= gameMode.MaxPlayers)
            return Result.Invalid(new ValidationError(nameof(gameMode.MaxPlayers), "This game is already full."));

        game.Join(requestedByUserId, gameMode.MaxPlayers);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

        return Result.Success();
    }
}