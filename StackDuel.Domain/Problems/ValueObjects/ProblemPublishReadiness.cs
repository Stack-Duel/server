namespace StackDuel.Domain.Problems.ValueObjects;

public sealed record ProblemPublishReadiness(
    int SetupCount,
    int SetupsMissingReferenceSolution,
    int SampleTestCaseCount,
    bool HasGenerationParameters
)
{
    public const int RequirementCount = 4;

    public bool HasSetups => SetupCount > 0;

    public bool HasReferenceSolutions => HasSetups && SetupsMissingReferenceSolution == 0;

    public bool HasSampleTestCases => SampleTestCaseCount > 0;

    public int RequirementsMet =>
        Convert.ToInt32(HasSetups)
        + Convert.ToInt32(HasReferenceSolutions)
        + Convert.ToInt32(HasSampleTestCases)
        + Convert.ToInt32(HasGenerationParameters);

    public bool IsReady => RequirementsMet == RequirementCount;
}