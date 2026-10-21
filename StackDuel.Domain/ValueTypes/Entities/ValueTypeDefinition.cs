using StackDuel.Domain.SeedWork;
using StackDuel.Domain.ValueTypes.Exceptions;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Domain.ValueTypes.Entities;

public sealed class ValueTypeDefinition : AggregateRoot
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    public ValueTypeDefinition(ValueTypeKey key, string name, string? description, ValueTypeSpec spec, bool isBuiltIn)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Spec = spec ?? throw new ArgumentNullException(nameof(spec));
        IsBuiltIn = isBuiltIn;
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        CreatedAt = DateTime.UtcNow;
    }

    public void Describe(string name, string? description)
    {
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restructure(ValueTypeSpec spec)
    {
        if (IsBuiltIn)
            throw new InvalidValueTypeDefinitionException(
                $"The structure of the built-in value type '{Key.Value}' cannot be changed."
            );

        Spec = spec ?? throw new ArgumentNullException(nameof(spec));
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidValueTypeDefinitionException("Value type name must not be empty.");

        string trimmed = name.Trim();

        return trimmed.Length <= MaxNameLength
            ? trimmed
            : throw new InvalidValueTypeDefinitionException(
                $"Value type name must be at most {MaxNameLength} characters."
            );
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        string trimmed = description.Trim();

        return trimmed.Length <= MaxDescriptionLength
            ? trimmed
            : throw new InvalidValueTypeDefinitionException(
                $"Value type description must be at most {MaxDescriptionLength} characters."
            );
    }

    private ValueTypeDefinition() { }

    public ValueTypeKey Key { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ValueTypeSpec Spec { get; private set; } = null!;
    public bool IsBuiltIn { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}