using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ExecutionAssets.Exceptions;

public sealed class InvalidAdditionalFileBundleContentException : DomainException
{
    public InvalidAdditionalFileBundleContentException(string reason)
        : base($"Additional file bundle content is invalid: {reason}") { }
}