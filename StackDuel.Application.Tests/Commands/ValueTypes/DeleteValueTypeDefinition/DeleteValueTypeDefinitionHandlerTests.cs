using Ardalis.Result;
using Moq;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using DeleteValueTypeDefinitionCommand = StackDuel.Application.Commands.ValueTypes.DeleteValueTypeDefinition.DeleteValueTypeDefinitionCommand;
using DeleteValueTypeDefinitionHandler = StackDuel.Application.Commands.ValueTypes.DeleteValueTypeDefinition.DeleteValueTypeDefinitionHandler;
using DeleteValueTypeDefinitionValidator = StackDuel.Application.Commands.ValueTypes.DeleteValueTypeDefinition.DeleteValueTypeDefinitionValidator;

namespace StackDuel.Application.Tests.Commands.ValueTypes.DeleteValueTypeDefinition;

public class DeleteValueTypeDefinitionHandlerTests
{
    private Mock<IValueTypeDefinitionRepository> _repository = null!;
    private Mock<IValueTypeUsageRepository> _usageRepository = null!;
    private DeleteValueTypeDefinitionHandler _handler = null!;

    public DeleteValueTypeDefinitionHandlerTests()
    {
        _repository = new Mock<IValueTypeDefinitionRepository>();
        _usageRepository = new Mock<IValueTypeUsageRepository>();
        _handler = new DeleteValueTypeDefinitionHandler(
            new DeleteValueTypeDefinitionValidator(),
            _repository.Object,
            _usageRepository.Object
        );
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

        Result result = await _handler.Handle(new DeleteValueTypeDefinitionCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_BuiltIn_ReturnsInvalidAndDoesNotCheckUsageOrRemove()
    {
        var definition = CreateBuiltIn();
        GivenDefinition(definition);

        Result result = await _handler.Handle(new DeleteValueTypeDefinitionCommand(definition.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _usageRepository.Verify(x => x.IsKeyInUseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(x => x.RemoveAsync(It.IsAny<ValueTypeDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CustomTypeInUse_ReturnsInvalidAndDoesNotRemove()
    {
        var definition = CreateCustom();
        GivenDefinition(definition);
        _usageRepository
            .Setup(x => x.IsKeyInUseAsync(definition.Key.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result result = await _handler.Handle(new DeleteValueTypeDefinitionCommand(definition.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _repository.Verify(x => x.RemoveAsync(It.IsAny<ValueTypeDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnusedCustomType_Succeeds()
    {
        var definition = CreateCustom();
        GivenDefinition(definition);
        _usageRepository
            .Setup(x => x.IsKeyInUseAsync(definition.Key.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result result = await _handler.Handle(new DeleteValueTypeDefinitionCommand(definition.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(x => x.RemoveAsync(definition, It.IsAny<CancellationToken>()), Times.Once);
    }
}