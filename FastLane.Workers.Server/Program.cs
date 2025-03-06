using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Models;
using FastLane.Workers.Server.BackgroundServices;
using FastLane.Workers.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Server;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddControllers();

		IConfiguration config = builder.Configuration;

		builder.Services.Configure<BusConfiguration>(config.GetSection("BusConfig"));
		var serverConfigSection = config.GetSection("ServerConfig");
		var serverConfig = serverConfigSection.Get<ServerConfiguration>();
		builder.Services.Configure<ServerConfiguration>(serverConfigSection);

		builder.Services.AddSingleton(new HostsState(serverConfig.PingRequestResponseTimeOutMs));
		builder.Services.AddSingleton<WorkerInstancesState>();
		builder.Services.AddSingleton<StopWorkersState>();
		builder.Services.AddSingleton<RunNewWorkersState>();
		builder.Services.AddSingleton<RestartWorkersState>();
		builder.Services.AddSingleton<HostsStatsCacheManager>();
		builder.Services.AddSingleton<ServerStatus>();
		builder.Services.AddSingleton<IWorkerBus, WorkerBusRabbitMQ>(s => {
			return new WorkerBusRabbitMQ(s.GetRequiredService<ILogger<WorkerBusRabbitMQ>>(),
				 s.GetRequiredService<IOptions<BusConfiguration>>(),
				 s);
			});
		builder.Services.AddWorkerBusConsumers();

		builder.Services.AddHostedService<ProgramInitService>();
		builder.Services.AddHostedService<ScheduledRoutines>();

		if (builder.Environment.IsDevelopment())
		{
			builder.Services.AddSwaggerGen(o =>
			{
				var filePath = Path.Combine(System.AppContext.BaseDirectory, "FastLane.Workers.Server.xml");
				o.IncludeXmlComments(filePath);
			});
		}

		var app = builder.Build();

		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee API V1");
			});
		}

		app.UseAuthorization();
		app.MapControllers();
		
		app.Run();
	}
}

public class ProgramInitService : IHostedService
{
	private readonly ILogger<Program> logger;
	private readonly ServerConfiguration serverConfig;
	private readonly BusConfiguration busConfig;
	private readonly IWorkerBus bus;

	public ProgramInitService(ILogger<Program> logger, 
		IOptions<ServerConfiguration> serverConfig,
		IOptions<BusConfiguration> busConfig)
	{
		this.logger = logger;
		this.serverConfig = serverConfig.Value;
		this.busConfig = busConfig.Value;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		var cfg = serverConfig;
		logger.LogInformation($"{DateTime.Now} Starting Server with ServerId={busConfig.ServerId}.");
		logger.LogInformation($"Path where worker assemblies will be searched: {cfg.WorkersAssemblyFolder}");
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}