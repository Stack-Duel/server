using Ardalis.Result;
using StackDuel.Application.Languages;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Application.Tracks.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Application.Queries.StudioProblems.GetInitialStudioProblemSetups;

internal sealed class GetInitialStudioProblemSetupsHandler(
    ITrackReadRepository trackReadRepository,
    ILanguageReadRepository languageReadRepository
) : IQueryHandler<GetInitialStudioProblemSetupsQuery, InitialStudioProblemSetupDto>
{
    public async Task<Result<InitialStudioProblemSetupDto>> Handle(
        GetInitialStudioProblemSetupsQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Track> tracks = await trackReadRepository.GetActiveTracksAsync(cancellationToken);

        IReadOnlyList<Language> languages = await languageReadRepository.GetActiveLanguagesByTrackIdsAsync(
            tracks.Select(t => t.Id),
            cancellationToken
        );

        ILookup<Guid, Language> languagesByTrackId = languages.ToLookup(l => l.TrackId);

        IReadOnlyList<TrackDto> availableTracks =
        [
            .. tracks.Select(t => new TrackDto(
                t.Id,
                t.Key,
                t.Name,
                t.AllowsLanguageSelection,
                [.. languagesByTrackId[t.Id].Select(l => new TrackLanguageDto(l.Id, l.Name.Value))]
            )),
        ];

        return Result.Success(new InitialStudioProblemSetupDto(availableTracks));
    }
}