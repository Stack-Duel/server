using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Tests.SeedWork;

public class EntityTests
{
    private sealed class TestEntity : Entity
    {
        public void SetId(Guid id) => Id = id;
    }

    private sealed class OtherTestEntity : Entity
    {
        public void SetId(Guid id) => Id = id;
    }

    [Fact]
    public void Constructor_GeneratesNonEmptyId()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Constructor_GeneratesUniqueIdsForDifferentInstances()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        var entity = new TestEntity();

        Assert.True(entity.Equals(entity));
    }

    [Fact]
    public void Equals_SameTypeAndId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity();
        first.SetId(id);
        var second = new TestEntity();
        second.SetId(id);

        Assert.True(first.Equals(second));
    }

    [Fact]
    public void Equals_SameTypeDifferentId_ReturnsFalse()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity();
        entity.SetId(id);
        var other = new OtherTestEntity();
        other.SetId(id);

        Assert.False(entity.Equals(other));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var entity = new TestEntity();

        Assert.False(entity.Equals(null));
    }

    [Fact]
    public void Equals_NonEntityObject_ReturnsFalse()
    {
        var entity = new TestEntity();

        Assert.False(entity.Equals("not an entity"));
    }

    [Fact]
    public void GetHashCode_ReturnsIdHashCode()
    {
        var entity = new TestEntity();

        Assert.Equal(entity.Id.GetHashCode(), entity.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        TestEntity? left = null;
        TestEntity? right = null;

        Assert.True(left == right);
    }

    [Fact]
    public void EqualityOperator_LeftNullRightNotNull_ReturnsFalse()
    {
        TestEntity? left = null;
        var right = new TestEntity();

        Assert.False(left == right);
    }

    [Fact]
    public void EqualityOperator_RightNullLeftNotNull_ReturnsFalse()
    {
        var left = new TestEntity();
        TestEntity? right = null;

        Assert.False(left == right);
    }

    [Fact]
    public void EqualityOperator_SameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var left = new TestEntity();
        left.SetId(id);
        var right = new TestEntity();
        right.SetId(id);

        Assert.True(left == right);
    }

    [Fact]
    public void InequalityOperator_DifferentId_ReturnsTrue()
    {
        var left = new TestEntity();
        var right = new TestEntity();

        Assert.True(left != right);
    }
}