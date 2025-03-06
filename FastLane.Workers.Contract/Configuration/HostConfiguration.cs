using System.Diagnostics;

namespace FastLane.Workers.Configuration
{
	public record HostConfiguration
	{
		public string HostId { get; init; }
		public string HostDnsName { get; init; }
		public string HostProcessId { get; init; }
		public string HostStartDate { get; init; }

		private bool shouldExpandEnvironment = true;
		private string _workersAssemblyFolder = "";
		public string WorkersAssemblyFolder {
			get {
				if (shouldExpandEnvironment) {
					_workersAssemblyFolder = Environment.ExpandEnvironmentVariables(_workersAssemblyFolder);
					Directory.CreateDirectory(_workersAssemblyFolder);
					shouldExpandEnvironment = false;
				}
				return _workersAssemblyFolder;
			}
			init => _workersAssemblyFolder = value; 
		}
		public bool StartFailIfQueueAlreadyInUse { get; init; }

		public HostConfiguration()
		{
			HostStartDate = DateTimeOffset.Now.ToString();
			HostDnsName = System.Net.Dns.GetHostName();
			HostProcessId = Process.GetCurrentProcess().Id.ToString();
		}
	}
}