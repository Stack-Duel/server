using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Game;
using StackDuel.Application;
using StackDuel.Application.Commands.Games.CompleteProblem;
using StackDuel.Application.Commands.Games.SkipProblem;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Games;
using StackDuel.Application.Tracks.Dtos;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class GameController(IGameService gameService, UserContext userContext) : ControllerBase
{
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> CreateGame(
        [FromBody] CreateGameRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<Guid>.Unauthorized());

        IReadOnlyList<TrackLanguageSelection> trackSelections =
        [
            .. request.TrackSelections.Select(s => new TrackLanguageSelection(s.TrackKey, s.LanguageIds)),
        ];

        Result<Guid> result = await gameService.CreateGameAsync(
            request.GameModeKey,
            trackSelections,
            request.TimeLimitInSeconds,
            userContext.User.Id,
            request.SkipsEnabled,
            cancellationToken
        );

        if (!result.IsSuccess)
            return this.ToActionResult(result);

        return CreatedAtAction(nameof(CreateGame), new { gameId = result.Value }, result.Value);
    }

    [HttpPost("{gameId:guid}/start")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StartGame(Guid gameId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(await gameService.StartGameAsync(gameId, userContext.User.Id, cancellationToken));
    }

    [HttpPost("{gameId:guid}/forfeit")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ForfeitGame(Guid gameId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(await gameService.ForfeitGameAsync(gameId, userContext.User.Id, cancellationToken));
    }

    [HttpPost("{gameId:guid}/join")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> JoinGame(Guid gameId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(await gameService.JoinGameAsync(gameId, userContext.User.Id, cancellationToken));
    }

    [HttpPost("join-by-code")]
    [RequireUser]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> JoinGameByCode(
        [FromBody] JoinGameByCodeRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<Guid>.Unauthorized());

        return this.ToActionResult(
            await gameService.JoinGameByCodeAsync(request.JoinCode, userContext.User.Id, cancellationToken)
        );
    }

    [HttpPost("{gameId:guid}/leave")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> LeaveGame(Guid gameId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(await gameService.LeaveGameAsync(gameId, userContext.User.Id, cancellationToken));
    }

    [HttpPost("{gameId:guid}/close")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CloseLobby(Guid gameId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(await gameService.CloseLobbyAsync(gameId, userContext.User.Id, cancellationToken));
    }

    [HttpPost("{gameId:guid}/problems/{problemId:guid}/submit")]
    [RequireUser]
    [EnableRateLimiting(WellKnownPolicies.Submissions)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> SubmitGameProblem(
        Guid gameId,
        Guid problemId,
        [FromBody] SubmitGameProblemRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<Guid>.Unauthorized());

        return this.ToActionResult(
            await gameService.SubmitGameProblemAsync(
                gameId,
                problemId,
                request.ProblemSetupId,
                request.Code,
                userContext.User.Id,
                cancellationToken
            )
        );
    }

    [HttpPost("{gameId:guid}/problems/{problemId:guid}/complete")]
    [RequireUser]
    [ProducesResponseType(typeof(CompleteProblemResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompleteProblemResultDto>> CompleteProblem(
        Guid gameId,
        Guid problemId,
        [FromBody] CompleteProblemRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<CompleteProblemResultDto>.Unauthorized());

        var result = await gameService.CompleteProblemAsync(
            gameId,
            problemId,
            request.SubmissionId,
            userContext.User.Id,
            cancellationToken
        );

        return this.ToActionResult(result);
    }

    [HttpPost("{gameId:guid}/problems/{problemId:guid}/skip")]
    [RequireUser]
    [ProducesResponseType(typeof(SkipProblemResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkipProblemResultDto>> SkipProblem(
        Guid gameId,
        Guid problemId,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<SkipProblemResultDto>.Unauthorized());

        var result = await gameService.SkipProblemAsync(gameId, problemId, userContext.User.Id, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("{gameId:guid}")]
    [RequireUser]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameStateDto>> GetGameState(Guid gameId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<GameStateDto>.Unauthorized());

        return this.ToActionResult(await gameService.GetGameStateAsync(gameId, userContext.User.Id, cancellationToken));
    }

    [HttpGet("{gameId:guid}/problems")]
    [RequireUser]
    [ProducesResponseType(typeof(IReadOnlyList<GameProblemHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<GameProblemHistoryDto>>> GetGameProblems(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<IReadOnlyList<GameProblemHistoryDto>>.Unauthorized());

        var result = await gameService.GetGameProblemsAsync(gameId, userContext.User.Id, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("duel-record/{opponentId:guid}")]
    [RequireUser]
    [ProducesResponseType(typeof(DuelHeadToHeadRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DuelHeadToHeadRecordDto>> GetDuelHeadToHeadRecord(
        Guid opponentId,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<DuelHeadToHeadRecordDto>.Unauthorized());

        return this.ToActionResult(
            await gameService.GetDuelHeadToHeadRecordAsync(userContext.User.Id, opponentId, cancellationToken)
        );
    }

    [HttpGet("modes")]
    [ProducesResponseType(typeof(IReadOnlyList<GameModeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GameModeDto>>> GetGameModes(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await gameService.GetGameModesAsync(cancellationToken));
    }

    [HttpGet("tracks")]
    [ProducesResponseType(typeof(IReadOnlyList<TrackDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrackDto>>> GetTracks(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await gameService.GetTracksAsync(cancellationToken));
    }

    [HttpGet]
    [RequireUser]
    [ProducesResponseType(typeof(PageResult<GameLobbySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResult<GameLobbySummaryDto>>> GetOpenGames(
        [FromQuery] GetOpenGamesRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await gameService.GetOpenGamesAsync(
                query.GameModeKey,
                new PaginationRequest { Page = query.Page, Size = query.Size },
                userContext.User?.Id,
                cancellationToken
            )
        );
    }

    [HttpGet("mine")]
    [RequireUser]
    [ProducesResponseType(typeof(IReadOnlyList<MyActiveGameDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MyActiveGameDto>>> GetMyActiveGames(
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<IReadOnlyList<MyActiveGameDto>>.Unauthorized());

        return this.ToActionResult(await gameService.GetMyActiveGamesAsync(userContext.User.Id, cancellationToken));
    }

    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminGames)]
    [ProducesResponseType(typeof(PageResult<AdminGameListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PageResult<AdminGameListItemDto>>> GetAdminGames(
        [FromQuery] GetAdminGamesPageableRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await gameService.GetAdminGamesPageableAsync(
                query.Status,
                new PaginationRequest
                {
                    Page = query.Page,
                    Size = query.Size,
                    Timestamp = query.Timestamp,
                },
                cancellationToken
            )
        );
    }

    [HttpGet("admin/{gameId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminGames)]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameStateDto>> GetAdminGameDetail(Guid gameId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await gameService.GetAdminGameDetailAsync(gameId, cancellationToken));
    }

    [HttpGet("admin/{gameId:guid}/history")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminGames)]
    [ProducesResponseType(typeof(IReadOnlyList<AdminGamePlayerHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AdminGamePlayerHistoryDto>>> GetAdminGamePlayerHistory(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await gameService.GetAdminGamePlayerHistoryAsync(gameId, cancellationToken));
    }
}