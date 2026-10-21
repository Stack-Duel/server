using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.ValueTypes.DeleteValueTypeDefinition;

internal sealed record DeleteValueTypeDefinitionCommand(Guid Id) : ICommand, IAuditableCommand
{
    public string AuditAction => "valuetype.deleted";
    public string? AuditTargetType => "value-type";
    public string? AuditTargetId => Id.ToString();
    public object? AuditDetails => null;
}