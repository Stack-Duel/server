using StackDuel.Application.ValueTypes.Dtos;
using UpdateValueTypeDefinitionCommand = StackDuel.Application.Commands.ValueTypes.UpdateValueTypeDefinition.UpdateValueTypeDefinitionCommand;
using UpdateValueTypeDefinitionValidator = StackDuel.Application.Commands.ValueTypes.UpdateValueTypeDefinition.UpdateValueTypeDefinitionValidator;

namespace StackDuel.Application.Tests.Commands.ValueTypes.UpdateValueTypeDefinition;

public class UpdateValueTypeDefinitionValidatorTests
{
    private UpdateValueTypeDefinitionValidator _validator = null!;

    public UpdateValueTypeDefinitionValidatorTests()
    {
        _validator = new UpdateValueTypeDefinitionValidator();
    }

    private static UpdateValueTypeDefinitionCommand ValidCommand() =>
        new(Guid.NewGuid(), "Positive Integer", "A whole number greater than zero.", null);

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_IsInvalid()
    {
        var command = ValidCommand() with { Id = Guid.Empty };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyName_IsInvalid()
    {
        var command = ValidCommand() with { Name = "" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NullSpec_IsValid()
    {
        var command = ValidCommand() with { Spec = null };
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NonNullSpec_IsValid()
    {
        var command = ValidCommand() with { Spec = new ValueTypeSpecDto("integer", null, null, null, null, null, null) };
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_DescriptionTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Description = new string('a', 501) };
        Assert.False(_validator.Validate(command).IsValid);
    }
}