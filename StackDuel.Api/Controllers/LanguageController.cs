using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.RateLimiting;
using StackDuel.Application.Languages.Dtos;
using StackDuel.Application.Services.Languages;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class LanguageController(ILanguageService languageService) : ControllerBase
{
    [HttpGet]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<LanguageDto>>> GetLanguages(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await languageService.GetAllAsync(cancellationToken));
    }
}