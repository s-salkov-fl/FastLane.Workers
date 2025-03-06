namespace FastLane.Workers.Configuration
{
	public record BusConfiguration
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public long ResponseWaitTimeMilliseconds { get; set; }
		public string ServerId { get; set; }
		public string HostsBroadCastExchange { get; set;}
		public string HostsDirectExchange { get; set; }
		public string ServersExchangeTemplate { get; set; }
		public string QueueServerTemplate { get; set; }
		public string QueueHostTemplate { get; set; }
	}
}