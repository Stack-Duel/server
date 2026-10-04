using StackDuel.Application.Languages;
using StackDuel.Application.Tracks;
using StackDuel.Application.Tracks.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Tracks.Entities;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Tracks.GetTracks;

internal sealed class GetTracksHandler(
    ITrackReadRepository trackReadRepository,
    ILanguageReadRepository languageReadRepository
) : IQueryHandler<GetTracksQuery, IReadOnlyList<TrackDto>>
{
    public async Task<Result<IReadOnlyList<TrackDto>>> Handle(
        GetTracksQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Track> tracks = await trackReadRepository.GetActiveTracksAsync(cancellationToken);

        IReadOnlyList<Language> languages = await languageReadRepository.GetActiveLanguagesByTrackIdsAsync(
            tracks.Select(t => t.Id),
            cancellationToken
        );

        ILookup<Guid, Language> languagesByTrackId = languages.ToLookup(l => l.TrackId);

        IReadOnlyList<TrackDto> dtos =
        [
            .. tracks.Select(t => new TrackDto(
                t.Id,
                t.Key,
                t.Name,
                t.AllowsLanguageSelection,
                [.. languagesByTrackId[t.Id].Select(l => new TrackLanguageDto(l.Id, l.Name.Value))]
            )),
        ];

        return Result<IReadOnlyList<TrackDto>>.Success(dtos);
    }
}