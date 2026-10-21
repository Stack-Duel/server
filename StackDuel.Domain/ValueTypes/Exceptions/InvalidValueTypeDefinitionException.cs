namespace StackDuel.Domain.ValueTypes.Exceptions;

public sealed class InvalidValueTypeDefinitionException(string message) : Exception(message);