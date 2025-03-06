using FastLane.Workers.Bucket.Services;
using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using FastLane.Workers.Models;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Bucket.Consumers;

public class WorkerInstanceStatusRequestConsumer : WorkerBusConsumer<HostWorkerInstanceStatusRequest>
{
	private readonly ILogger<WorkerInstanceStatusRequestConsumer> _logger;
	private readonly WorkersDispatcher workersDispatcher;

	public WorkerInstanceStatusRequestConsumer(ILogger<WorkerInstanceStatusRequestConsumer> logger,
		IOptions<BusConfiguration> busConfiguration,
		WorkersDispatcher workersDispatcher) : base (busConfiguration, logger)
	{
		_logger = logger;
		this.workersDispatcher = workersDispatcher;
	}

	public override Task Consume(IWorkerBus bus, HostWorkerInstanceStatusRequest message, CancellationToken cancelToken)
	{
		Guid instanceId = message.WorkerInstanceId;
		WorkerContainer? targetWorkerInfo = workersDispatcher.workersInfo.FirstOrDefault<WorkerContainer>(w => w.WorkerInstanceId == instanceId);
		
		if (targetWorkerInfo == null) 
		{
			throw new ArgumentOutOfRangeException(nameof(message.WorkerInstanceId), $"Worker instance {instanceId} not found in Host workers pool");
		}
		
		return bus.PublishFromHostToServer(new HostWorkerInstanceStatusResponse
		{
			Id = message.Id,
			HostId = message.HostId,
			InstanceId = instanceId,
			TypeId = targetWorkerInfo.WorkerTypeName,
			Status = targetWorkerInfo.Status,
			Input = targetWorkerInfo.Input.ToString(),
			Result = targetWorkerInfo.Result.ToString()
		}, cancelToken);
	}
}

