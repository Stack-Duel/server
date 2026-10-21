using Moq;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using StackDuel.Infrastructure.ValueTypes;

namespace StackDuel.Infrastructure.Tests.ValueTypes;

public class ValueTypeSpecResolverTests
{
    private readonly Mock<IValueTypeDefinitionRepository> _repository = new();
    private readonly ValueTypeSpecResolver _resolver;

    public ValueTypeSpecResolverTests() => _resolver = new ValueTypeSpecResolver(_repository.Object);

    private static ValueTypeDefinition Definition(string key, ValueTypeSpec spec) =>
        new(new ValueTypeKey(key), key, null, spec, isBuiltIn: false);

    [Fact]
    public async Task ResolveAsync_MultipleKeys_ResolvesAllInOneRepositoryCall()
    {
        ValueTypeDefinition integerDefinition = Definition("integer", new ValueTypeSpec(ValueTypeKind.Integer));
        ValueTypeDefinition stringDefinition = Definition("string", new ValueTypeSpec(ValueTypeKind.String));
        _repository
            .Setup(x => x.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([integerDefinition, stringDefinition]);

        IReadOnlyDictionary<string, ValueTypeSpec> result = await _resolver.ResolveAsync(
            ["integer", "string"],
            CancellationToken.None
        );

        Assert.Equal(2, result.Count);
        Assert.Equal(ValueTypeKind.Integer, result["integer"].Kind);
        Assert.Equal(ValueTypeKind.String, result["string"].Kind);
        _repository.Verify(x => x.GetAllOrderedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_UnknownKey_IsOmittedFromTheResult()
    {
        ValueTypeDefinition integerDefinition = Definition("integer", new ValueTypeSpec(ValueTypeKind.Integer));
        _repository.Setup(x => x.GetAllOrderedAsync(It.IsAny<CancellationToken>())).ReturnsAsync([integerDefinition]);

        IReadOnlyDictionary<string, ValueTypeSpec> result = await _resolver.ResolveAsync(
            ["integer", "no_such_type"],
            CancellationToken.None
        );

        Assert.Single(result);
        Assert.True(result.ContainsKey("integer"));
        Assert.False(result.ContainsKey("no_such_type"));
    }

    [Fact]
    public async Task ResolveAsync_EmptyInput_ReturnsEmptyAndDoesNotCallTheRepository()
    {
        IReadOnlyDictionary<string, ValueTypeSpec> result = await _resolver.ResolveAsync([], CancellationToken.None);

        Assert.Empty(result);
        _repository.Verify(x => x.GetAllOrderedAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}