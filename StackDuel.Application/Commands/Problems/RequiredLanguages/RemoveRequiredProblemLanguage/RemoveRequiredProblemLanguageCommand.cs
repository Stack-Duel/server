using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.RequiredLanguages.RemoveRequiredProblemLanguage;

internal sealed record RemoveRequiredProblemLanguageCommand(Guid Id) : ICommand, IAuditableCommand
{
    public string AuditAction => "problem-required-language.removed";
    public string? AuditTargetType => "required-problem-language";
    public string? AuditTargetId => Id.ToString();
    public object? AuditDetails => null;
}