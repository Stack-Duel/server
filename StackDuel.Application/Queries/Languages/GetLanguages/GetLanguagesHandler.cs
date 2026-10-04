using Ardalis.Result;
using StackDuel.Application.Languages;
using StackDuel.Application.Languages.Dtos;

namespace StackDuel.Application.Queries.Languages.GetLanguages;

internal sealed class GetLanguagesHandler(ILanguageReadRepository languageReadRepository)
    : IQueryHandler<GetLanguagesQuery, IReadOnlyList<LanguageDto>>
{
    public async Task<Result<IReadOnlyList<LanguageDto>>> Handle(
        GetLanguagesQuery request,
        CancellationToken cancellationToken
    )
    {
        IEnumerable<Domain.Languages.Entities.Language> languages =
            await languageReadRepository.GetActiveLanguagesAsync(cancellationToken);

        IReadOnlyList<LanguageDto> result =
        [
            .. languages.Select(language => new LanguageDto(
                language.Id,
                language.Name.Value,
                [
                    .. language
                        .Versions.Where(version => version.IsActive)
                        .Select(version => new LanguageVersionDto(version.Id, version.Version.Value)),
                ]
            )),
        ];

        return Result.Success(result);
    }
}