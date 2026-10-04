using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Entities;

public sealed class ProblemHistory : Entity
{
    internal ProblemHistory(
        Title title,
        Question question,
        Difficulty difficulty,
        TimeLimit timeLimit,
        MemoryLimit memoryLimit
    )
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Question = question ?? throw new ArgumentNullException(nameof(question));
        Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
        TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
        MemoryLimit = memoryLimit ?? throw new ArgumentNullException(nameof(memoryLimit));
        CreatedAt = DateTime.UtcNow;
    }

    private ProblemHistory() { }

    public Title Title { get; private set; } = null!;
    public Question Question { get; private set; } = null!;
    public Difficulty Difficulty { get; private set; } = null!;
    public TimeLimit TimeLimit { get; private set; } = null!;
    public MemoryLimit MemoryLimit { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
}