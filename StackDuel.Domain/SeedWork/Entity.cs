using System.Diagnostics.CodeAnalysis;

namespace StackDuel.Domain.SeedWork;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (GetType() != other.GetType())
            return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

#pragma warning disable S3875
    public static bool operator ==(Entity? left, Entity? right)
#pragma warning restore S3875
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}