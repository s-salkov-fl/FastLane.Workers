using FastLane.Workers.Bucket.Services;
using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using FastLane.Workers.Models;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Bucket.Consumers;

public class HostStatusRequestConsumer : WorkerBusConsumer<HostStatusRequest>
{
	private readonly ILogger<HostStatusRequestConsumer> _logger;
	private readonly HostConfiguration hostConfig;
	private readonly WorkersDispatcher workersDispatcher;

	public HostStatusRequestConsumer(ILogger<HostStatusRequestConsumer> logger,
		IOptions<HostConfiguration> hostConfig,
		IOptions<BusConfiguration> busConfiguration,
		WorkersDispatcher workersDispatcher) : base(busConfiguration, logger)
	{
		_logger = logger;
		this.hostConfig = hostConfig.Value;
		this.workersDispatcher = workersDispatcher;
	}

	public override Task Consume(IWorkerBus bus, HostStatusRequest message, CancellationToken cancelToken)
	{
		//_logger.LogDebug($"Received ping contextId={message.Id} datetime={DateTime.Now} hostId={message.HostId}");

		return bus.PublishFromHostToServer(new HostStatusResponse
		{
			Id = message.Id,
			HostId = hostConfig.HostId,
			HostDnsName = hostConfig.HostDnsName,
			HostProcessId = hostConfig.HostProcessId,
			HostStartDate =  DateTimeOffset.Parse(hostConfig.HostStartDate),
			NumberWorkersRun = workersDispatcher.workersInfo.Count(w => w.Status == WorkerInstanceExecutionStatus.Running),
			WorkerInstances = workersDispatcher.workersInfo.Select(w => new WorkerInstanceBriefInfo
			{
				InstanceId = w.WorkerInstanceId,
				Status = w.Status,
				TypeId = w.WorkerTypeName
			}),
			HealthStatus = HostServiceHealthStatus.Online
		}, cancelToken);
	}
}


