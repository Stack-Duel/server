using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Entities;

public sealed class ProblemTag : Entity
{
    public ProblemTag(Tag name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    private ProblemTag()
    {
        Name = null!;
    }

    public Tag Name { get; private set; }

    public IReadOnlyCollection<Problem> Problems => _problems.AsReadOnly();

    private readonly List<Problem> _problems = [];
}