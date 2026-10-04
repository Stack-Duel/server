using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Problem;
using StackDuel.Application;
using StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Services.Problems;
using StackDuel.Application.Services.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class ProblemController(
    IProblemService problemService,
    ISubmissionService submissionService,
    UserContext userContext
) : ControllerBase
{
    [HttpGet("{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProblemWithSetupsDto>> GetProblemBySlug(
        string slug,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await problemService.GetProblemWithSetupsBySlug(slug, cancellationToken));
    }

    [HttpGet("by-id/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProblemWithSetupsDto>> GetProblemById(Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await problemService.GetProblemWithSetupsById(id, cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResult<ProblemDto>>> GetProblems(
        [FromQuery] GetProblemsPageableRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemService.GetProblemsPageableAsync(
                new PaginationRequest
                {
                    Page = query.Page,
                    Size = query.Size,
                    Timestamp = query.Timestamp,
                },
                query.Search,
                cancellationToken
            )
        );
    }

    [HttpGet("{slug}/setup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProblemSetupDto>> GetProblemSetup(
        string slug,
        [FromQuery] Guid languageVersionId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemService.GetProblemSetupAsync(slug, languageVersionId, cancellationToken)
        );
    }

    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PageResult<AdminProblemListItemDto>>> GetAdminProblems(
        [FromQuery] GetAdminProblemsPageableRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemService.GetAdminProblemsPageableAsync(
                new PaginationRequest
                {
                    Page = query.Page,
                    Size = query.Size,
                    Timestamp = query.Timestamp,
                },
                query.Search,
                cancellationToken
            )
        );
    }

    [HttpGet("admin/{problemId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProblemDetailDto>> GetAdminProblemDetail(
        Guid problemId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await problemService.GetAdminProblemDetailAsync(problemId, cancellationToken));
    }

    [HttpPut("admin/{problemId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.UpdateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateProblem(
        Guid problemId,
        [FromBody] UpdateProblemRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemService.UpdateProblemAsync(
                problemId,
                request.Title,
                request.Question,
                request.Difficulty,
                request.TimeLimitMs,
                request.MemoryLimitMb,
                request.Tags,
                request.Status,
                cancellationToken
            )
        );
    }

    [HttpPost("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.CreateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> CreateProblemDraft(
        [FromBody] CreateProblemDraftRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemService.CreateProblemDraftAsync(
                new CreateProblemDraftInput(
                    request.Title,
                    request.Question,
                    request.Difficulty,
                    request.TimeLimitMs,
                    request.MemoryLimitMb,
                    request.Tags,
                    request.TrackId
                ),
                cancellationToken
            )
        );
    }

    [HttpPost("admin/{problemId:guid}/setups")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.CreateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> AddSetup(
        Guid problemId,
        [FromBody] AddProblemSetupRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemService.AddProblemSetupAsync(problemId, request.LanguageVersionId, cancellationToken)
        );
    }

    [HttpPut("admin/{problemId:guid}/setups/reference-solution")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.CreateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> UpsertSetupReferenceSolution(
        Guid problemId,
        [FromBody] UpsertProblemSetupReferenceSolutionRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemService.UpsertProblemSetupReferenceSolutionAsync(
                problemId,
                request.LanguageVersionId,
                request.InitialCode,
                request.FunctionName,
                request.ReferenceSolutionCode,
                cancellationToken
            )
        );
    }

    [HttpPut("admin/{problemId:guid}/generation-parameters")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.CreateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetGenerationParameters(
        Guid problemId,
        [FromBody] SetProblemGenerationParametersRequest request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<GenerationParameterSpec> parameters =
        [
            .. request.Parameters.Select(p => new GenerationParameterSpec(
                p.Name,
                p.ValueType,
                p.Min,
                p.Max,
                p.LengthMin,
                p.LengthMax,
                p.Charset
            )),
        ];

        return this.ToActionResult(
            await problemService.SetProblemGenerationParametersAsync(
                problemId,
                parameters,
                request.OutputValueType,
                request.TargetCaseCount,
                request.Seed,
                cancellationToken
            )
        );
    }

    [HttpPut("admin/{problemId:guid}/sample-test-cases")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.CreateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetSampleTestCases(
        Guid problemId,
        [FromBody] SetProblemSampleTestCasesRequest request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<SampleTestCaseDto> testCases =
        [
            .. request.TestCases.Select(tc => new SampleTestCaseDto(
                tc.Name,
                [.. tc.Inputs.Select(i => new SampleTestCaseInputDto(i.Value, i.ValueType))],
                tc.ExpectedOutputValue,
                tc.ExpectedOutputValueType
            )),
        ];

        return this.ToActionResult(
            await problemService.SetProblemSampleTestCasesAsync(problemId, testCases, cancellationToken)
        );
    }

    [HttpPost("admin/{problemId:guid}/submit-for-validation")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.SubmitAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SubmitForValidation(Guid problemId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await problemService.SubmitProblemForValidationAsync(problemId, cancellationToken));
    }

    [HttpGet("{problemId:guid}/reaction")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProblemReactionSummaryDto>> GetProblemReaction(
        Guid problemId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemService.GetProblemReactionSummaryAsync(problemId, userContext.User?.Id, cancellationToken)
        );
    }

    [HttpPost("{problemId:guid}/reaction")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProblemReactionSummaryDto>> SetProblemReaction(
        Guid problemId,
        [FromBody] SetProblemReactionRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(
            await problemService.SetProblemReactionAsync(
                problemId,
                userContext.User.Id,
                request.ReactionTypeKey,
                cancellationToken
            )
        );
    }

    [HttpGet("{slug}/submissions")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ViewSubmission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageResult<ProblemSubmissionDto>>> GetSubmissions(
        string slug,
        [FromQuery] GetProblemSubmissionsRequest query,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(
            await submissionService.GetSubmissionsByProblemSlugAsync(
                slug,
                new PaginationRequest
                {
                    Page = query.Page,
                    Size = query.Size,
                    Timestamp = query.Timestamp,
                },
                userContext.User.Id,
                query.Type,
                query.SortBy,
                cancellationToken
            )
        );
    }
}