using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Problem;
using StackDuel.Application;
using StackDuel.Application.Commands.Problems.DeleteProblem;
using StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/studio/problems")]
[EnableRateLimiting(WellKnownPolicies.General)]
[RequireUser]
[RequirePermission(WellKnownPermissions.ReadStudioProblems)]
public sealed class StudioProblemController(IStudioProblemService studioProblemService, UserContext userContext)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudioProblemDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(
            await studioProblemService.GetStudioProblemByIdAsync(id, userContext.User!.Id, cancellationToken)
        );
    }

    [HttpGet("initial-values")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InitialStudioProblemSetupDto>> GetInitialStudioProblemAsync(
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await studioProblemService.GetInitialStudioProblemSetupAsync(cancellationToken));
    }

    [HttpGet("{id:guid}/setups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<StudioProblemDetailsSetupDto>>> GetSetupsAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await studioProblemService.GetStudioProblemDetailSetupsAsync(id, userContext.User!.Id, cancellationToken)
        );
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateMetadataAsync(
        Guid id,
        [FromBody] UpdateStudioProblemMetadataRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await studioProblemService.UpdateStudioProblemMetadataAsync(
                new UpdateStudioProblemMetadataInput(
                    id,
                    request.Title,
                    request.Question,
                    request.Difficulty,
                    request.TimeLimitMs,
                    request.MemoryLimitMb,
                    request.Tags
                ),
                userContext.User!.Id,
                cancellationToken
            )
        );
    }

    [HttpPost("{id:guid}/setups")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> AddSetupAsync(
        Guid id,
        [FromBody] AddProblemSetupRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await studioProblemService.AddStudioProblemSetupAsync(
                id,
                request.LanguageVersionId,
                userContext.User!.Id,
                cancellationToken
            )
        );
    }

    [HttpDelete("{id:guid}/setups/{languageVersionId:guid}")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveSetupAsync(
        Guid id,
        Guid languageVersionId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await studioProblemService.RemoveStudioProblemSetupAsync(
                id,
                languageVersionId,
                userContext.User!.Id,
                cancellationToken
            )
        );
    }

    [HttpPut("{id:guid}/setups/reference-solution")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> UpsertSetupSolutionAsync(
        Guid id,
        [FromBody] UpsertStudioProblemSetupSolutionRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await studioProblemService.UpsertStudioProblemSetupSolutionAsync(
                new UpsertStudioProblemSetupSolutionInput(
                    id,
                    request.LanguageVersionId,
                    request.StarterCode,
                    request.FunctionName,
                    request.ReferenceSolution
                ),
                userContext.User!.Id,
                cancellationToken
            )
        );
    }

    [HttpPut("{id:guid}/generation-parameters")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetGenerationParametersAsync(
        Guid id,
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
            await studioProblemService.SetStudioProblemGenerationParametersAsync(
                new SetStudioProblemGenerationParametersInput(
                    id,
                    parameters,
                    request.OutputValueType,
                    request.TargetCaseCount,
                    request.Seed
                ),
                userContext.User!.Id,
                cancellationToken
            )
        );
    }

    [HttpPut("{id:guid}/sample-test-cases")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetSampleTestCasesAsync(
        Guid id,
        [FromBody] SetProblemSampleTestCasesRequest request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<SampleTestCaseDto> testCases =
        [
            .. request.TestCases.Select(testCase => new SampleTestCaseDto(
                testCase.Name,
                [.. testCase.Inputs.Select(input => new SampleTestCaseInputDto(input.Value, input.ValueType))],
                testCase.ExpectedOutputValue,
                testCase.ExpectedOutputValueType
            )),
        ];

        return this.ToActionResult(
            await studioProblemService.SetStudioProblemSampleTestCasesAsync(
                new SetStudioProblemSampleTestCasesInput(id, testCases),
                userContext.User!.Id,
                cancellationToken
            )
        );
    }

    [HttpPost("{id:guid}/submit-for-validation")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SubmitForValidationAsync(Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(
            await studioProblemService.SubmitStudioProblemForValidationAsync(
                id,
                userContext.User!.Id,
                cancellationToken
            )
        );
    }

    [HttpPost("{id:guid}/cancel-validation")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelValidationAsync(Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(
            await studioProblemService.CancelStudioProblemValidationAsync(
                id,
                userContext.User!.Id,
                cancellationToken
            )
        );
    }

    [HttpPost]
    [RequirePermission(WellKnownPermissions.CreateAdminProblems)]
    [EnableRateLimiting(WellKnownPolicies.ProblemCreation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> CreateAsync(
        [FromBody] CreateDraftProblemRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await studioProblemService.CreateStudioProblemAsync(
                new CreateDraftProblemInput(
                    request.Title,
                    request.Question,
                    request.Tags,
                    request.Difficulty,
                    request.TimeLimitMs,
                    request.MemoryLimitMb,
                    request.TrackId
                ),
                cancellationToken
            )
        );
    }

    [HttpGet("{id:guid}/deletion-impact")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProblemDeletionImpactDto>> GetStudioProblemDeletionImpactAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await studioProblemService.GetStudioProblemDeletionImpactAsync(
                id,
                userContext.User!.Id,
                cancellationToken
            )
        );
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(WellKnownPermissions.UpdateStudioProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStudioProblemAsync(
        Guid id,
        [FromBody] DeleteStudioProblemRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await studioProblemService.DeleteStudioProblemAsync(
                id,
                request.ConfirmationTitle,
                userContext.User!.Id,
                cancellationToken
            )
        );
    }
}