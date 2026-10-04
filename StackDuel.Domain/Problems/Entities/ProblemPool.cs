using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Entities;

public sealed class ProblemPool : Entity
{
    public ProblemPool(string key, string name, string? description = null)
    {
        Key = ValidateKey(key);
        Name = ValidateName(name);
        Description = NormalizeDescription(description);
        CreatedAt = DateTime.UtcNow;
    }

    private ProblemPool()
    {
        Key = null!;
        Name = null!;
    }

    public void Rename(string name)
    {
        Name = ValidateName(name);
    }

    public void UpdateDescription(string? description)
    {
        Description = NormalizeDescription(description);
    }

    public void AddProblem(Guid problemId)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        if (!_items.Any(item => item.ProblemId == problemId))
        {
            int nextPosition = _items.Count == 0 ? 0 : _items.Max(item => item.Position) + 1;
            _items.Add(new ProblemPoolItem(problemId, nextPosition));
        }
    }

    public void RemoveProblem(Guid problemId)
    {
        ProblemPoolItem? item = _items.FirstOrDefault(item => item.ProblemId == problemId);
        if (item is not null)
            _items.Remove(item);
    }

    /// <summary>
    /// Reassigns each member's position to match <paramref name="orderedProblemIds"/>. The given list
    /// must contain exactly the pool's current members (same count, same ids) — it's a reordering of
    /// the existing set, not a way to add or remove members.
    /// </summary>
    public void Reorder(IReadOnlyList<Guid> orderedProblemIds)
    {
        if (orderedProblemIds is null)
            throw new ArgumentNullException(nameof(orderedProblemIds));

        if (orderedProblemIds.Count != _items.Count || orderedProblemIds.Distinct().Count() != _items.Count)
            throw new ArgumentException(
                "Reorder must include every current pool member exactly once.",
                nameof(orderedProblemIds)
            );

        Dictionary<Guid, ProblemPoolItem> itemsByProblemId = _items.ToDictionary(item => item.ProblemId);

        for (int position = 0; position < orderedProblemIds.Count; position++)
        {
            if (!itemsByProblemId.TryGetValue(orderedProblemIds[position], out ProblemPoolItem? item))
                throw new ArgumentException(
                    "Reorder must include every current pool member exactly once.",
                    nameof(orderedProblemIds)
                );

            item.SetPosition(position);
        }
    }

    private static string ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Pool key must not be empty.", nameof(key));

        string normalized = key.Trim().ToLowerInvariant();

        if (normalized.Length > MaxKeyLength)
            throw new ArgumentException($"Pool key must be {MaxKeyLength} characters or fewer.", nameof(key));

        return normalized;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pool name is required.", nameof(name));

        if (name.Length > MaxNameLength)
            throw new ArgumentException($"Pool name must be {MaxNameLength} characters or fewer.", nameof(name));

        return name.Trim();
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public static readonly int MaxKeyLength = 50;
    public static readonly int MaxNameLength = 100;

    public string Key { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Guid> ProblemIds =>
        _items.OrderBy(item => item.Position).Select(item => item.ProblemId).ToList().AsReadOnly();

    private readonly List<ProblemPoolItem> _items = [];
}