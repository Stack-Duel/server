namespace StackDuel.Infrastructure.ExecutionEngine.Assert;

public enum AssertStrategy
{
    ExactMatch = 1,
    FloatTolerance = 2,
    SetEquality = 3,
    Regex = 4,
}

/// <summary>Infrastructure-only POCO — not a domain entity.</summary>
public sealed class AssertStepConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PipelineStepId { get; set; }
    public AssertStrategy Strategy { get; set; } = AssertStrategy.ExactMatch;
    public decimal? Tolerance { get; set; }
    public bool CaseSensitive { get; set; } = true;
}