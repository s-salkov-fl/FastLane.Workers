using FastLane.Workers.Configuration;

namespace FastLane.Workers.Contract.Services;

public interface IWorkerBus
{
	Task Publish(IWorkerBusMessage message,
		string exchangePoint,
		string routingKey,
		CancellationToken cancellationToken = default);

	Task PublishFromHostToServer(IWorkerBusMessage message, CancellationToken cancellationToken = default);

	Task PublishFromServerToHostBroadCast(IWorkerBusMessage message, CancellationToken cancellationToken = default);

	Task PublishFromServerToHostDirect(IWorkerBusMessage message, string hostId, CancellationToken cancellationToken = default);
}
