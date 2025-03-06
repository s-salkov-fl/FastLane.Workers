using FastLane.Workers.Contracts;
using FastLane.Workers.Configuration;
using FastLane.Workers.Models;
using FastLane.Workers.Contract.Services;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Server.Consumers;

public class HostStatusResponseConsumer : WorkerBusConsumer<HostStatusResponse>
{
	private readonly ILogger<HostStatusResponseConsumer> _logger;
	private readonly HostsState hostsState;

	public HostStatusResponseConsumer(ILogger<HostStatusResponseConsumer> logger,
		IOptions<BusConfiguration> busConfiguration,
		HostsState hostsState) : base(busConfiguration, logger)
	{
		_logger = logger;
		this.hostsState = hostsState;
	}

	public override Task Consume(IWorkerBus bus, HostStatusResponse message, CancellationToken cancelToken)
	{
		hostsState.AddHostStatus(
			new HostStatus
			{
				HostId = message.HostId,
				HostDnsName = message.HostDnsName,
				HostProcessId = message.HostProcessId,
				HostStartDate = message.HostStartDate,
				HostObtainStatusDate = DateTimeOffset.Now,
				LastResponseId = message.Id,
				NumberWorkersRun = message.NumberWorkersRun,
				WorkerInstances = message.WorkerInstances,
				HostHealth = message.HealthStatus
			});

		return Task.CompletedTask;
	}

}