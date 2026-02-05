using TransactionSchedulerWorker.WorkerHost.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerHost(builder.Configuration);

var host = builder.Build();
host.Run();
