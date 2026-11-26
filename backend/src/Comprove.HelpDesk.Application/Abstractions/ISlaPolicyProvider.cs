using Comprove.HelpDesk.Domain.Rules;

namespace Comprove.HelpDesk.Application.Abstractions;

public interface ISlaPolicyProvider
{
    Task<SlaPolicy> GetActiveAsync(CancellationToken ct);
}
