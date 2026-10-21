using Ardalis.Result;
using Moq;
using StackDuel.Application.ValueTypes.Dtos;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using GetValueTypeMetadataHandler = StackDuel.Application.Queries.ValueTypes.GetValueTypeMetadata.GetValueTypeMetadataHandler;
using GetValueTypeMetadataQuery = StackDuel.Application.Queries.ValueTypes.GetValueTypeMetadata.GetValueTypeMetadataQuery;

namespace StackDuel.Application.Tests.Queries.ValueTypes.GetValueTypeMetadata;

public class GetValueTypeMetadataHandlerTests
{
    private Mock<IValueTypeDefinitionRepository> _repository = null!;
    private GetValueTypeMetadataHandler _handler = null!;

    public GetValueTypeMetadataHandlerTests()
    {
        _repository = new Mock<IValueTypeDefinitionRepository>();
        _handler = new GetValueTypeMetadataHandler(_repository.Object);

        _repository
            .Setup(x => x.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ValueTypeDefinition>)[]);
    }

    [Fact]
    public async Task Handle_ReturnsMetadataForEveryKind()
    {
        Result<ValueTypeMetadataDto> result = await _handler.Handle(
            new GetValueTypeMetadataQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Kinds.Count);
        Assert.Contains(result.Value.Kinds, k => k.Kind == "integer");
        Assert.Contains(result.Value.Kinds, k => k.Kind == "array");
    }

    [Fact]
    public async Task Handle_ArrayKind_AcceptsElementField()
    {
        Result<ValueTypeMetadataDto> result = await _handler.Handle(
            new GetValueTypeMetadataQuery(),
            CancellationToken.None
        );

        var array = result.Value.Kinds.Single(k => k.Kind == "array");
        Assert.Contains("element", array.Fields.Select(field => field.Name));
        Assert.Equal("valuetype", array.Fields.Single(field => field.Name == "element").ValueKind);
    }

    [Fact]
    public async Task Handle_BooleanKind_AcceptsNoFields()
    {
        Result<ValueTypeMetadataDto> result = await _handler.Handle(
            new GetValueTypeMetadataQuery(),
            CancellationToken.None
        );

        var boolean = result.Value.Kinds.Single(k => k.Kind == "boolean");
        Assert.Empty(boolean.Fields);
    }

    [Fact]
    public async Task Handle_IncludesDefinitionsFromRepository()
    {
        var result = await _handler.Handle(new GetValueTypeMetadataQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Definitions);
    }
}