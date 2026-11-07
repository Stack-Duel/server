namespace __NAMESPACE__;

public sealed record Sample
{
    public Sample(__VALUE_TYPE__ value)
    {
        // TODO: validate, throw a domain exception from Exceptions/ on failure
        Value = value;
    }

    public static implicit operator __VALUE_TYPE__(Sample valueObject) => valueObject.Value;

    public override string ToString() => Value?.ToString() ?? string.Empty;

    public __VALUE_TYPE__ Value { get; }
}
