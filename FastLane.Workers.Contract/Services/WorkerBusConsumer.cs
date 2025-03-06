using FastLane.Workers.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Contract.Services
{
	public abstract class WorkerBusConsumer<TMessage> : IWorkerBusConsumer, IWorkerBusMessage
	{
		protected BusConfiguration busConfiguration;
		protected ILogger logger;
		public Type ConsumerEntityType => typeof(TMessage);

		public virtual string ContentEncoding { get; set; }
		public virtual string ContentType { get; set; }
		public long TimeStamp { get; set; }
		public virtual string Expiration
		{
			get => busConfiguration.ResponseWaitTimeMilliseconds.ToString();
			set { }
		}

		public WorkerBusConsumer(IOptions<BusConfiguration> busConfig, ILogger logger = null)
		{
			busConfiguration = busConfig.Value;
			this.logger = logger;
		}

		public abstract Task Consume(IWorkerBus bus, TMessage message, CancellationToken cancelToken);

		public async Task Consume(IWorkerBus bus, object message, CancellationToken cancelToken)
		{
			try
			{
				await Consume(bus, (TMessage)message, cancelToken);
			}
			catch(Exception ex)
			{
				logger?.LogError(ex.ToString());
				throw;
			}
		}
	}

	public interface IWorkerBusConsumer
	{
		public Type ConsumerEntityType { get; }
		public Task Consume(IWorkerBus bus, object message, CancellationToken cancelToken);
	}
}
