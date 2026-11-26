using Comprove.HelpDesk.Api.Auth;
using Comprove.HelpDesk.Api.Mapping;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comprove.HelpDesk.Api.Controllers;

// Visão do Help Desk. Somente agentes de suporte; ações de escrita reutilizam /api/v1/tickets.
[ApiController]
[Authorize(Policy = BubbleAuthenticationExtensions.SupportAgentPolicy)]
[Route("api/v1/support/tickets")]
public sealed class SupportTicketsController(
    SupportTicketViewUseCase views,
    PublishResolutionUseCase publishResolution) : ControllerBase
{
    private RequestContext Context => RequestContextFactory.Create(HttpContext);

    [HttpGet]
    public async Task<ActionResult<PagedResponse<SupportTicketResponse>>> List(
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? cursor,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var parsedStatus = string.IsNullOrWhiteSpace(status) ? (TicketStatus?)null : ParseStatus(status);
        var page = await views.ListAsync(Context, parsedStatus, cursor, pageSize, ct);
        var items = page.Items.Select(SupportTicketMapper.ToResponse).ToList();
        return Ok(new PagedResponse<SupportTicketResponse>(items, page.NextCursor));
    }

    [HttpGet("{ticketId}")]
    public async Task<ActionResult<SupportTicketResponse>> Get(string ticketId, CancellationToken ct) =>
        Ok(SupportTicketMapper.ToResponse(await views.GetAsync(Context, ticketId, ct)));

    // "Resolver e sincronizar com o FAQ": move o chamado, grava a resolução oficial e devolve o chamado atualizado.
    [HttpPost("{ticketId}/resolution")]
    public async Task<ActionResult<SupportTicketResponse>> PublishResolution(
        string ticketId,
        [FromBody] PublishResolutionRequest request,
        CancellationToken ct)
    {
        var command = new PublishResolutionCommand(
            ticketId,
            request.ResolutionSummary,
            FrontendFaqStatusMapper.Parse(request.FaqStatus),
            request.IdempotencyKey);
        await publishResolution.ExecuteAsync(Context, command, ct);
        return Ok(SupportTicketMapper.ToResponse(await views.GetAsync(Context, ticketId, ct)));
    }

    private static TicketStatus ParseStatus(string value) =>
        Enum.TryParse<TicketStatus>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new DomainRuleViolationException($"Status inválido: {value}.");
}
