using StackDuel.Api.Attributes;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.LanguageServer;
using StackDuel.Api.Responses.LanguageServer;
using StackDuel.Application;
using StackDuel.Application.LanguageServer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/language-servers")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class LanguageServerController(ILanguageServerSessionManager sessionManager, UserContext userContext)
    : ControllerBase
{
    [HttpPost("sessions")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<LanguageServerSessionResponse>> StartSession(
        [FromBody] StartLanguageServerSessionRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return Unauthorized();

        LanguageServerSessionStartResult result = await sessionManager.StartSessionAsync(
            userContext.User.Id,
            request.Language,
            cancellationToken
        );

        if (result.Error is not null)
        {
            return result.Error switch
            {
                LanguageServerSessionStartError.UnsupportedLanguage => BadRequest(
                    $"No language server is available for '{request.Language}'."
                ),
                LanguageServerSessionStartError.Disabled => StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    "Language server support is disabled."
                ),
                LanguageServerSessionStartError.AtCapacity => StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    "Language server capacity reached, try again shortly."
                ),
                _ => StatusCode(StatusCodes.Status503ServiceUnavailable),
            };
        }

        return Ok(LanguageServerSessionResponse.FromSession(result.Session!));
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EndSession(Guid sessionId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return Unauthorized();

        if (sessionManager.TryGetSession(sessionId, userContext.User.Id) is not null)
            await sessionManager.EndSessionAsync(sessionId, cancellationToken);

        return NoContent();
    }
}