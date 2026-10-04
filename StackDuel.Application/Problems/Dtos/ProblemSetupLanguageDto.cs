namespace StackDuel.Application.Problems.Dtos;

public sealed record ProblemSetupLanguageVersionDto(Guid Id, string Version);

public sealed record ProblemSetupLanguageDto(
    Guid Id,
    string Name,
    IEnumerable<ProblemSetupLanguageVersionDto> Versions
);