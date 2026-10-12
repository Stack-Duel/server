using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Submissions.Enums;
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
}