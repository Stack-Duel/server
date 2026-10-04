using StackDuel.Application.Queries;
using StackDuel.Application.Tracks.Dtos;

namespace StackDuel.Application.Queries.Tracks.GetTracks;

public sealed record GetTracksQuery : IQuery<IReadOnlyList<TrackDto>>;