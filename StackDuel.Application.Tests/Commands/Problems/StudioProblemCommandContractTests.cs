using Ardalis.Result;
using StackDuel.Application.Audit;
using StackDuel.Application.Authorization;
using StackDuel.Application.Commands.Problems.AddProblemSetup;
using StackDuel.Application.Commands.Problems.CancelProblemValidation;
using StackDuel.Application.Commands.Problems.DeleteProblem;
using StackDuel.Application.Commands.Problems.RemoveProblemSetup;
using StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;
using StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;
using StackDuel.Application.Commands.Problems.SubmitProblemForValidation;
using StackDuel.Application.Commands.Problems.UpsertProblemSetupReferenceSolution;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application.Tests.Commands.Problems;

public class StudioProblemCommandContractTests
{
    private static readonly Guid ProblemId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public static TheoryData<IAuditableCommand, string> AuditableCommands =>
        new()
        {
            { new DeleteProblemCommand(ProblemId, "Two Sum"), "problem.deleted" },
            { new CancelProblemValidationCommand(ProblemId), "problem.validation-cancelled" },
            { new SubmitProblemForValidationCommand(ProblemId), "problem.submitted-for-validation" },
            { new AddProblemSetupCommand(ProblemId, Guid.NewGuid()), "problem.setup-added" },
            { new SetProblemSampleTestCasesCommand(ProblemId, []), "problem.sample-test-cases-set" },
            { new RemoveProblemSetupCommand(ProblemId, Guid.NewGuid()), "problem.setup-removed" },
        };

    [Theory]
    [MemberData(nameof(AuditableCommands))]
    public void EveryStudioCommand_AuditsAgainstTheProblemItTargets(IAuditableCommand command, string auditAction)
    {
        Assert.Equal(auditAction, command.AuditAction);
        Assert.Equal("problem", command.AuditTargetType);
        Assert.Equal(ProblemId.ToString(), command.AuditTargetId);
    }

    [Fact]
    public void DeletingAProblem_RecordsNoExtraAuditDetail()
    {
        Assert.Null(new DeleteProblemCommand(ProblemId, "Two Sum").AuditDetails);
    }

    [Fact]
    public void CancellingValidation_RecordsNoExtraAuditDetail()
    {
        Assert.Null(new CancelProblemValidationCommand(ProblemId).AuditDetails);
    }

    [Fact]
    public void AddingASetup_AuditsTheLanguageVersion()
    {
        Guid languageVersionId = Guid.NewGuid();

        object? details = new AddProblemSetupCommand(ProblemId, languageVersionId).AuditDetails;

        Assert.Contains(languageVersionId.ToString(), details!.ToString());
    }

    [Fact]
    public void RemovingASetup_AuditsTheLanguageVersion()
    {
        Guid languageVersionId = Guid.NewGuid();

        RemoveProblemSetupCommand command = new(ProblemId, languageVersionId, UserId);

        Assert.Contains(languageVersionId.ToString(), command.AuditDetails!.ToString());
        Assert.NotEmpty(command.Requirements);
    }

    [Fact]
    public void UpsertingAReferenceSolution_AuditsTheLanguageVersionAndFunctionName()
    {
        Guid languageVersionId = Guid.NewGuid();

        object? details = new UpsertProblemSetupReferenceSolutionCommand(
            ProblemId,
            languageVersionId,
            "starter",
            "solve",
            "solution"
        ).AuditDetails;

        Assert.Contains(languageVersionId.ToString(), details!.ToString());
        Assert.Contains("solve", details.ToString());
    }

    [Fact]
    public void SettingSampleTestCases_AuditsHowManyWereSupplied()
    {
        object? details = new SetProblemSampleTestCasesCommand(
            ProblemId,
            [
                new SampleTestCaseDto("one", [], "1", "integer"),
                new SampleTestCaseDto("two", [], "2", "integer"),
            ]
        ).AuditDetails;

        Assert.Contains("2", details!.ToString());
    }

    [Fact]
    public void SettingGenerationParameters_CarriesTheSeedAndTargetCount()
    {
        SetProblemGenerationParametersCommand command = new(
            ProblemId,
            [new GenerationParameterSpec("n", "integer", 1, 10, null, null, null)],
            "integer",
            TargetCaseCount: 20,
            Seed: 99
        );

        Assert.Equal(20, command.TargetCaseCount);
        Assert.Equal(99, command.Seed);
        Assert.Equal("problem.generation-parameters-set", command.AuditAction);
    }

    [Fact]
    public void AUserScopedCommand_RequiresOwnershipOfTheProblem()
    {
        var command = new DeleteProblemCommand(ProblemId, "Two Sum", RequestedByUserId: UserId);

        IAuthorizationRequirement requirement = Assert.Single(command.Requirements);

        OwnershipRequirement<Problem> ownership = Assert.IsType<OwnershipRequirement<Problem>>(requirement);
        Assert.Equal(ProblemId, ownership.ResourceId);
        Assert.Equal(UserId, ownership.UserId);
    }

    [Fact]
    public void AnAdminCommandWithNoRequestingUser_RequiresNoOwnership()
    {
        Assert.Empty(new DeleteProblemCommand(ProblemId, "Two Sum").Requirements);
        Assert.Empty(new CancelProblemValidationCommand(ProblemId).Requirements);
        Assert.Empty(new SubmitProblemForValidationCommand(ProblemId).Requirements);
        Assert.Empty(new AddProblemSetupCommand(ProblemId, Guid.NewGuid()).Requirements);
        Assert.Empty(new SetProblemSampleTestCasesCommand(ProblemId, []).Requirements);
    }

    [Fact]
    public void EveryUserScopedCommand_RequiresOwnership()
    {
        Assert.Single(new CancelProblemValidationCommand(ProblemId, UserId).Requirements);
        Assert.Single(new SubmitProblemForValidationCommand(ProblemId, UserId).Requirements);
        Assert.Single(new AddProblemSetupCommand(ProblemId, Guid.NewGuid(), UserId).Requirements);
        Assert.Single(new SetProblemSampleTestCasesCommand(ProblemId, [], UserId).Requirements);
    }

    [Fact]
    public void AnUnauthorizedStudioCommand_LooksLikeAMissingProblemRatherThanAForbiddenOne()
    {
        IAuthorizationGuarded<Result> command = new SetProblemSampleTestCasesCommand(ProblemId, []);

        Assert.Equal(ResultStatus.NotFound, command.Unauthorized.Status);
    }
}