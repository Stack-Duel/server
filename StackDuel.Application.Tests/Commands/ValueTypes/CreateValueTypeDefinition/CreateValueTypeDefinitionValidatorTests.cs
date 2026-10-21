using StackDuel.Application.ValueTypes.Dtos;
using CreateValueTypeDefinitionCommand = StackDuel.Application.Commands.ValueTypes.CreateValueTypeDefinition.CreateValueTypeDefinitionCommand;
using CreateValueTypeDefinitionValidator = StackDuel.Application.Commands.ValueTypes.CreateValueTypeDefinition.CreateValueTypeDefinitionValidator;

namespace StackDuel.Application.Tests.Commands.ValueTypes.CreateValueTypeDefinition;

public class CreateValueTypeDefinitionValidatorTests
{
    private CreateValueTypeDefinitionValidator _validator = null!;

    public CreateValueTypeDefinitionValidatorTests()
    {
        _validator = new CreateValueTypeDefinitionValidator();
    }

    private static ValueTypeSpecDto ValidSpec() => new("integer", null, null, null, null, null, null);

    private static CreateValueTypeDefinitionCommand ValidCommand() =>
        new("positive_integer", "Positive Integer", "A whole number greater than zero.", ValidSpec());

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Validate_EmptyKey_IsInvalid()
    {
        var command = ValidCommand() with { Key = "" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_KeyTooShort_IsInvalid()
    {
        var command = ValidCommand() with { Key = "a" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_KeyTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Key = new string('a', 51) };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyName_IsInvalid()
    {
        var command = ValidCommand() with { Name = "" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NullDescription_IsValid()
    {
        var command = ValidCommand() with { Description = null };
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_DescriptionTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Description = new string('a', 501) };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NullSpec_IsInvalid()
    {
        var command = ValidCommand() with { Spec = null! };
        Assert.False(_validator.Validate(command).IsValid);
    }
}