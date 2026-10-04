using StackDuel.Domain.ExecutionAssets.Exceptions;
using StackDuel.Domain.ExecutionAssets.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ExecutionAssets.Entities;

public sealed class AdditionalFileBundle : AggregateRoot
{
    public AdditionalFileBundle(AdditionalFileBundleName name, byte[] content)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Content = Validate(content);
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateContent(byte[] content)
    {
        Content = Validate(content);
    }

    private static byte[] Validate(byte[] content) =>
        content is { Length: > 0 }
            ? content
            : throw new InvalidAdditionalFileBundleContentException("Content must not be empty.");

    private AdditionalFileBundle() { }

    public AdditionalFileBundleName Name { get; private set; } = null!;
    public byte[] Content { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
}