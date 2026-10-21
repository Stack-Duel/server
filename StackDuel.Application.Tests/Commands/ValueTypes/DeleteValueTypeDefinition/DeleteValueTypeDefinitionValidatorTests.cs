using DeleteValueTypeDefinitionCommand = StackDuel.Application.Commands.ValueTypes.DeleteValueTypeDefinition.DeleteValueTypeDefinitionCommand;
using DeleteValueTypeDefinitionValidator = StackDuel.Application.Commands.ValueTypes.DeleteValueTypeDefinition.DeleteValueTypeDefinitionValidator;

namespace StackDuel.Application.Tests.Commands.ValueTypes.DeleteValueTypeDefinition;

public class DeleteValueTypeDefinitionValidatorTests
{
    private DeleteValueTypeDefinitionValidator _validator = null!;

    public DeleteValueTypeDefinitionValidatorTests()
    {
        _validator = new DeleteValueTypeDefinitionValidator();
    }

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        Assert.True(_validator.Validate(new DeleteValueTypeDefinitionCommand(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_IsInvalid()
    {
        Assert.False(_validator.Validate(new DeleteValueTypeDefinitionCommand(Guid.Empty)).IsValid);
    }
}