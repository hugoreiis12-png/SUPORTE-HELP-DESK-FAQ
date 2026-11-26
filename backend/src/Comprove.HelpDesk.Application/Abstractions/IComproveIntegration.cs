namespace Comprove.HelpDesk.Application.Abstractions;

public sealed record ComproveQuery(string Kind, string TicketId, string TenantId, string CorrelationId);

// ACL: única porta para o ERP Comprove (ADR-003/D-05). Nunca acessa collections do ERP.
public interface IComproveIntegration
{
    Task<string> QueryAsync(ComproveQuery query, CancellationToken ct);
}
