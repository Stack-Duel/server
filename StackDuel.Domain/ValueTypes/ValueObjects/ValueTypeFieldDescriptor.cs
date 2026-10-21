namespace StackDuel.Domain.ValueTypes.ValueObjects;

public enum ValueTypeFieldValueKind
{
    Number = 1,
    Integer = 2,
    Text = 3,
    ValueType = 4,
}

public sealed record ValueTypeFieldDescriptor(string Name, ValueTypeFieldValueKind ValueKind);