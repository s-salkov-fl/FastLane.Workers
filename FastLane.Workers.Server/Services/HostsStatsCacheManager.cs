using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using FastLane.Workers.Models;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Server.Services;

public class HostsStatsCacheManager
{
	private readonly ILogger<HostsStatsCacheManager> _logger;
	private readonly ServerConfiguration serverConfig;
	private readonly IWorkerBus publishEndpoint;
	private readonly HostsState hostsState;
	private readonly ServerStatus serverStatus;

	public HostsStatsCacheManager(ILogger<HostsStatsCacheManager> logger,
		IOptions<ServerConfiguration> serverConfig,
		HostsState hostsState,
		ServerStatus serverStatus,
		IWorkerBus publishEndpoint)
	{
		_logger = logger;
		this.serverConfig = serverConfig.Value;
		this.publishEndpoint = publishEndpoint;
		this.hostsState = hostsState;
		this.serverStatus = serverStatus;
	}

	public async Task RefreshHostsStatsCache(CancellationToken cancellationToken)
	{
		var requestId = Guid.NewGuid();
		hostsState.StartModifying();
		hostsState.Clear();

		try
		{
			await publishEndpoint.PublishFromServerToHostBroadCast(
				new HostStatusRequest
				{
					Id = requestId,
					Expiration = serverConfig.PingRequestResponseTimeOutMs.ToString()
				}, cancellationToken);

			await Task.Delay(serverConfig.PingRequestResponseTimeOutMs, cancellationToken);

			serverStatus.Issues.RemoveAll(i => i is ServerHostConflictIssue);
			Dictionary<string, List<HostStatus>> countHostIds = new Dictionary<string, List<HostStatus>>();

			foreach (var hoststat in hostsState.HostsStateCache)
			{
				List<HostStatus> statsList;
				if (!countHostIds.TryGetValue(hoststat.HostId, out statsList)) countHostIds.Add(hoststat.HostId, statsList = new List<HostStatus>());
				statsList.Add(hoststat);
			}

			foreach (var statsList in countHostIds.Values)
			{
				if (statsList.Count == 1) countHostIds.Remove(statsList[0].HostId);
			}

			if (countHostIds.Count > 0)
			{
				serverStatus.Issues.Add(new ServerHostConflictIssue { HostIdStateMatches = countHostIds });
			}
		}
		finally
		{
			hostsState.EndModifying();
		}
	}
}
