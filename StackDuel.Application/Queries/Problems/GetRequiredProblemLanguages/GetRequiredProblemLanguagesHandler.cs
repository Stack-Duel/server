using Ardalis.Result;
using StackDuel.Application.Problems.RequiredLanguages;
using StackDuel.Application.Problems.RequiredLanguages.Dtos;

namespace StackDuel.Application.Queries.Problems.GetRequiredProblemLanguages;

internal sealed class GetRequiredProblemLanguagesHandler(IRequiredProblemLanguageReadRepository readRepository)
    : IQueryHandler<GetRequiredProblemLanguagesQuery, IReadOnlyList<RequiredProblemLanguageAdminDto>>
{
    public async Task<Result<IReadOnlyList<RequiredProblemLanguageAdminDto>>> Handle(
        GetRequiredProblemLanguagesQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<RequiredProblemLanguageAdminDto> languages = await readRepository.GetAllAsync(cancellationToken);
        return Result.Success(languages);
    }
}