using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Rules;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Lê a matriz ativa de sla_policies (ADR-008). Sem documento = erro explícito, não default silencioso.
public sealed class MongoSlaPolicyProvider(MongoContext mongo) : ISlaPolicyProvider
{
    private const string ActivePolicyId = "active";

    public async Task<SlaPolicy> GetActiveAsync(CancellationToken ct)
    {
        var document = await mongo.Collection<SlaPolicyDocument>(MongoCollectionNames.SlaPolicies)
            .Find(Builders<SlaPolicyDocument>.Filter.Eq(d => d.Id, ActivePolicyId))
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("sla_policies sem documento 'active'. Rode a inicialização do storage.");

        var targets = document.TargetsHours.ToDictionary(
            pair => Enum.Parse<Severity>(pair.Key, ignoreCase: false),
            pair => TimeSpan.FromHours(pair.Value));
        return new SlaPolicy(targets);
    }
}
