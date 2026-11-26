namespace Comprove.HelpDesk.Application.Services;

// Preenchido a partir de StorageOptions na composição (Infrastructure).
public sealed record AttachmentLimits(long MaxBytes, TimeSpan UploadUrlTtl);
