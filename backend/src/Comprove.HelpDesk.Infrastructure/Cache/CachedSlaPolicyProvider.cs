using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.Rules;
using Microsoft.Extensions.Caching.Memory;

namespace Comprove.HelpDesk.Infrastructure.Cache;

// Cache-aside só para regra de configuração (ADR-009). Chave inclui rules_version.
public sealed class CachedSlaPolicyProvider(ISlaPolicyProvider inner, IMemoryCache cache, string rulesVersion) : ISlaPolicyProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private string CacheKey => $"sla_policy:{rulesVersion}";

    public Task<SlaPolicy> GetActiveAsync(CancellationToken ct) =>
        cache.GetOrCreateAsync(CacheKey, entry => LoadAsync(entry, ct))!;

    private async Task<SlaPolicy> LoadAsync(ICacheEntry entry, CancellationToken ct)
    {
        entry.AbsoluteExpirationRelativeToNow = Ttl;
        return await inner.GetActiveAsync(ct);
    }
}
