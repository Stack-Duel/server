using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Services.Problems;

public interface ISlugService
{
    Slug GenerateSlugFromTitle(Title title);

    Slug GenerateDraftSlug();

    Task<Slug> ResolveUniqueSlugFromTitleAsync(Title title, CancellationToken cancellationToken = default);
}

internal sealed class SlugService(IProblemReadRepository problemReadRepository) : ISlugService
{
    public Slug GenerateSlugFromTitle(Title title) => Slug.FromTitle(title);

    public async Task<Slug> ResolveUniqueSlugFromTitleAsync(
        Title title,
        CancellationToken cancellationToken = default
    )
    {
        Slug baseSlug = Slug.FromTitle(title);
        Slug slug = baseSlug;
        int suffix = 2;

        while (await problemReadRepository.SlugExistsAsync(slug, cancellationToken))
        {
            slug = new Slug($"{baseSlug.Value}-{suffix}");
            suffix++;
        }

        return slug;
    }

    /*
     * Generates a guid for draft slugs since we don't want to take up title space with draft problems.
     * This becomes a problem when a user creates a problem and it says the title already exists, but there
     * is no visibility of draft problems for other users.
     */
    public Slug GenerateDraftSlug() => new(Guid.NewGuid().ToString());
}