using MoneyManager.Infrastructure.Configuration;
using TransactionSchedulerWorker.WorkerHost.DependencyInjection;

// Revert to simple worker startup - configuration issues need to be fixed first
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerHost(builder.Configuration);

var host = builder.Build();

// Verificação das variáveis de ambiente: problema crítico interrompe a subida; o restante é registrado
// como erro no log e como ProcessLog "EnvironmentValidation" com falha na observabilidade do Backoffice
var environmentReport = EnvironmentValidator.Validate(
    "MoneyManager.Worker.Operational", builder.Configuration, EnvironmentRequirements.Worker);
EnvironmentValidator.ReportOrThrow(environmentReport, host.Services, failOnCritical: !builder.Environment.IsDevelopment());

host.Run();
