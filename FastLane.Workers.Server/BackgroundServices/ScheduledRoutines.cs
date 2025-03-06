using Microsoft.Extensions.Options;
using FastLane.Workers.Configuration;
using FastLane.Workers.Server.Services;

namespace FastLane.Workers.Server.BackgroundServices
{
	public class ScheduledRoutines : BackgroundService
	{
		private readonly ILogger<ScheduledRoutines> _logger;
		private readonly ServerConfiguration serverConfig;
		private readonly HostsStatsCacheManager hostsStatsCacheManager;

		public ScheduledRoutines(ILogger<ScheduledRoutines> logger,
			IOptions<ServerConfiguration> serverConfig, 
			HostsStatsCacheManager hostsStatsCacheManager)
		{
			_logger = logger;
			this.serverConfig = serverConfig.Value;
			this.hostsStatsCacheManager = hostsStatsCacheManager;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await hostsStatsCacheManager.RefreshHostsStatsCache(stoppingToken);

				await Task.Delay(serverConfig.PingHostsStatusPeriodMilliseconds, stoppingToken);
			}

		}
	}
}
