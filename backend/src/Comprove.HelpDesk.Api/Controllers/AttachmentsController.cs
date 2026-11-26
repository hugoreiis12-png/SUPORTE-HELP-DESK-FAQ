using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Comprove.HelpDesk.Api.Controllers;

// Sem JWT: a assinatura do link é a autorização (B22). O tipo do arquivo é o declarado, nunca o do upload.
[ApiController]
[Route("api/v1/attachments")]
public sealed class AttachmentsController(
    IObjectStorage storage,
    IAttachmentRepository attachments,
    AttachmentLinkSigner signer,
    AttachmentLimits limits) : ControllerBase
{
    private const string DefaultContentType = "application/octet-stream";
    private const string DownloadFileName = "anexo.bin";

    [HttpPut("upload")]
    public async Task<IActionResult> Upload([FromQuery] string key, [FromQuery] long exp, [FromQuery] string sig, CancellationToken ct)
    {
        if (!IsAuthorized(key, exp, sig))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var attachment = await attachments.FindByObjectKeyAsync(key, ct);
        if (attachment is null)
        {
            return NotFound();
        }

        if (Request.ContentLength is not long length || length > Math.Min(attachment.SizeBytes, limits.MaxBytes))
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        await storage.StoreAsync(key, Request.Body, attachment.ContentType, ct);
        return NoContent();
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string key, [FromQuery] long exp, [FromQuery] string sig, CancellationToken ct)
    {
        if (!IsAuthorized(key, exp, sig))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var stored = await storage.OpenReadAsync(key, ct);
        if (stored is null)
        {
            return NotFound();
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        return AttachmentContentTypes.IsAllowed(stored.ContentType)
            ? File(stored.Content, stored.ContentType)
            : File(stored.Content, DefaultContentType, DownloadFileName);
    }

    private bool IsAuthorized(string key, long exp, string sig) =>
        signer.IsValid(Request.Path.Value ?? string.Empty, key, exp, sig, DateTimeOffset.UtcNow);
}
