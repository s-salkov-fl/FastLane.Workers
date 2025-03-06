using FastLane.Workers.Bucket.Services;
using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Bucket;

public class Program
{
	public static void Main(string[] args)
	{
		IHost host = Host.CreateDefaultBuilder(args)
			.ConfigureServices((context, services) =>
			{
				services.Configure<BusConfiguration>(context.Configuration.GetSection("BusConfig"));
				services.Configure<HostConfiguration>(context.Configuration.GetSection("HostConfig"));

				services.AddSingleton<WorkersDispatcher>();
				services.AddSingleton<WorkerFactory>();
				services.AddSingleton<IWorkerBus, WorkerBusRabbitMQ>(s =>
				{
					return new WorkerBusRabbitMQ(s.GetRequiredService<ILogger<WorkerBusRabbitMQ>>(),
						 s.GetRequiredService<IOptions<BusConfiguration>>(),
						 s,
						 true,
						 s.GetRequiredService<IOptions<HostConfiguration>>());
				});

				services.AddWorkerBusConsumers();
				services.AddHostedService<ProgramInitService>();
			})
			.Build();

		host.Run();
	}
}

public class ProgramInitService: IHostedService
{
	private readonly ILogger<Program> logger;
	private readonly HostConfiguration hostConfig;
	private readonly IWorkerBus workerBus;

	public ProgramInitService(ILogger<Program> logger, 
		IOptions<HostConfiguration> hostConfig,
		IWorkerBus workerBus)
	{
		this.logger = logger;
		this.hostConfig = hostConfig.Value;
		this.workerBus = workerBus;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		var cfg = hostConfig;
		logger.LogInformation($"{cfg.HostStartDate} Starting Host with HostId={cfg.HostId}, HostProcesId={cfg.HostProcessId}, HostName={cfg.HostDnsName}");
		logger.LogInformation($"Path where worker assemblies will be searched: {cfg.WorkersAssemblyFolder}");
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}