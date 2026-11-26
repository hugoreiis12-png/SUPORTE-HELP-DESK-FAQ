using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Application.UseCases;

public sealed record AttachmentUpload(string AttachmentId, Uri UploadUrl, DateTimeOffset ExpiresAt);

// Metadados no Mongo, binário no object storage via URL temporária (B22/D-13). Nome do arquivo nunca vira chave.
public sealed class RequestAttachmentUseCase(
    ITicketRepository tickets,
    IAttachmentRepository attachments,
    ITicketHistoryRepository history,
    IObjectStorage storage,
    ITransactionRunner transactions,
    IClock clock,
    AttachmentLimits limits)
{
    public async Task<AttachmentUpload> ExecuteAsync(RequestContext context, string ticketId, string contentType, long sizeBytes, CancellationToken ct)
    {
        if (sizeBytes <= 0 || sizeBytes > limits.MaxBytes)
        {
            throw new DomainRuleViolationException($"Anexo deve ter entre 1 byte e {limits.MaxBytes} bytes.");
        }

        if (!AttachmentContentTypes.IsAllowed(contentType))
        {
            throw new DomainRuleViolationException("Anexo deve ser uma imagem PNG, JPG, WebP ou GIF.");
        }

        var ticket = await tickets.FindAsync(context.TenantId, ticketId, ct) ?? throw new TicketNotFoundException(ticketId);
        TicketAccess.EnsureCanSee(context, ticket);
        ticket.EnsureCanReceiveMessages();

        var now = clock.UtcNow;
        var attachmentId = Guid.NewGuid().ToString("N");
        var objectKey = $"{context.TenantId.Value}/{ticketId}/{attachmentId}";
        var uploadUrl = await storage.CreateUploadUrlAsync(objectKey, limits.UploadUrlTtl, ct);

        var attachment = new TicketAttachment(attachmentId, ticketId, context.TenantId, objectKey, contentType, sizeBytes, context.ActorId, now);
        await transactions.RunAsync(async token =>
        {
            await attachments.InsertAsync(attachment, token);
            await history.AppendAsync(AuditFactory.Create(ticket, TicketChangeType.AttachmentAdded, context.ActorId, now, context.CorrelationId), token);
        }, ct);

        return new AttachmentUpload(attachmentId, uploadUrl, now + limits.UploadUrlTtl);
    }
}
