using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ExecutionAssets.Exceptions;

public sealed class InvalidAdditionalFileBundleNameException : DomainException
{
    public InvalidAdditionalFileBundleNameException(string reason)
        : base($"Additional file bundle name is invalid: {reason}") { }
}