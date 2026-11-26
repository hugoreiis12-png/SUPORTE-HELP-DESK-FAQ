using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Fila por categoria + rodízio de agentes. O contador é incrementado atomicamente (B17: rodízio auditável).
public sealed class TeamRoster(MongoContext mongo) : ITeamRoster
{
    // Categoria sem fila própria cai aqui: o chamado nunca para na triagem por falta de cadastro.
    private const string DefaultQueueId = "suporte-geral";

    public async Task<TeamAssignment> NextAssignmentAsync(string categoryId, CancellationToken ct)
    {
        var queue = await mongo.Collection<SupportQueueDocument>(MongoCollectionNames.SupportQueues)
            .Find(Builders<SupportQueueDocument>.Filter.AnyEq(d => d.CategoryIds, categoryId))
            .FirstOrDefaultAsync(ct);
        var queueId = queue?.Id ?? DefaultQueueId;
        var teams = mongo.Collection<SupportTeamDocument>(MongoCollectionNames.SupportTeams);
        var filter = Builders<SupportTeamDocument>.Filter.Eq(d => d.QueueId, queueId);
        var update = Builders<SupportTeamDocument>.Update.Inc(d => d.NextIndex, 1L);
        var options = new FindOneAndUpdateOptions<SupportTeamDocument> { ReturnDocument = ReturnDocument.Before };
        var team = await teams.FindOneAndUpdateAsync(filter, update, options, ct);

        return new TeamAssignment(queueId, PickAgent(team));
    }

    private static string? PickAgent(SupportTeamDocument? team)
    {
        if (team is null || team.Agents.Count == 0)
        {
            return null;
        }

        return team.Agents[(int)(team.NextIndex % team.Agents.Count)];
    }
}
