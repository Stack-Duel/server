using Ardalis.Result;
using Moq;
using StackDuel.Application.ValueTypes.Dtos;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using CreateValueTypeDefinitionCommand = StackDuel.Application.Commands.ValueTypes.CreateValueTypeDefinition.CreateValueTypeDefinitionCommand;
using CreateValueTypeDefinitionHandler = StackDuel.Application.Commands.ValueTypes.CreateValueTypeDefinition.CreateValueTypeDefinitionHandler;
using CreateValueTypeDefinitionValidator = StackDuel.Application.Commands.ValueTypes.CreateValueTypeDefinition.CreateValueTypeDefinitionValidator;

namespace StackDuel.Application.Tests.Commands.ValueTypes.CreateValueTypeDefinition;

public class CreateValueTypeDefinitionHandlerTests
{
    private Mock<IValueTypeDefinitionRepository> _repository = null!;
    private CreateValueTypeDefinitionHandler _handler = null!;

    public CreateValueTypeDefinitionHandlerTests()
    {
        _repository = new Mock<IValueTypeDefinitionRepository>();
        _handler = new CreateValueTypeDefinitionHandler(new CreateValueTypeDefinitionValidator(), _repository.Object);

        _repository
            .Setup(x => x.FindByKeyAsync(It.IsAny<ValueTypeKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValueTypeDefinition?)null);
    }

    private static ValueTypeSpecDto IntegerSpec() => new("integer", null, null, null, null, null, null);

    private static CreateValueTypeDefinitionCommand ValidCommand(string key = "positive_integer") =>
        new(key, "Positive Integer", "A whole number greater than zero.", IntegerSpec());

    [Fact]
    public async Task Handle_DuplicateKey_ReturnsInvalidAndDoesNotPersist()
    {
        _repository
            .Setup(x => x.FindByKeyAsync(It.IsAny<ValueTypeKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValueTypeDefinition(new ValueTypeKey("integer"), "Integer", null, new ValueTypeSpec(ValueTypeKind.Integer), isBuiltIn: true));

        Result<Guid> result = await _handler.Handle(ValidCommand("integer"), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _repository.Verify(
            x => x.AddAsync(It.IsAny<ValueTypeDefinition>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesAndPersistsAsNonBuiltIn()
    {
        ValueTypeDefinition? added = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<ValueTypeDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<ValueTypeDefinition, CancellationToken>((definition, _) => added = definition)
            .Returns(Task.CompletedTask);

        Result<Guid> result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.False(added!.IsBuiltIn);
        Assert.Equal("positive_integer", added.Key.Value);
        Assert.Equal(added.Id, result.Value);
    }

    [Fact]
    public async Task Handle_InvalidSpec_CharsetOnInteger_ReturnsInvalid()
    {
        var invalidSpec = new ValueTypeSpecDto("integer", null, null, null, null, "abc", null);

        Result<Guid> result = await _handler.Handle(ValidCommand() with { Spec = invalidSpec }, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _repository.Verify(
            x => x.AddAsync(It.IsAny<ValueTypeDefinition>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_InvalidSpec_ArrayWithoutElement_ReturnsInvalid()
    {
        var invalidSpec = new ValueTypeSpecDto("array", null, null, null, null, null, null);

        Result<Guid> result = await _handler.Handle(ValidCommand() with { Spec = invalidSpec }, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = ValidCommand() with { Key = "" };

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _repository.Verify(
            x => x.FindByKeyAsync(It.IsAny<ValueTypeKey>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}