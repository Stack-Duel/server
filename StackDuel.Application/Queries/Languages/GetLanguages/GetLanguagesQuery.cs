using StackDuel.Application.Languages.Dtos;

namespace StackDuel.Application.Queries.Languages.GetLanguages;

public sealed record GetLanguagesQuery : IQuery<IReadOnlyList<LanguageDto>>;