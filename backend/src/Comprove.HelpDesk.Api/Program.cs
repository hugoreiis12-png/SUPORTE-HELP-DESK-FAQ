using Comprove.HelpDesk.Api.Configuration;
using Comprove.HelpDesk.Api.Middlewares;
using Comprove.HelpDesk.Api.SignalR;
using Comprove.HelpDesk.Application;
using Comprove.HelpDesk.Infrastructure;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHelpDeskApplication();
builder.Services.AddHelpDeskInfrastructure(builder.Configuration);
builder.Services.AddHelpDeskApi(builder.Configuration);

var app = builder.Build();
await app.Services.InitializeHelpDeskStorageAsync();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(HelpDeskCorsOptions.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TicketsHub>("/hubs/tickets");
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
