namespace FastLane.Workers.Configuration
{
	public record ServerConfiguration
	{
		public int PingHostsStatusPeriodMilliseconds { get; init; }
        public int PingRequestResponseTimeOutMs { get; init; }

		private bool shouldExpandEnvironment = true;
		private string _workersAssemblyFolder = "";
		public string WorkersAssemblyFolder
		{
			get
			{
				if (shouldExpandEnvironment)
				{
					_workersAssemblyFolder = Environment.ExpandEnvironmentVariables(_workersAssemblyFolder);
					Directory.CreateDirectory(_workersAssemblyFolder);
					shouldExpandEnvironment = false;
				}
				return _workersAssemblyFolder;
			}
			init => _workersAssemblyFolder = value;
		}
	}
}
