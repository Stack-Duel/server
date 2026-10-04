using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Games;
using StackDuel.Application.Languages;
using StackDuel.Application.Tracks;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Application.Commands.Games.CreateGame;

internal sealed class CreateGameHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    ITrackReadRepository trackReadRepository,
    ILanguageReadRepository languageReadRepository,
    UserContext userContext,
    IValidator<CreateGameCommand> validator
) : AbstractCommandHandler<CreateGameCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        CreateGameCommand request,
        CancellationToken cancellationToken
    )
    {
        GameMode? gameMode = await gameReadRepository.FindGameModeByKeyAsync(request.GameModeKey, cancellationToken);

        if (gameMode is null)
            return Result<Guid>.NotFound($"Game mode '{request.GameModeKey}' was not found or is not active.");

        string[] requestedTrackKeys = [.. request.TrackSelections.Select(s => s.TrackKey).Distinct()];
        IReadOnlyList<Track> tracks = await trackReadRepository.FindByKeysAsync(requestedTrackKeys, cancellationToken);

        if (tracks.Count != requestedTrackKeys.Length)
        {
            string[] missingKeys = requestedTrackKeys.Except(tracks.Select(t => t.Key)).ToArray();
            return Result<Guid>.NotFound(
                $"Track(s) '{string.Join(", ", missingKeys)}' were not found or are not active."
            );
        }

        Dictionary<string, Track> trackByKey = tracks.ToDictionary(t => t.Key);

        IReadOnlyList<Language> availableLanguages = await languageReadRepository.GetActiveLanguagesByTrackIdsAsync(
            tracks.Select(t => t.Id),
            cancellationToken
        );
        ILookup<Guid, Language> languagesByTrackId = availableLanguages.ToLookup(l => l.TrackId);

        var trackLanguageIds = new Dictionary<Guid, IReadOnlyList<Guid>>();

        foreach (TrackLanguageSelection selection in request.TrackSelections)
        {
            Track track = trackByKey[selection.TrackKey];
            Guid[] requestedLanguageIds = [.. selection.LanguageIds.Distinct()];
            HashSet<Guid> validLanguageIds = [.. languagesByTrackId[track.Id].Select(l => l.Id)];

            Guid[] invalidLanguageIds = [.. requestedLanguageIds.Where(id => !validLanguageIds.Contains(id))];
            if (invalidLanguageIds.Length > 0)
            {
                return Result<Guid>.NotFound(
                    $"Language(s) '{string.Join(", ", invalidLanguageIds)}' were not found or are not active for track '{track.Key}'."
                );
            }

            // Tracks that disallow language selection don't offer a picker client-side — the UI
            // always submits every active language for them. Reject a partial set explicitly
            // rather than silently honoring it, so a client bypassing the UI can't narrow a
            // player's language options on a track that isn't supposed to allow that choice.
            if (!track.AllowsLanguageSelection && requestedLanguageIds.Length != validLanguageIds.Count)
            {
                return Result<Guid>.Invalid(
                    new ValidationError(
                        nameof(request.TrackSelections),
                        $"Track '{track.Key}' does not allow selecting individual languages; all active languages must be included."
                    )
                );
            }

            trackLanguageIds[track.Id] = requestedLanguageIds;
        }

        string? requiredPermission = GameModePermissions.For(gameMode.Key);
        bool hasAccess = requiredPermission is not null && userContext.HasPermission(requiredPermission);

        if (!hasAccess)
            return Result<Guid>.Forbidden();

        bool validDuration = gameMode.TimeOptions.Any(o => o.DurationSeconds == request.TimeLimitInSeconds);
        if (!validDuration)
            return Result<Guid>.Invalid(
                new ValidationError(
                    nameof(request.TimeLimitInSeconds),
                    $"'{request.TimeLimitInSeconds}' is not a valid time option for this game mode."
                )
            );

        Game game = new(
            gameMode.Id,
            gameMode.DefaultPoolId,
            tracks.Select(t => t.Id),
            [request.CreatedByUserId],
            request.TimeLimitInSeconds,
            trackLanguageIds: trackLanguageIds,
            skipsEnabled: request.SkipsEnabled
        );

        await gameWriteRepository.AddAsync(game, cancellationToken);

        return Result<Guid>.Success(game.Id);
    }
}