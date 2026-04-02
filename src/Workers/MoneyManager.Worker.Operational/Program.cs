using TransactionSchedulerWorker.WorkerHost.DependencyInjection;

// Revert to simple worker startup - configuration issues need to be fixed first
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerHost(builder.Configuration);

var host = builder.Build();
host.Run();
