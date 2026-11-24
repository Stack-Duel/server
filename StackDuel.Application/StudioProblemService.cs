using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.Problems.AddProblemSetup;
using StackDuel.Application.Commands.Problems.CancelProblemValidation;
using StackDuel.Application.Commands.Problems.CreateDraftProblem;
using StackDuel.Application.Commands.Problems.DeleteProblem;
using StackDuel.Application.Commands.Problems.RemoveProblemSetup;
using StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;
using StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;
using StackDuel.Application.Commands.Problems.SubmitProblemForValidation;
using StackDuel.Application.Commands.Problems.UpdateProblem;
using StackDuel.Application.Commands.Problems.UpsertProblemSetupReferenceSolution;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Queries.StudioProblems.GetInitialStudioProblemSetups;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemDeletionImpact;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemSetups;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application;

public sealed record UpdateStudioProblemMetadataInput(
    Guid ProblemId,
    string Title,
    string Question,
    int Difficulty,
    int TimeLimitMs,
    int MemoryLimitMb,
    IReadOnlyCollection<string> Tags
);

public sealed record UpsertStudioProblemSetupSolutionInput(
    Guid ProblemId,
    Guid LanguageVersionId,
    string StarterCode,
    string? FunctionName,
    string ReferenceSolution
);

public sealed record SetStudioProblemSampleTestCasesInput(
    Guid ProblemId,
    IReadOnlyList<SampleTestCaseDto> TestCases
);

public sealed record SetStudioProblemGenerationParametersInput(
    Guid ProblemId,
    IReadOnlyList<GenerationParameterSpec> Parameters,
    string OutputValueType,
    int TargetCaseCount,
    int Seed
);

public interface IStudioProblemService
{
    Task<Result<InitialStudioProblemSetupDto>> GetInitialStudioProblemSetupAsync(CancellationToken cancellationToken);

    Task<Result<StudioProblemDetailsDto>> GetStudioProblemByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result<IEnumerable<StudioProblemDetailsSetupDto>>> GetStudioProblemDetailSetupsAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result<Guid>> CreateStudioProblemAsync(CreateDraftProblemInput input, CancellationToken cancellationToken);

    Task<Result> UpdateStudioProblemMetadataAsync(
        UpdateStudioProblemMetadataInput input,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result<Guid>> AddStudioProblemSetupAsync(
        Guid problemId,
        Guid languageVersionId,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result> RemoveStudioProblemSetupAsync(
        Guid problemId,
        Guid languageVersionId,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result<Guid>> UpsertStudioProblemSetupSolutionAsync(
        UpsertStudioProblemSetupSolutionInput input,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result> SetStudioProblemGenerationParametersAsync(
        SetStudioProblemGenerationParametersInput input,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result> SetStudioProblemSampleTestCasesAsync(
        SetStudioProblemSampleTestCasesInput input,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result> SubmitStudioProblemForValidationAsync(
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result> CancelStudioProblemValidationAsync(
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result<ProblemDeletionImpactDto>> GetStudioProblemDeletionImpactAsync(
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result> DeleteStudioProblemAsync(
        Guid problemId,
        string confirmationTitle,
        Guid userId,
        CancellationToken cancellationToken
    );
}

internal sealed class StudioProblemService(IMediator mediator) : IStudioProblemService
{
    public async Task<Result<InitialStudioProblemSetupDto>> GetInitialStudioProblemSetupAsync(
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetInitialStudioProblemSetupsQuery(), cancellationToken);

    public async Task<Result<StudioProblemDetailsDto>> GetStudioProblemByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetStudioProblemByIdQuery(id, userId), cancellationToken);

    public async Task<Result<IEnumerable<StudioProblemDetailsSetupDto>>> GetStudioProblemDetailSetupsAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetStudioProblemSetupsQuery(id, userId), cancellationToken);

    public async Task<Result<Guid>> CreateStudioProblemAsync(
        CreateDraftProblemInput input,
        CancellationToken cancellationToken
    ) => await mediator.Send(new CreateDraftProblemCommand(input), cancellationToken);

    public async Task<Result> UpdateStudioProblemMetadataAsync(
        UpdateStudioProblemMetadataInput input,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new UpdateProblemCommand(
                input.ProblemId,
                input.Title,
                input.Question,
                input.Difficulty,
                input.TimeLimitMs,
                input.MemoryLimitMb,
                input.Tags,
                Status: null,
                RequestedByUserId: userId
            ),
            cancellationToken
        );

    public async Task<Result<Guid>> AddStudioProblemSetupAsync(
        Guid problemId,
        Guid languageVersionId,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new AddProblemSetupCommand(problemId, languageVersionId, RequestedByUserId: userId),
            cancellationToken
        );

    public async Task<Result> RemoveStudioProblemSetupAsync(
        Guid problemId,
        Guid languageVersionId,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new RemoveProblemSetupCommand(problemId, languageVersionId, RequestedByUserId: userId),
            cancellationToken
        );

    public async Task<Result<Guid>> UpsertStudioProblemSetupSolutionAsync(
        UpsertStudioProblemSetupSolutionInput input,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new UpsertProblemSetupReferenceSolutionCommand(
                input.ProblemId,
                input.LanguageVersionId,
                input.StarterCode,
                input.FunctionName,
                input.ReferenceSolution,
                RequestedByUserId: userId
            ),
            cancellationToken
        );

    public async Task<Result> SetStudioProblemGenerationParametersAsync(
        SetStudioProblemGenerationParametersInput input,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new SetProblemGenerationParametersCommand(
                input.ProblemId,
                input.Parameters,
                input.OutputValueType,
                input.TargetCaseCount,
                input.Seed,
                RequestedByUserId: userId
            ),
            cancellationToken
        );

    public async Task<Result> SetStudioProblemSampleTestCasesAsync(
        SetStudioProblemSampleTestCasesInput input,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new SetProblemSampleTestCasesCommand(
                input.ProblemId,
                input.TestCases,
                RequestedByUserId: userId
            ),
            cancellationToken
        );

    public async Task<Result> SubmitStudioProblemForValidationAsync(
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new SubmitProblemForValidationCommand(problemId, RequestedByUserId: userId),
            cancellationToken
        );

    public async Task<Result> CancelStudioProblemValidationAsync(
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new CancelProblemValidationCommand(problemId, RequestedByUserId: userId),
            cancellationToken
        );

    public async Task<Result<ProblemDeletionImpactDto>> GetStudioProblemDeletionImpactAsync(
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new GetStudioProblemDeletionImpactQuery(problemId, userId),
            cancellationToken
        );

    public async Task<Result> DeleteStudioProblemAsync(
        Guid problemId,
        string confirmationTitle,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new DeleteProblemCommand(problemId, confirmationTitle, RequestedByUserId: userId),
            cancellationToken
        );
}