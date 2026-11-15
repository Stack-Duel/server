using StackDuel.Application.Tracks.Dtos;

namespace StackDuel.Application.StudioProblems.Dtos;

public sealed record InitialStudioProblemSetupDto(IEnumerable<TrackDto> AvailableTracks);
