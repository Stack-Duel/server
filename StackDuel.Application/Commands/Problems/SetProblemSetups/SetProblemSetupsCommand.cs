using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.SetProblemSetups;

public sealed record ProblemSetupInputDto(
    Guid? LanguageVersionId,
    string? Language,
    string? LanguageVersion,
    string InitialCode,
    string? FunctionName,
    string ReferenceSolutionCode
);

internal sealed record SetProblemSetupsCommand(Guid ProblemId, IReadOnlyList<ProblemSetupInputDto> Setups)
    : ICommand,
        IAuditableCommand
{
    public string AuditAction => "problem.setups-saved";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails =>
        new
        {
            Languages = Setups.Select(setup => setup.Language ?? setup.LanguageVersionId?.ToString()),
        };
}