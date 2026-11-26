using Microsoft.Extensions.Hosting;
using Comprove.HelpDesk.Application;
using Comprove.HelpDesk.Infrastructure;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo;
using Comprove.HelpDesk.Worker.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHelpDeskApplication();
builder.Services.AddHelpDeskInfrastructure(builder.Configuration);
builder.Services.AddHelpDeskWorkers();

var host = builder.Build();
await host.Services.InitializeHelpDeskStorageAsync();
host.Run();
