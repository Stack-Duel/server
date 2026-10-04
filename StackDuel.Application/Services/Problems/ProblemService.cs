using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.Problems.AddProblemSetup;
using StackDuel.Application.Commands.Problems.CreateProblemDraft;
using StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;
using StackDuel.Application.Commands.Problems.SetProblemReaction;
using StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;
using StackDuel.Application.Commands.Problems.SubmitProblemForValidation;
using StackDuel.Application.Commands.Problems.UpdateProblem;
using StackDuel.Application.Commands.Problems.UpsertProblemSetupReferenceSolution;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Queries.Problems.GetAdminProblemDetail;
using StackDuel.Application.Queries.Problems.GetAdminProblemsPageable;
using StackDuel.Application.Queries.Problems.GetProblemById;
using StackDuel.Application.Queries.Problems.GetProblemBySlug;
using StackDuel.Application.Queries.Problems.GetProblemReactionSummary;
using StackDuel.Application.Queries.Problems.GetProblemSetup;
using StackDuel.Application.Queries.Problems.GetProblemsPageable;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application.Services.Problems;

public interface IProblemService
{
    Task<Result<ProblemSetupDto>> GetProblemSetupAsync(
        string slug,
        Guid languageVersionId,
        CancellationToken cancellationToken
    );

    Task<Result<ProblemReactionSummaryDto>> GetProblemReactionSummaryAsync(
        Guid problemId,
        Guid? userId,
        CancellationToken cancellationToken
    );

    Task<Result<ProblemReactionSummaryDto>> SetProblemReactionAsync(
        Guid problemId,
        Guid userId,
        string reactionTypeKey,
        CancellationToken cancellationToken
    );

    Task<Result<PageResult<ProblemDto>>> GetProblemsPageableAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken
    );

    Task<Result<ProblemWithSetupsDto>> GetProblemWithSetupsBySlug(string slug, CancellationToken cancellationToken);

    Task<Result<ProblemWithSetupsDto>> GetProblemWithSetupsById(Guid id, CancellationToken cancellationToken);

    Task<Result<PageResult<AdminProblemListItemDto>>> GetAdminProblemsPageableAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken
    );

    Task<Result<AdminProblemDetailDto>> GetAdminProblemDetailAsync(Guid problemId, CancellationToken cancellationToken);

    Task<Result> UpdateProblemAsync(
        Guid problemId,
        string title,
        string question,
        int difficulty,
        int timeLimitMs,
        int memoryLimitMb,
        IReadOnlyCollection<string> tags,
        ProblemStatus? status,
        CancellationToken cancellationToken
    );

    Task<Result<Guid>> CreateProblemDraftAsync(CreateProblemDraftInput input, CancellationToken cancellationToken);

    Task<Result<Guid>> AddProblemSetupAsync(
        Guid problemId,
        Guid languageVersionId,
        CancellationToken cancellationToken
    );

    Task<Result<Guid>> UpsertProblemSetupReferenceSolutionAsync(
        Guid problemId,
        Guid languageVersionId,
        string initialCode,
        string? functionName,
        string referenceSolutionCode,
        CancellationToken cancellationToken
    );

    Task<Result> SetProblemGenerationParametersAsync(
        Guid problemId,
        IReadOnlyList<GenerationParameterSpec> parameters,
        string outputValueType,
        int targetCaseCount,
        int seed,
        CancellationToken cancellationToken
    );

    Task<Result> SetProblemSampleTestCasesAsync(
        Guid problemId,
        IReadOnlyList<SampleTestCaseDto> testCases,
        CancellationToken cancellationToken
    );

    Task<Result> SubmitProblemForValidationAsync(Guid problemId, CancellationToken cancellationToken);
}

internal sealed class ProblemService(IMediator mediator) : IProblemService
{
    public async Task<Result<ProblemSetupDto>> GetProblemSetupAsync(
        string slug,
        Guid languageVersionId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetProblemSetupQuery(slug, languageVersionId), cancellationToken);

        return result;
    }

    public async Task<Result<PageResult<ProblemDto>>> GetProblemsPageableAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetProblemsPageableQuery(paginationRequest, search), cancellationToken);

        return result;
    }

    public async Task<Result<ProblemWithSetupsDto>> GetProblemWithSetupsBySlug(
        string slug,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetProblemBySlugQuery(slug), cancellationToken);

        return result;
    }

    public async Task<Result<ProblemWithSetupsDto>> GetProblemWithSetupsById(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetProblemByIdQuery(id), cancellationToken);

        return result;
    }

    public async Task<Result<ProblemReactionSummaryDto>> GetProblemReactionSummaryAsync(
        Guid problemId,
        Guid? userId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetProblemReactionSummaryQuery(problemId, userId), cancellationToken);

        return result;
    }

    public async Task<Result<ProblemReactionSummaryDto>> SetProblemReactionAsync(
        Guid problemId,
        Guid userId,
        string reactionTypeKey,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new SetProblemReactionCommand(problemId, userId, reactionTypeKey),
            cancellationToken
        );

        return result;
    }

    public async Task<Result<PageResult<AdminProblemListItemDto>>> GetAdminProblemsPageableAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new GetAdminProblemsPageableQuery(paginationRequest, search),
            cancellationToken
        );

        return result;
    }

    public async Task<Result<AdminProblemDetailDto>> GetAdminProblemDetailAsync(
        Guid problemId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetAdminProblemDetailQuery(problemId), cancellationToken);

        return result;
    }

    public async Task<Result> UpdateProblemAsync(
        Guid problemId,
        string title,
        string question,
        int difficulty,
        int timeLimitMs,
        int memoryLimitMb,
        IReadOnlyCollection<string> tags,
        ProblemStatus? status,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new UpdateProblemCommand(problemId, title, question, difficulty, timeLimitMs, memoryLimitMb, tags, status),
            cancellationToken
        );

        return result;
    }

    public async Task<Result<Guid>> CreateProblemDraftAsync(
        CreateProblemDraftInput input,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new CreateProblemDraftCommand(
                input.Title,
                input.Question,
                input.Difficulty,
                input.TimeLimitMs,
                input.MemoryLimitMb,
                input.Tags,
                input.TrackId
            ),
            cancellationToken
        );

        return result;
    }

    public async Task<Result<Guid>> AddProblemSetupAsync(
        Guid problemId,
        Guid languageVersionId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new AddProblemSetupCommand(problemId, languageVersionId), cancellationToken);

        return result;
    }

    public async Task<Result<Guid>> UpsertProblemSetupReferenceSolutionAsync(
        Guid problemId,
        Guid languageVersionId,
        string initialCode,
        string? functionName,
        string referenceSolutionCode,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new UpsertProblemSetupReferenceSolutionCommand(
                problemId,
                languageVersionId,
                initialCode,
                functionName,
                referenceSolutionCode
            ),
            cancellationToken
        );

        return result;
    }

    public async Task<Result> SetProblemGenerationParametersAsync(
        Guid problemId,
        IReadOnlyList<GenerationParameterSpec> parameters,
        string outputValueType,
        int targetCaseCount,
        int seed,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new SetProblemGenerationParametersCommand(problemId, parameters, outputValueType, targetCaseCount, seed),
            cancellationToken
        );

        return result;
    }

    public async Task<Result> SetProblemSampleTestCasesAsync(
        Guid problemId,
        IReadOnlyList<SampleTestCaseDto> testCases,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new SetProblemSampleTestCasesCommand(problemId, testCases), cancellationToken);

        return result;
    }

    public async Task<Result> SubmitProblemForValidationAsync(Guid problemId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitProblemForValidationCommand(problemId), cancellationToken);

        return result;
    }
}