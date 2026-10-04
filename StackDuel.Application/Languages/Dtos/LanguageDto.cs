namespace StackDuel.Application.Languages.Dtos;

public sealed record LanguageVersionDto(Guid Id, string Version);

public sealed record LanguageDto(Guid Id, string Name, IReadOnlyList<LanguageVersionDto> Versions);