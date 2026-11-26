using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Services;

// Sequência por ano (B08). A unicidade final é garantida pelo índice em tickets.protocol.
public sealed class ProtocolGenerator(IProtocolSequence sequence)
{
    public async Task<ProtocolNumber> NextAsync(int year, CancellationToken ct) =>
        ProtocolNumber.Create(year, await sequence.NextAsync(year, ct));
}
