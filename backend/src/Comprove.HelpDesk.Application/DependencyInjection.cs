using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Comprove.HelpDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddHelpDeskApplication(this IServiceCollection services)
    {
        services.AddScoped<ProtocolGenerator>();
        services.AddScoped<OpenTicketUseCase>();
        services.AddScoped<ClassifyTicketUseCase>();
        services.AddScoped<DispatchTicketUseCase>();
        services.AddScoped<TransitionTicketUseCase>();
        services.AddScoped<ResolveTicketUseCase>();
        services.AddScoped<AddTicketMessageUseCase>();
        services.AddScoped<UpdateTicketMetaUseCase>();
        services.AddScoped<RequestAttachmentUseCase>();
        services.AddScoped<TicketReadUseCase>();
        services.AddScoped<AttachmentLinkResolver>();
        services.AddScoped<CreateFaqUseCase>();
        services.AddScoped<FaqReadUseCase>();
        services.AddScoped<SupportTicketViewUseCase>();
        services.AddScoped<PublishResolutionUseCase>();
        services.AddScoped<NotifyResolutionUseCase>();
        return services;
    }
}
