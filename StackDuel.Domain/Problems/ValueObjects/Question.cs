using StackDuel.Domain.Problems.Exceptions;

namespace StackDuel.Domain.Problems.ValueObjects;

public sealed record Question
{
    public Question(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidQuestionException("Question cannot be empty.");

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new InvalidQuestionException($"Question must be between {MinLength} and {MaxLength} characters.");

        Value = value;
    }

    public static implicit operator string(Question question) => question.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 10000;
    public static readonly int MinLength = 50;
    public string Value { get; }
}