using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.Games.CloseLobby;
using StackDuel.Application.Commands.Games.CompleteProblem;
using StackDuel.Application.Commands.Games.CreateGame;
using StackDuel.Application.Commands.Games.ForfeitGame;
using StackDuel.Application.Commands.Games.JoinGame;
using StackDuel.Application.Commands.Games.JoinGameByCode;
using StackDuel.Application.Commands.Games.LeaveGame;
using StackDuel.Application.Commands.Games.SkipProblem;
using StackDuel.Application.Commands.Games.StartGame;
using StackDuel.Application.Commands.Games.SubmitGameProblem;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Queries.Games.GetAdminGameDetail;
using StackDuel.Application.Queries.Games.GetAdminGamePlayerHistory;
using StackDuel.Application.Queries.Games.GetAdminGamesPageable;
using StackDuel.Application.Queries.Games.GetDuelHeadToHeadRecord;
using StackDuel.Application.Queries.Games.GetGameModes;
using StackDuel.Application.Queries.Games.GetGameProblems;
using StackDuel.Application.Queries.Games.GetGameState;
using StackDuel.Application.Queries.Games.GetMyActiveGames;
using StackDuel.Application.Queries.Games.GetOpenGames;
using StackDuel.Application.Queries.Tracks.GetTracks;
using StackDuel.Application.Tracks.Dtos;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Services.Games;

public interface IGameService
{
    Task<Result<Guid>> CreateGameAsync(
        string gameModeKey,
        IReadOnlyList<TrackLanguageSelection> trackSelections,
        int timeLimitInSeconds,
        Guid userId,
        bool skipsEnabled,
        CancellationToken cancellationToken
    );
    Task<Result> StartGameAsync(Guid gameId, Guid userId, CancellationToken cancellationToken);
    Task<Result> JoinGameAsync(Guid gameId, Guid userId, CancellationToken cancellationToken);
    Task<Result<Guid>> JoinGameByCodeAsync(string joinCode, Guid userId, CancellationToken cancellationToken);
    Task<Result> LeaveGameAsync(Guid gameId, Guid userId, CancellationToken cancellationToken);
    Task<Result> CloseLobbyAsync(Guid gameId, Guid userId, CancellationToken cancellationToken);
    Task<Result> ForfeitGameAsync(Guid gameId, Guid userId, CancellationToken cancellationToken);
    Task<Result<Guid>> SubmitGameProblemAsync(
        Guid gameId,
        Guid problemId,
        Guid problemSetupId,
        string code,
        Guid userId,
        CancellationToken cancellationToken
    );
    Task<Result<CompleteProblemResultDto>> CompleteProblemAsync(
        Guid gameId,
        Guid problemId,
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken
    );
    Task<Result<SkipProblemResultDto>> SkipProblemAsync(
        Guid gameId,
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken
    );
    Task<Result<GameStateDto>> GetGameStateAsync(Guid gameId, Guid userId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<GameProblemHistoryDto>>> GetGameProblemsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    );
    Task<Result<IReadOnlyList<GameModeDto>>> GetGameModesAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<TrackDto>>> GetTracksAsync(CancellationToken cancellationToken);
    Task<Result<PageResult<GameLobbySummaryDto>>> GetOpenGamesAsync(
        string? gameModeKey,
        PaginationRequest paginationRequest,
        Guid? userId,
        CancellationToken cancellationToken
    );
    Task<Result<IReadOnlyList<MyActiveGameDto>>> GetMyActiveGamesAsync(
        Guid userId,
        CancellationToken cancellationToken
    );
    Task<Result<PageResult<AdminGameListItemDto>>> GetAdminGamesPageableAsync(
        GameStatus? status,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken
    );
    Task<Result<GameStateDto>> GetAdminGameDetailAsync(Guid gameId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<AdminGamePlayerHistoryDto>>> GetAdminGamePlayerHistoryAsync(
        Guid gameId,
        CancellationToken cancellationToken
    );
    Task<Result<DuelHeadToHeadRecordDto>> GetDuelHeadToHeadRecordAsync(
        Guid userId,
        Guid opponentId,
        CancellationToken cancellationToken
    );
}

internal sealed class GameService(IMediator mediator) : IGameService
{
    public async Task<Result<Guid>> CreateGameAsync(
        string gameModeKey,
        IReadOnlyList<TrackLanguageSelection> trackSelections,
        int timeLimitInSeconds,
        Guid userId,
        bool skipsEnabled,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(
            new CreateGameCommand(gameModeKey, trackSelections, timeLimitInSeconds, userId, skipsEnabled),
            cancellationToken
        );
    }

    public async Task<Result> StartGameAsync(Guid gameId, Guid userId, CancellationToken cancellationToken)
    {
        return await mediator.Send(new StartGameCommand(gameId, userId), cancellationToken);
    }

    public async Task<Result> JoinGameAsync(Guid gameId, Guid userId, CancellationToken cancellationToken)
    {
        return await mediator.Send(new JoinGameCommand(gameId, userId), cancellationToken);
    }

    public async Task<Result<Guid>> JoinGameByCodeAsync(
        string joinCode,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new JoinGameByCodeCommand(joinCode, userId), cancellationToken);
    }

    public async Task<Result> LeaveGameAsync(Guid gameId, Guid userId, CancellationToken cancellationToken)
    {
        return await mediator.Send(new LeaveGameCommand(gameId, userId), cancellationToken);
    }

    public async Task<Result> CloseLobbyAsync(Guid gameId, Guid userId, CancellationToken cancellationToken)
    {
        return await mediator.Send(new CloseLobbyCommand(gameId, userId), cancellationToken);
    }

    public async Task<Result> ForfeitGameAsync(Guid gameId, Guid userId, CancellationToken cancellationToken)
    {
        return await mediator.Send(new ForfeitGameCommand(gameId, userId), cancellationToken);
    }

    public async Task<Result<Guid>> SubmitGameProblemAsync(
        Guid gameId,
        Guid problemId,
        Guid problemSetupId,
        string code,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(
            new SubmitGameProblemCommand(gameId, problemId, problemSetupId, code, userId),
            cancellationToken
        );
    }

    public async Task<Result<CompleteProblemResultDto>> CompleteProblemAsync(
        Guid gameId,
        Guid problemId,
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(
            new CompleteProblemCommand(gameId, problemId, submissionId, userId),
            cancellationToken
        );
    }

    public async Task<Result<SkipProblemResultDto>> SkipProblemAsync(
        Guid gameId,
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new SkipProblemCommand(gameId, problemId, userId), cancellationToken);
    }

    public async Task<Result<GameStateDto>> GetGameStateAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetGameStateQuery(gameId, userId), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<GameProblemHistoryDto>>> GetGameProblemsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetGameProblemsQuery(gameId, userId), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<GameModeDto>>> GetGameModesAsync(CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetGameModesQuery(), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<TrackDto>>> GetTracksAsync(CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetTracksQuery(), cancellationToken);
    }

    public async Task<Result<PageResult<GameLobbySummaryDto>>> GetOpenGamesAsync(
        string? gameModeKey,
        PaginationRequest paginationRequest,
        Guid? userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetOpenGamesQuery(gameModeKey, paginationRequest, userId), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<MyActiveGameDto>>> GetMyActiveGamesAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetMyActiveGamesQuery(userId), cancellationToken);
    }

    public async Task<Result<PageResult<AdminGameListItemDto>>> GetAdminGamesPageableAsync(
        GameStatus? status,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetAdminGamesPageableQuery(status, paginationRequest), cancellationToken);
    }

    public async Task<Result<GameStateDto>> GetAdminGameDetailAsync(Guid gameId, CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetAdminGameDetailQuery(gameId), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<AdminGamePlayerHistoryDto>>> GetAdminGamePlayerHistoryAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetAdminGamePlayerHistoryQuery(gameId), cancellationToken);
    }

    public async Task<Result<DuelHeadToHeadRecordDto>> GetDuelHeadToHeadRecordAsync(
        Guid userId,
        Guid opponentId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetDuelHeadToHeadRecordQuery(userId, opponentId), cancellationToken);
    }
}