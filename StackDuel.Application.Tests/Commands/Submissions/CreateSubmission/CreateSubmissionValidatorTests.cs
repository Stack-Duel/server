using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;
using CreateSubmissionCommand = StackDuel.Application.Commands.Submissions.CreateSubmission.CreateSubmissionCommand;
using CreateSubmissionValidator = StackDuel.Application.Commands.Submissions.CreateSubmission.CreateSubmissionValidator;

namespace StackDuel.Application.Tests.Commands.Submissions.CreateSubmission;

public class CreateSubmissionValidatorTests
{
    private CreateSubmissionValidator _validator = null!;

    public CreateSubmissionValidatorTests()
    {
        _validator = new CreateSubmissionValidator();
    }

    private static CreateSubmissionCommand ValidCommand(
        SubmissionType type = SubmissionType.Run,
        IReadOnlyCollection<CreateSubmissionCustomTestCaseDto>? customTestCases = null
    ) => new(Guid.NewGuid(), type, "return 42;", Guid.NewGuid(), customTestCases);

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Validate_EmptyProblemSetupId_IsInvalid()
    {
        var command = ValidCommand() with { ProblemSetupId = Guid.Empty };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyCode_IsInvalid()
    {
        var command = ValidCommand() with { Code = "" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_CodeTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Code = new string('a', 65537) };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyCreatedById_IsInvalid()
    {
        var command = ValidCommand() with { CreatedById = Guid.Empty };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_RunType_CustomTestCaseWithNoInputs_IsInvalid()
    {
        var command = ValidCommand(customTestCases: [new CreateSubmissionCustomTestCaseDto([])]);
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_RunType_CustomTestCaseWithInputs_IsValid()
    {
        var command = ValidCommand(customTestCases: [new CreateSubmissionCustomTestCaseDto(["1"])]);
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_SubmitType_CustomTestCaseWithNoInputs_IsValid()
    {
        var command = ValidCommand(
            type: SubmissionType.Submit,
            customTestCases: [new CreateSubmissionCustomTestCaseDto([])]
        );
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NullCustomTestCases_IsValid()
    {
        var command = ValidCommand(customTestCases: null);
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_CodeLongerThanSourceCodeMaxLength_IsInvalid()
    {
        var command = ValidCommand() with { Code = new string('x', SourceCode.MaxLength + 1) };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("harness")]
    [InlineData("harness.js")]
    [InlineData("HARNESS.JS")]
    [InlineData("solution")]
    [InlineData("run")]
    [InlineData("compile")]
    public void Validate_AdditionalFileTargetingAReservedRuntimeName_IsInvalid(string path)
    {
        var command = WithFiles(new CreateSubmissionFileDto(path, "module.exports = {};"));
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("../harness")]
    [InlineData("../../run")]
    [InlineData("nested/helper")]
    [InlineData("nested\\helper")]
    [InlineData("helper.spec.js")]
    [InlineData(".hidden")]
    [InlineData("helper file")]
    public void Validate_AdditionalFileWithAnUnsafePath_IsInvalid(string path)
    {
        var command = WithFiles(new CreateSubmissionFileDto(path, "module.exports = {};"));
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("helper")]
    [InlineData("helper.js")]
    [InlineData("My-Helper_2.ts")]
    public void Validate_AdditionalFileWithAPlainName_IsValid(string path)
    {
        var command = WithFiles(new CreateSubmissionFileDto(path, "module.exports = {};"));
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_MoreAdditionalFilesThanAllowed_IsInvalid()
    {
        CreateSubmissionFileDto[] files =
        [
            .. Enumerable
                .Range(0, SubmissionSourceFile.MaxFilesPerSubmission + 1)
                .Select(index => new CreateSubmissionFileDto($"helper{index}", "x")),
        ];

        Assert.False(_validator.Validate(WithFiles(files)).IsValid);
    }

    [Fact]
    public void Validate_DuplicateAdditionalFilePaths_IsInvalid()
    {
        var command = WithFiles(
            new CreateSubmissionFileDto("helper", "x"),
            new CreateSubmissionFileDto("HELPER", "y")
        );

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_AdditionalFileContentOverTheLimit_IsInvalid()
    {
        var command = WithFiles(
            new CreateSubmissionFileDto("helper", new string('x', SubmissionSourceFile.MaxContentLength + 1))
        );

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_MoreCustomTestCasesThanAllowed_IsInvalid()
    {
        CreateSubmissionCustomTestCaseDto[] cases =
        [
            .. Enumerable
                .Range(0, CreateSubmissionValidator.MaxCustomTestCases + 1)
                .Select(_ => new CreateSubmissionCustomTestCaseDto(["1"])),
        ];

        Assert.False(_validator.Validate(ValidCommand(customTestCases: cases)).IsValid);
    }

    [Fact]
    public void Validate_CustomTestCaseWithMoreInputsThanAllowed_IsInvalid()
    {
        string[] inputs = [.. Enumerable.Range(0, CreateSubmissionValidator.MaxCustomTestCaseInputs + 1).Select(i => "1")];
        var command = ValidCommand(customTestCases: [new CreateSubmissionCustomTestCaseDto(inputs)]);

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_CustomTestCaseInputOverTheLengthLimit_IsInvalid()
    {
        var command = ValidCommand(
            customTestCases:
            [
                new CreateSubmissionCustomTestCaseDto(
                    [new string('1', CreateSubmissionValidator.MaxCustomTestCaseInputLength + 1)]
                ),
            ]
        );

        Assert.False(_validator.Validate(command).IsValid);
    }

    private static CreateSubmissionCommand WithFiles(params CreateSubmissionFileDto[] files) =>
        ValidCommand() with { AdditionalFiles = files };
}