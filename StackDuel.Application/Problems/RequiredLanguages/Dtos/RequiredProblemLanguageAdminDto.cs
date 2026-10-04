namespace StackDuel.Application.Problems.RequiredLanguages.Dtos;

public sealed record RequiredProblemLanguageAdminDto(
    Guid Id,
    Guid LanguageVersionId,
    string LanguageName,
    string VersionLabel,
    int SortOrder,
    Guid? TrackId,
    string? TrackKey,
    string? TrackName
);