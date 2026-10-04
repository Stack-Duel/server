using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.RequiredLanguages.AddRequiredProblemLanguage;

internal sealed record AddRequiredProblemLanguageCommand(Guid LanguageVersionId) : ICommand<Guid>, IAuditableCommand
{
    public string AuditAction => "problem-required-language.added";
    public string? AuditTargetType => "required-problem-language";
    public string? AuditTargetId => LanguageVersionId.ToString();
    public object? AuditDetails => new { LanguageVersionId };
}