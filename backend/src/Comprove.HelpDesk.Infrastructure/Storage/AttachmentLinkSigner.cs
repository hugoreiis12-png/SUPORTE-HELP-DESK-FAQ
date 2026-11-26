using System.Security.Cryptography;
using System.Text;
using Comprove.HelpDesk.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Comprove.HelpDesk.Infrastructure.Storage;

// HMAC-SHA256 de caminho + objeto + expiração. A chave vem de Storage:LinkSigningKey (env).
public sealed class AttachmentLinkSigner(IOptions<StorageOptions> options)
{
    public string Sign(string path, string objectKey, DateTimeOffset expiresAt)
    {
        var payload = Encoding.UTF8.GetBytes($"{path}\n{objectKey}\n{expiresAt.ToUnixTimeSeconds()}");
        var key = Encoding.UTF8.GetBytes(options.Value.LinkSigningKey);
        return Convert.ToHexString(HMACSHA256.HashData(key, payload)).ToLowerInvariant();
    }

    public bool IsValid(string path, string objectKey, long expiresAtUnix, string signature, DateTimeOffset now)
    {
        if (now.ToUnixTimeSeconds() > expiresAtUnix)
        {
            return false;
        }

        var expected = Sign(path, objectKey, DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix));
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }
}
