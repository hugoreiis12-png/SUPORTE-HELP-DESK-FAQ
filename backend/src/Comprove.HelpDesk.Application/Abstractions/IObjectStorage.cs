namespace Comprove.HelpDesk.Application.Abstractions;

public sealed record StoredObject(Stream Content, string ContentType);

// Anexos ficam no MongoDB (GridFS), decisão D-13 / ADR-010. Links temporários são assinados pela API.
public interface IObjectStorage
{
    Task<Uri> CreateUploadUrlAsync(string objectKey, TimeSpan validFor, CancellationToken ct);

    Task<Uri> CreateDownloadUrlAsync(string objectKey, TimeSpan validFor, CancellationToken ct);

    Task StoreAsync(string objectKey, Stream content, string contentType, CancellationToken ct);

    // Das chaves pedidas, quais já têm binário gravado (upload concluído).
    Task<IReadOnlySet<string>> FindStoredAsync(IReadOnlyCollection<string> objectKeys, CancellationToken ct);

    Task<StoredObject?> OpenReadAsync(string objectKey, CancellationToken ct);
}
