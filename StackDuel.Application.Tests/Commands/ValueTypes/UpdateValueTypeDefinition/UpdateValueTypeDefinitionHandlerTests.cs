using Ardalis.Result;
using Moq;
using StackDuel.Application.ValueTypes.Dtos;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using UpdateValueTypeDefinitionCommand = StackDuel.Application.Commands.ValueTypes.UpdateValueTypeDefinition.UpdateValueTypeDefinitionCommand;
using UpdateValueTypeDefinitionHandler = StackDuel.Application.Commands.ValueTypes.UpdateValueTypeDefinition.UpdateValueTypeDefinitionHandler;
using UpdateValueTypeDefinitionValidator = StackDuel.Application.Commands.ValueTypes.UpdateValueTypeDefinition.UpdateValueTypeDefinitionValidator;

namespace StackDuel.Application.Tests.Commands.ValueTypes.UpdateValueTypeDefinition;

public class UpdateValueTypeDefinitionHandlerTests
{
    private Mock<IValueTypeDefinitionRepository> _repository = null!;
    private UpdateValueTypeDefinitionHandler _handler = null!;

    public UpdateValueTypeDefinitionHandlerTests()
    {
        _repository = new Mock<IValueTypeDefinitionRepository>();
        _handler = new UpdateValueTypeDefinitionHandler(new UpdateValueTypeDefinitionValidator(), _repository.Object);
    }

    private static ValueTypeDefinition CreateBuiltIn(string key = "integer") =>
        new(new ValueTypeKey(key), "Integer", null, new ValueTypeSpec(ValueTypeKind.Integer), isBuiltIn: true);

    private static ValueTypeDefinition CreateCustom(string key = "positive_integer") =>
        new(new ValueTypeKey(key), "Positive Integer", null, new ValueTypeSpec(ValueTypeKind.Integer), isBuiltIn: false);

    private void GivenDefinition(ValueTypeDefinition definition) =>
        _repository.Setup(x => x.FindByIdAsync(definition.Id, It.IsAny<CancellationToken>())).ReturnsAsync(definition);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValueTypeDefinition?)null);

        Result result = await _handler.Handle(
            new UpdateValueTypeDefinitionCommand(Guid.NewGuid(), "New Name", null, null),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_RenameBuiltIn_Succeeds()
    {
        var definition = CreateBuiltIn();
        GivenDefinition(definition);

        Result result = await _handler.Handle(
            new UpdateValueTypeDefinitionCommand(definition.Id, "Whole Number", "An updated description.", null),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("Whole Number", definition.Name);
        _repository.Verify(x => x.UpdateAsync(definition, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RestructureBuiltIn_ReturnsInvalidAndDoesNotPersist()
    {
        var definition = CreateBuiltIn();
        GivenDefinition(definition);
        var newSpec = new ValueTypeSpecDto("double", null, null, null, null, null, null);

        Result result = await _handler.Handle(
            new UpdateValueTypeDefinitionCommand(definition.Id, "Integer", null, newSpec),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(ValueTypeKind.Integer, definition.Spec.Kind);
        _repository.Verify(x => x.UpdateAsync(It.IsAny<ValueTypeDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RestructureCustomType_Succeeds()
    {
        var definition = CreateCustom();
        GivenDefinition(definition);
        var newSpec = new ValueTypeSpecDto("double", null, null, null, null, null, null);

        Result result = await _handler.Handle(
            new UpdateValueTypeDefinitionCommand(definition.Id, "Positive Number", null, newSpec),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(ValueTypeKind.Double, definition.Spec.Kind);
        _repository.Verify(x => x.UpdateAsync(definition, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullSpec_LeavesStructureUnchanged()
    {
        var definition = CreateCustom();
        GivenDefinition(definition);

        await _handler.Handle(
            new UpdateValueTypeDefinitionCommand(definition.Id, "Renamed", null, null),
            CancellationToken.None
        );

        Assert.Equal(ValueTypeKind.Integer, definition.Spec.Kind);
    }

    [Fact]
    public async Task Handle_InvalidSpec_ArrayWithoutElement_ReturnsInvalid()
    {
        var definition = CreateCustom();
        GivenDefinition(definition);
        var invalidSpec = new ValueTypeSpecDto("array", null, null, null, null, null, null);

        Result result = await _handler.Handle(
            new UpdateValueTypeDefinitionCommand(definition.Id, "Positive Number", null, invalidSpec),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _repository.Verify(x => x.UpdateAsync(It.IsAny<ValueTypeDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}