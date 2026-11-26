using Comprove.HelpDesk.Api.Auth;
using Comprove.HelpDesk.Contracts.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comprove.HelpDesk.Api.Controllers;

// Identidade do token do Bubble. O front usa para saber quem é o usuário e se vê o Help Desk.
[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class MeController : ControllerBase
{
    [HttpGet]
    public ActionResult<MeResponse> Get()
    {
        var context = RequestContextFactory.Create(HttpContext);
        return Ok(new MeResponse(
            context.ActorId,
            context.TenantId.Value,
            context.ActorName,
            context.ActorEmail,
            context.ActorUnit,
            context.Roles.ToArray(),
            context.IsAgent));
    }
}
