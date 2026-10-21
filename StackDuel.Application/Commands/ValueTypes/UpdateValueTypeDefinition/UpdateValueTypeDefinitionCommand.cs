using StackDuel.Application.Audit;
using StackDuel.Application.Commands;
using StackDuel.Application.ValueTypes.Dtos;

namespace StackDuel.Application.Commands.ValueTypes.UpdateValueTypeDefinition;

internal sealed record UpdateValueTypeDefinitionCommand(
    Guid Id,
    string Name,
    string? Description,
    ValueTypeSpecDto? Spec
) : ICommand, IAuditableCommand
{
    public string AuditAction => "valuetype.updated";
    public string? AuditTargetType => "value-type";
    public string? AuditTargetId => Id.ToString();
    public object? AuditDetails => new { Name, Description, Restructured = Spec is not null };
}