using Comprove.HelpDesk.Worker.Consumers;
using Comprove.HelpDesk.Worker.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace Comprove.HelpDesk.Worker.Configuration;

public static class WorkerServiceExtensions
{
    public static IServiceCollection AddHelpDeskWorkers(this IServiceCollection services)
    {
        services.AddHostedService<OutboxRelayService>();
        services.AddHostedService<TicketCreatedClassificationWorker>();
        services.AddHostedService<TicketClassifiedDispatchWorker>();
        services.AddHostedService<TicketResolvedNotifyWorker>();
        services.AddHostedService<ComproveIntegrationWorker>();
        return services;
    }
}
