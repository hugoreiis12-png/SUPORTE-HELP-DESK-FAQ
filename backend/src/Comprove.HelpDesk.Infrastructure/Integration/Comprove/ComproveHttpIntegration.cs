using System.Net.Http.Headers;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Comprove.HelpDesk.Infrastructure.Integration.Comprove;

// ACL do ERP. Timeout, retry, circuit breaker e bulkhead vêm do AddStandardResilienceHandler (B19).
// Não registra token nem payload em log.
public sealed class ComproveHttpIntegration(HttpClient http, IOptions<ComproveOptions> options) : IComproveIntegration
{
    private const string CorrelationHeader = "X-Correlation-Id";

    public async Task<string> QueryAsync(ComproveQuery query, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, query.Kind);
        request.Headers.Add(CorrelationHeader, query.CorrelationId);
        if (!string.IsNullOrWhiteSpace(options.Value.ApiToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiToken);
        }

        request.Content = System.Net.Http.Json.JsonContent.Create(new { ticket_id = query.TicketId, tenant_id = query.TenantId });
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
