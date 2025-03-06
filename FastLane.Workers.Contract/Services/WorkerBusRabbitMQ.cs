using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Exceptions;
using FastLane.Workers.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace FastLane.Workers.Contract.Services;

public class WorkerBusRabbitMQ : IWorkerBus, IDisposable
{
	private readonly BusConfiguration busConfiguration;
	private readonly ILogger<WorkerBusRabbitMQ> logger;
	private readonly IServiceProvider serviceProvider;
	private ConnectionFactory connectionFactory;
	private IConnection connectionBus;
	private IModel channelPublish;
	private IModel channelConsume;
	private bool isDisposed = false;
	private Dictionary<Type, Type> consumersEntityMatch = new Dictionary<Type, Type>();
	private CancellationTokenSource cancelStopSource = new CancellationTokenSource();

	public WorkerBusRabbitMQ(ILogger<WorkerBusRabbitMQ> logger,
		IOptions<BusConfiguration> busConfiguration,
		IServiceProvider serviceProvider,
		bool isHost = false,
		IOptions<HostConfiguration> hostConfig = null)
	{
		if (busConfiguration == null) throw new ArgumentNullException(nameof(busConfiguration));

		this.busConfiguration = busConfiguration.Value;
		this.logger = logger;
		this.serviceProvider = serviceProvider;
		InitAndConfigure(isHost, hostConfig?.Value);
	}

	public void Stop()
	{
		cancelStopSource.Cancel();
	}

	private void InitAndConfigure(bool configureAsHost = false, HostConfiguration hostConfig = null)
	{
		RefreshConsumers();

		connectionFactory = new ConnectionFactory
		{
			HostName = busConfiguration.Host,
			Port = busConfiguration.Port,
			DispatchConsumersAsync = true,
			UserName = busConfiguration.UserName,
			Password = busConfiguration.Password
		};

		connectionBus = connectionFactory.CreateConnection();
		channelPublish = connectionBus.CreateModel();
		channelConsume = connectionBus.CreateModel();

		var hostsBroadCastExchange = busConfiguration.HostsBroadCastExchange;
		var hostsDirectExchange = busConfiguration.HostsDirectExchange;
		var serverId = busConfiguration.ServerId;
		var serversExchangeTemplate = busConfiguration.ServersExchangeTemplate;
		var serversExchangeName = string.Format(serversExchangeTemplate, serverId);
		var queueServerTemplate = busConfiguration.QueueServerTemplate;
		var queueServerName = string.Format(queueServerTemplate, serverId);

		channelPublish.ExchangeDeclare(hostsBroadCastExchange, "fanout");
		channelPublish.ExchangeDeclare(hostsDirectExchange, "direct");
		channelPublish.ExchangeDeclare(serversExchangeName, "fanout");

		channelPublish.QueueDeclare(queueServerName, false, false, false);
		channelPublish.QueueBind(queueServerName, serversExchangeName, "");
		string consumersQueueName = queueServerName;

		if (configureAsHost)
		{
			var hostId = hostConfig.HostId;
			var queueHostTemplate = busConfiguration.QueueHostTemplate;
			var queueHostName = string.Format(queueHostTemplate, hostId);

			var consumerCount = channelPublish.QueueDeclare(queueHostName, false, false, false).ConsumerCount;

			if (consumerCount > 0)
				if (hostConfig.StartFailIfQueueAlreadyInUse)
					throw new ConnectFailureException("Can not finish host's bus configuration due to its queue already has consumers", null);
				else
					logger.LogWarning("Host started with its queue(" + queueHostName + ") already has connections. May be another host with same HostId already sterted.");

			channelPublish.QueueBind(queueHostName, hostsDirectExchange, hostId);
			channelPublish.QueueBind(queueHostName, hostsBroadCastExchange, "");

			consumersQueueName = queueHostName;
		}

		var internalConsumer = new AsyncEventingBasicConsumer(channelConsume);
		internalConsumer.Received += InternalConsumer_Received;

		channelConsume.BasicConsume(queue: consumersQueueName, autoAck: true, consumer: internalConsumer);
	}

	private Task InternalConsumer_Received(object sender, BasicDeliverEventArgs eventArgs)
	{
		if (isDisposed) throw new ObjectDisposedException(nameof(WorkerBusRabbitMQ));
		if (eventArgs?.BasicProperties == null) throw new ArgumentNullException("Unable to read incoming bus message properties.", nameof(eventArgs.BasicProperties));

		if (!eventArgs.BasicProperties.IsTypePresent()) throw new BusMessageException("Incoming message does not contain Type property");
		if (!eventArgs.BasicProperties.IsContentEncodingPresent()) throw new BusMessageException("Incoming message does not contain ContentEncoding property");
		if (!eventArgs.BasicProperties.IsContentTypePresent()) throw new BusMessageException("Incoming message does not contain ContentType property");
		if (!eventArgs.BasicProperties.IsExpirationPresent()) throw new BusMessageException("Incoming message does not contain Expiration property");
		if (!eventArgs.BasicProperties.IsTimestampPresent()) throw new BusMessageException("Incoming message does not contain TimeStamp property");

		var timestamp = eventArgs.BasicProperties.Timestamp.UnixTime;
		var dateSended = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
		var consumerEntityTypeName = eventArgs.BasicProperties.Type;
		long expMessMs;

		if (!long.TryParse(eventArgs.BasicProperties.Expiration, out expMessMs))
		{
			throw new BusMessageException(
				"Incoming message's TimeStamp property has invalid format, should be number but current value is '" + eventArgs.BasicProperties.Expiration + "'");
		}

		if (expMessMs > 0)
		{
			var dateExpire = dateSended.AddMilliseconds(expMessMs);
			if (dateExpire < DateTimeOffset.Now)
			{
				logger.LogWarning($"Message with type {consumerEntityTypeName} is Expired. It was sended at {dateSended} and has Expiration={expMessMs}");
				return Task.CompletedTask;
			}
		}

		var consumerEntityTypeMatch = consumersEntityMatch.FirstOrDefault(p => p.Key.FullName == consumerEntityTypeName);
		var consumerEntityType = consumerEntityTypeMatch.Key;
		var consumerType = consumerEntityTypeMatch.Value;
		if (consumerEntityType == null || consumerType == null)
			throw new BusMessageException("Unable to FIND matching consumer for message type: " + consumerEntityTypeName);

		var consumerInstance = serviceProvider.GetService(consumerType) as IWorkerBusConsumer;
		if (consumerInstance == null)
			throw new BusMessageException("Unable to CONSTRUCT matching consumer of type: " + consumerType.AssemblyQualifiedName);

		object messageObject = null;
		var contentType = eventArgs.BasicProperties.ContentType;

		if (contentType == MediaTypeNames.Application.Json)
		{
			using (var stream = new MemoryStream(eventArgs.Body.ToArray()))
				messageObject = JsonSerializer.Deserialize(stream, consumerEntityType);
		}

		if (contentType == MediaTypeNames.Application.Zip)
		{
			var enc = Encoding.UTF8;
			messageObject = new FileDownloadResponse
			{
				HostId = enc.GetString((byte[])eventArgs.BasicProperties.Headers["FileDownload_HostId"]),
				WorkerAssemblyName = enc.GetString((byte[])eventArgs.BasicProperties.Headers["FileDownload_WorkerAssemblyName"]),
				Id = Guid.Parse(enc.GetString((byte[])eventArgs.BasicProperties.Headers["FileDownload_Id"])),
				WorkerInstanceId = Guid.Parse(enc.GetString((byte[])eventArgs.BasicProperties.Headers["FileDownload_WorkerInstanceId"])),
				FileBody = eventArgs.Body.ToArray()
			};
		}

		if (messageObject == null) throw new BusMessageException($"Invalid Content Type:'{contentType}' in for message type: " + consumerEntityTypeName);

		return consumerInstance.Consume(this, messageObject, cancelStopSource.Token);
	}

	public Task Publish(IWorkerBusMessage message,
		string exchangePoint,
		string routingKey,
		CancellationToken cancellationToken = default)
	{
		if (isDisposed) throw new ObjectDisposedException(nameof(WorkerBusRabbitMQ));
		if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
		if (message == null) throw new ArgumentNullException(nameof(message));

		if (message == null) throw new ArgumentNullException(nameof(message));

		var props = channelPublish.CreateBasicProperties();
		props.Type = message.GetType().FullName;
		props.ContentEncoding = string.IsNullOrEmpty(message.ContentEncoding) ? Encoding.UTF8.BodyName : message.ContentEncoding;
		props.ContentType = string.IsNullOrEmpty(message.ContentType) ? MediaTypeNames.Application.Json : message.ContentType;
		props.Expiration = !string.IsNullOrEmpty(message.Expiration) ? message.Expiration : busConfiguration.ResponseWaitTimeMilliseconds.ToString();

		props.Timestamp = new AmqpTimestamp(DateTimeOffset.Now.ToUnixTimeMilliseconds());

		if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);

		byte[] bytes;

		if (props.ContentType == MediaTypeNames.Application.Json)
		{
			bytes = JsonSerializer.SerializeToUtf8Bytes(message, message.GetType());
		}
		else if (props.ContentType == MediaTypeNames.Application.Zip)
		{
			var typedMessage = message as FileDownloadResponse;

			if (typedMessage == null) throw new InvalidDataException($"Invalid message type('{props.Type}') for outgoing FILE message, should be FileDownloadResponse");

			bytes = typedMessage.FileBody;
			props.Headers = new Dictionary<string, object>() {
				{ "FileDownload_HostId", typedMessage.HostId },
				{ "FileDownload_WorkerAssemblyName", typedMessage.WorkerAssemblyName },
				{ "FileDownload_Id", typedMessage.Id.ToString() },
				{ "FileDownload_WorkerInstanceId", typedMessage.WorkerInstanceId.ToString()},
			};
		}
		else
			throw new InvalidDataException($"Invalid content type('{props.ContentType}') for outgoing message: Type:'{props.Type}'");

		lock (channelPublish)
		{
			channelPublish.BasicPublish(exchange: exchangePoint,
								 routingKey: routingKey,
								 body: bytes,
								 basicProperties: props);
		}

		return Task.CompletedTask;
	}

	public Task PublishFromHostToServer(IWorkerBusMessage message, CancellationToken cancellationToken = default)
	{
		var serverId = busConfiguration.ServerId;
		var serversExchangeTemplate = busConfiguration.ServersExchangeTemplate;
		var serversExchangeName = string.Format(serversExchangeTemplate, serverId);
		return Publish(message, serversExchangeName, "", cancellationToken);
	}

	public Task PublishFromServerToHostBroadCast(IWorkerBusMessage message, CancellationToken cancellationToken = default)
	{
		return Publish(message, busConfiguration.HostsBroadCastExchange, "", cancellationToken);
	}

	public Task PublishFromServerToHostDirect(IWorkerBusMessage message, string hostId, CancellationToken cancellationToken = default)
	{
		return Publish(message, busConfiguration.HostsDirectExchange, hostId, cancellationToken);
	}

	public void Dispose()
	{
		try
		{
			Stop();
			isDisposed = true;
			cancelStopSource.Dispose();
			channelConsume?.Dispose();
			channelPublish?.Dispose();
			connectionBus?.Dispose();
		}
		catch (Exception ex)
		{
			logger.LogError("Dispose of WorkerBusRabbitMQ error:" + ex);
		}
	}

	public void RefreshConsumers()
	{
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();

		var consumerTypeFounded = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
			.Where(t => t.GetInterface(nameof(IWorkerBusConsumer)) != null && !t.IsAbstract);

		foreach (var consumerType in consumerTypeFounded)
		{
			var tempConsumer = serviceProvider.GetService(consumerType) as IWorkerBusConsumer;
			if (tempConsumer == null) throw new InvalidOperationException("Unable to construct consumer with Type=" + consumerType.AssemblyQualifiedName);

			var consumerEntityType = tempConsumer.ConsumerEntityType;

			if (!consumersEntityMatch.ContainsKey(consumerEntityType))
			{
				consumersEntityMatch.Add(consumerEntityType, consumerType);
			}
			else if (!consumersEntityMatch[consumerEntityType].Equals(tempConsumer))
			{
				throw new InvalidOperationException(
					$"Consumer with such EntityType already exists. EntityType={consumerEntityType.AssemblyQualifiedName}. " + Environment.NewLine +
					$"Existing consumer={consumersEntityMatch[consumerEntityType].AssemblyQualifiedName}. " + Environment.NewLine +
					$"Consumer trying to be added={consumerType}");
			}
		}
	}
}

public static class WorkerBusExtensions
{
	public static void AddWorkerBusConsumers(this IServiceCollection services)
	{
		var consumerTypeFounded = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
					.Where(t => t.GetInterface(nameof(IWorkerBusConsumer)) != null && !t.IsAbstract);

		foreach (var consumerType in consumerTypeFounded)
		{
			services.AddTransient(consumerType);
		}
	}
}
