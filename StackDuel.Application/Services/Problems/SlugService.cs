using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Services.Problems;

public interface ISlugService
{
    Slug GenerateSlugFromTitle(Title title);

    Slug GenerateDraftSlug();
}

internal sealed class SlugService : ISlugService
{
    public Slug GenerateSlugFromTitle(Title title) => Slug.FromTitle(title);

    /*
     * Generates a guid for draft slugs since we don't want to take up title space with draft problems.
     * This becomes a problem when a user creates a problem and it says the title already exists, but there
     * is no visibility of draft problems for other users.
     */
    public Slug GenerateDraftSlug() => new(Guid.NewGuid().ToString());
}