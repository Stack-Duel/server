using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Languages;

public interface ILanguageRepository : IRepository<Entities.Language>
{
    Task<Entities.Language?> FindBySlugAsync(LanguageSlug slug, CancellationToken cancellationToken = default);
}