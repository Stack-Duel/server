using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.RequiredLanguages.Entities;

/// <summary>
/// A language version every new problem must ship a reference solution for before it can be
/// submitted for validation. Admin-managed, global — not scoped to a particular problem.
/// </summary>
public sealed class RequiredProblemLanguage : AggregateRoot
{
    private RequiredProblemLanguage(Guid languageVersionId, int sortOrder)
    {
        LanguageVersionId =
            languageVersionId != Guid.Empty
                ? languageVersionId
                : throw new ArgumentException("Language version id must not be empty.", nameof(languageVersionId));

        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
    }

    private RequiredProblemLanguage() { }

    public static RequiredProblemLanguage Create(Guid languageVersionId, int sortOrder) =>
        new(languageVersionId, sortOrder);

    public void UpdateSortOrder(int sortOrder) => SortOrder = sortOrder;

    public Guid LanguageVersionId { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
}