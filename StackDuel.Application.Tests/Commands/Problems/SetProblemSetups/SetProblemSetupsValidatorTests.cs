using StackDuel.Application.Commands.Problems.SetProblemSetups;
using SetProblemSetupsCommand = StackDuel.Application.Commands.Problems.SetProblemSetups.SetProblemSetupsCommand;
using SetProblemSetupsValidator = StackDuel.Application.Commands.Problems.SetProblemSetups.SetProblemSetupsValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SetProblemSetups;

public class SetProblemSetupsValidatorTests
{
    private SetProblemSetupsValidator _validator = null!;

    public SetProblemSetupsValidatorTests()
    {
        _validator = new SetProblemSetupsValidator();
    }

    private static ProblemSetupInputDto ValidSetup(Guid? languageVersionId = null) =>
        new(
            languageVersionId ?? Guid.NewGuid(),
            null,
            null,
            "function solve() {}",
            "solve",
            "function solve() { return 1; }"
        );

    private static ProblemSetupInputDto NamedSetup(string language, string? version = null) =>
        new(null, language, version, "function solve() {}", "solve", "function solve() { return 1; }");

    private static SetProblemSetupsCommand ValidCommand(params ProblemSetupInputDto[] setups) =>
        new(Guid.NewGuid(), setups.Length > 0 ? setups : [ValidSetup()]);

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Validate_EmptyProblemId_IsInvalid()
    {
        var command = ValidCommand() with { ProblemId = Guid.Empty };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NoSetups_IsInvalid()
    {
        var command = ValidCommand() with { Setups = [] };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_LanguageNameWithoutVersion_IsValid()
    {
        Assert.True(_validator.Validate(ValidCommand(NamedSetup("python"))).IsValid);
    }

    [Fact]
    public void Validate_LanguageNameWithVersion_IsValid()
    {
        Assert.True(_validator.Validate(ValidCommand(NamedSetup("python", "3.13.2"))).IsValid);
    }

    [Fact]
    public void Validate_NeitherLanguageVersionIdNorLanguage_IsInvalid()
    {
        var command = ValidCommand(ValidSetup() with { LanguageVersionId = null });

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyLanguageVersionIdAndBlankLanguage_IsInvalid()
    {
        var command = ValidCommand(ValidSetup() with { LanguageVersionId = Guid.Empty, Language = "   " });

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_LanguageTooLong_IsInvalid()
    {
        var command = ValidCommand(NamedSetup(new string('a', 101)));

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_LanguageVersionTooLong_IsInvalid()
    {
        var command = ValidCommand(NamedSetup("python", new string('a', 51)));

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_BlankInitialCode_IsInvalid()
    {
        var command = ValidCommand(ValidSetup() with { InitialCode = "" });

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_BlankReferenceSolution_IsInvalid()
    {
        var command = ValidCommand(ValidSetup() with { ReferenceSolutionCode = "" });

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NullFunctionName_IsValid()
    {
        var command = ValidCommand(ValidSetup() with { FunctionName = null });

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_FunctionNameTooLong_IsInvalid()
    {
        var command = ValidCommand(ValidSetup() with { FunctionName = new string('a', 201) });

        Assert.False(_validator.Validate(command).IsValid);
    }
}