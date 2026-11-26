using Comprove.HelpDesk.Api.Auth;
using Comprove.HelpDesk.Api.Mapping;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comprove.HelpDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tickets")]
public sealed class TicketsController(
    OpenTicketUseCase open,
    TicketReadUseCase reads,
    UpdateTicketMetaUseCase updateMeta,
    AddTicketMessageUseCase addMessage,
    TransitionTicketUseCase transition,
    ResolveTicketUseCase resolve,
    RequestAttachmentUseCase attachments,
    IValidator<OpenTicketRequest> openValidator) : ControllerBase
{
    private RequestContext Context => RequestContextFactory.Create(HttpContext);

    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Open([FromBody] OpenTicketRequest request, CancellationToken ct)
    {
        var validation = await openValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var command = new OpenTicketCommand(
            request.Title,
            request.Description,
            request.CategoryId,
            ParseEnum<ImpactLevel>(request.Impact),
            ParseEnum<ScopeLevel>(request.Scope),
            ParseEnum<FrequencyLevel>(request.Frequency),
            ParseEnum<UrgencyLevel>(request.Urgency),
            request.IdempotencyKey);
        var ticket = await open.ExecuteAsync(Context, command, ct);
        var response = ToResponse(ticket);
        return CreatedAtAction(nameof(GetTicket), new { ticketId = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<TicketResponse>>> List(
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? cursor,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var parsedStatus = string.IsNullOrWhiteSpace(status) ? (TicketStatus?)null : ParseEnum<TicketStatus>(status);
        var page = await reads.ListAsync(Context, parsedStatus, cursor, pageSize, ct);
        var items = page.Items.Select(ToResponse).ToList();
        return Ok(new PagedResponse<TicketResponse>(items, page.NextCursor));
    }

    [HttpGet("{ticketId}", Name = nameof(GetTicket))]
    public async Task<ActionResult<TicketResponse>> GetTicket(string ticketId, CancellationToken ct)
    {
        var ticket = await reads.GetAsync(Context, ticketId, ct);
        return Ok(ToResponse(ticket));
    }

    [HttpPatch("{ticketId}/meta")]
    public async Task<IActionResult> UpdateMeta(string ticketId, [FromBody] UpdateTicketMetaRequest request, CancellationToken ct)
    {
        await updateMeta.ExecuteAsync(Context, ticketId, request.CategoryId, ct);
        return NoContent();
    }

    [HttpPost("{ticketId}/messages")]
    public async Task<IActionResult> AddMessage(string ticketId, [FromBody] AddTicketMessageRequest request, CancellationToken ct)
    {
        var message = await addMessage.ExecuteAsync(
            Context, ticketId, request.Body, ParseEnum<MessageVisibility>(request.Visibility), ct);
        return Ok(new { id = message.Id });
    }

    [HttpPost("{ticketId}/transitions")]
    public async Task<IActionResult> Transition(string ticketId, [FromBody] TransitionTicketRequest request, CancellationToken ct)
    {
        await transition.ExecuteAsync(Context, ticketId, FrontendStatusMapper.ParseTarget(request.TargetStatus), request.Reason, ct);
        return NoContent();
    }

    [Authorize(Policy = BubbleAuthenticationExtensions.SupportAgentPolicy)]
    [HttpPost("{ticketId}/resolve")]
    public async Task<ActionResult<TicketResponse>> Resolve(string ticketId, [FromBody] ResolveTicketRequest request, CancellationToken ct)
    {
        var ticket = await resolve.ExecuteAsync(Context, ticketId, request.IdempotencyKey, ct);
        return Ok(ToResponse(ticket));
    }

    [HttpPost("{ticketId}/attachments")]
    public async Task<ActionResult<AttachmentUploadResponse>> RequestAttachment(
        string ticketId, [FromBody] RequestAttachmentRequest request, CancellationToken ct)
    {
        var upload = await attachments.ExecuteAsync(Context, ticketId, request.ContentType, request.SizeBytes, ct);
        return Ok(new AttachmentUploadResponse(upload.AttachmentId, upload.UploadUrl.ToString(), upload.ExpiresAt));
    }

    private TicketResponse ToResponse(Ticket ticket) => TicketResponseMapper.ToResponse(ticket, Context.IsAgent);

    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new DomainRuleViolationException($"Valor inválido para {typeof(T).Name}: {value}.");
}
