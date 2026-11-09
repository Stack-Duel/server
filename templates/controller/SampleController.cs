using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.RateLimiting;
using __SERVICE_NAMESPACE__;

namespace __NAMESPACE__;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class SampleController(ISampleService service) : ControllerBase
{
    [HttpGet]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
    {
        // TODO: implement, e.g. return this.ToActionResult(await service.GetAllAsync(cancellationToken));
        throw new NotImplementedException();
    }
}
