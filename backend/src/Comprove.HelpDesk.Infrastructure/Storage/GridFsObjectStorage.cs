using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Infrastructure.Configuration;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Comprove.HelpDesk.Infrastructure.Storage;

// Anexos no MongoDB via GridFS (D-13 / ADR-010). Links temporários são assinados e apontam para a própria API.
public sealed class GridFsObjectStorage : IObjectStorage
{
    private const string BucketName = "attachments";
    private const string ContentTypeKey = "content_type";
    private const string DefaultContentType = "application/octet-stream";
    private const string UploadPath = "/api/v1/attachments/upload";
    private const string DownloadPath = "/api/v1/attachments/download";

    private readonly GridFSBucket _bucket;
    private readonly AttachmentLinkSigner _signer;
    private readonly Uri _publicBase;

    public GridFsObjectStorage(MongoContext mongo, AttachmentLinkSigner signer, IOptions<StorageOptions> options)
    {
        _bucket = new GridFSBucket(mongo.Database, new GridFSBucketOptions { BucketName = BucketName });
        _signer = signer;
        _publicBase = new Uri(options.Value.PublicBaseUrl);
    }

    public Task<Uri> CreateUploadUrlAsync(string objectKey, TimeSpan validFor, CancellationToken ct) =>
        Task.FromResult(BuildSignedUrl(UploadPath, objectKey, validFor));

    public Task<Uri> CreateDownloadUrlAsync(string objectKey, TimeSpan validFor, CancellationToken ct) =>
        Task.FromResult(BuildSignedUrl(DownloadPath, objectKey, validFor));

    public async Task StoreAsync(string objectKey, Stream content, string contentType, CancellationToken ct)
    {
        var options = new GridFSUploadOptions { Metadata = new BsonDocument(ContentTypeKey, contentType) };
        await _bucket.UploadFromStreamAsync(objectKey, content, options, ct);
    }

    public async Task<IReadOnlySet<string>> FindStoredAsync(IReadOnlyCollection<string> objectKeys, CancellationToken ct)
    {
        var filter = Builders<GridFSFileInfo>.Filter.In(file => file.Filename, objectKeys);
        using var cursor = await _bucket.FindAsync(filter, cancellationToken: ct);
        return (await cursor.ToListAsync(ct)).Select(file => file.Filename).ToHashSet();
    }

    public async Task<StoredObject?> OpenReadAsync(string objectKey, CancellationToken ct)
    {
        try
        {
            var download = await _bucket.OpenDownloadStreamByNameAsync(objectKey, cancellationToken: ct);
            var contentType = download.FileInfo.Metadata?.GetValue(ContentTypeKey, DefaultContentType).AsString ?? DefaultContentType;
            return new StoredObject(download, contentType);
        }
        catch (GridFSFileNotFoundException)
        {
            return null;
        }
    }

    private Uri BuildSignedUrl(string path, string objectKey, TimeSpan validFor)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(validFor);
        var signature = _signer.Sign(path, objectKey, expiresAt);
        var query = $"key={Uri.EscapeDataString(objectKey)}&exp={expiresAt.ToUnixTimeSeconds()}&sig={signature}";
        return new Uri(_publicBase, $"{path}?{query}");
    }
    
}
