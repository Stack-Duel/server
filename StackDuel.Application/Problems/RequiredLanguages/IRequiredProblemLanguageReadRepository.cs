using StackDuel.Application.Problems.RequiredLanguages.Dtos;

namespace StackDuel.Application.Problems.RequiredLanguages;

public interface IRequiredProblemLanguageReadRepository
{
    Task<IReadOnlyList<RequiredProblemLanguageAdminDto>> GetAllAsync(CancellationToken cancellationToken = default);
}