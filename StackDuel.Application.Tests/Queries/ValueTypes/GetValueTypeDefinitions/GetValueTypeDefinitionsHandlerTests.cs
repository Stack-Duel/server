using Ardalis.Result;
using Moq;
using StackDuel.Application.ValueTypes.Dtos;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using GetValueTypeDefinitionsHandler = StackDuel.Application.Queries.ValueTypes.GetValueTypeDefinitions.GetValueTypeDefinitionsHandler;
using GetValueTypeDefinitionsQuery = StackDuel.Application.Queries.ValueTypes.GetValueTypeDefinitions.GetValueTypeDefinitionsQuery;

namespace StackDuel.Application.Tests.Queries.ValueTypes.GetValueTypeDefinitions;

public class GetValueTypeDefinitionsHandlerTests
{
    private Mock<IValueTypeDefinitionRepository> _repository = null!;
    private GetValueTypeDefinitionsHandler _handler = null!;

    public GetValueTypeDefinitionsHandlerTests()
    {
        _repository = new Mock<IValueTypeDefinitionRepository>();
        _handler = new GetValueTypeDefinitionsHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_MapsDefinitionsToDtos()
    {
        var definition = new ValueTypeDefinition(
            new ValueTypeKey("integer"),
            "Integer",
            "A whole number.",
            new ValueTypeSpec(ValueTypeKind.Integer),
            isBuiltIn: true
        );
        _repository.Setup(x => x.GetAllOrderedAsync(It.IsAny<CancellationToken>())).ReturnsAsync([definition]);

        Result<IReadOnlyList<ValueTypeDefinitionDto>> result = await _handler.Handle(
            new GetValueTypeDefinitionsQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        ValueTypeDefinitionDto dto = result.Value.Single();
        Assert.Equal(definition.Id, dto.Id);
        Assert.Equal("integer", dto.Key);
        Assert.Equal("Integer", dto.Name);
        Assert.True(dto.IsBuiltIn);
        Assert.Equal("integer", dto.Spec.Kind);
    }

    [Fact]
    public async Task Handle_NoDefinitions_ReturnsEmptyList()
    {
        _repository.Setup(x => x.GetAllOrderedAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Result<IReadOnlyList<ValueTypeDefinitionDto>> result = await _handler.Handle(
            new GetValueTypeDefinitionsQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}