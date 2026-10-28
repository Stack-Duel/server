using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems;

public interface IProblemRepository : IRepository<Problem>
{
    Task<Problem?> FindBySetupIdAsync(Guid setupId, CancellationToken cancellationToken = default);

    Task<Problem?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProblemTag>> FindOrCreateTagsAsync(
        IEnumerable<Tag> tags,
        CancellationToken cancellationToken = default
    );
}