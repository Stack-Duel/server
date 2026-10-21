using StackDuel.Application.Audit;
using StackDuel.Application.Commands;
using StackDuel.Application.ValueTypes.Dtos;

namespace StackDuel.Application.Commands.ValueTypes.CreateValueTypeDefinition;

internal sealed record CreateValueTypeDefinitionCommand(
    string Key,
    string Name,
    string? Description,
    ValueTypeSpecDto Spec
) : ICommand<Guid>, IAuditableCommand
{
    public string AuditAction => "valuetype.created";
    public string? AuditTargetType => "value-type";
    public string? AuditTargetId => null;
    public object? AuditDetails => new { Key, Name, Description };
}