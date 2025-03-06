using FastLane.Workers.Contract.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace FastLane.Workers.Bucket.Services
{
	public class WorkersAssemblyLoadContext : AssemblyLoadContext
	{
		private readonly AssemblyDependencyResolver _resolver;
		private readonly string workersAssemblyRootPath;
		private readonly ILogger<WorkersAssemblyLoadContext> logger;

		public WorkersAssemblyLoadContext(
			ILogger<WorkersAssemblyLoadContext> logger,
			string workersAssemblyRootPath)
		{
			_resolver = new AssemblyDependencyResolver(workersAssemblyRootPath);

			this.workersAssemblyRootPath = workersAssemblyRootPath;
			this.logger = logger;
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			var defaultDep = Default.Assemblies.FirstOrDefault(a=>a.FullName == assemblyName.FullName);
			if (defaultDep != null) return defaultDep;

			if (assemblyName == null) throw	new ArgumentNullException(nameof(assemblyName));
			string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

			if (string.IsNullOrEmpty(assemblyPath)) throw new FileLoadException($"Unable to find path to assembly:{assemblyName.FullName}");
			
			return LoadFromAssemblyPath(assemblyPath);
		}

		protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
		{
			string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

			if (string.IsNullOrEmpty(libraryPath)) throw new FileLoadException($"Unable to find path to unmanaged dll:{unmanagedDllName}");

			return LoadUnmanagedDllFromPath(libraryPath);
		}

		public Assembly LoadWorker(string workerTypeName)
		{
			string workerLocation = Path.Combine(workersAssemblyRootPath, GetWorkerRelativePath(workerTypeName), GetWorkerPackageName(workerTypeName));

			if (!File.Exists(workerLocation)) 
				throw new WorkerAssemblyNotFoundException("Unable to find assembly " + workerLocation) 
				{ 
					AssemblyLocation = GetWorkerRelativePath(workerTypeName),
					AssemblyName = GetWorkerPackageName(workerTypeName),
					WorkerType = workerTypeName
				};

			try
			{
				return LoadFromAssemblyPath(workerLocation);
				//return LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(workerLocation)));
			}
			catch (Exception ex)
			{
				logger.LogError($"Unable load assembly [workerTypeName] : {Environment.NewLine}{ex}");
				throw;
			}
		}

		public string GetWorkerTypeNameOnly(string workerTypeName)
		{
			if (string.IsNullOrEmpty(workerTypeName)) throw new ArgumentNullException(nameof(workerTypeName));
			var workerTypeParts = workerTypeName.Split(',');
			if (workerTypeParts.Length == 0) throw new ArgumentNullException(nameof(workerTypeName) + ": Invalid format for assembly qualified name [" + workerTypeName + "]");

			var workerName = workerTypeParts[0].Trim();

			return workerName;
		}

		public string GetWorkerPackageName(string workerTypeName)
		{
			if (string.IsNullOrEmpty(workerTypeName)) throw new ArgumentNullException(nameof(workerTypeName));
			var workerTypeParts = workerTypeName.Split(',');
			if (workerTypeParts.Length < 2) throw new ArgumentNullException(nameof(workerTypeName) + ": Invalid format for assembly qualified name [" + workerTypeName + "]");

			var workerName = workerTypeParts[1].Trim();

			return workerName + ".dll";
		}

		public string GetWorkerRelativePath(string workerTypeName)
		{
			if (string.IsNullOrEmpty(workerTypeName)) throw new ArgumentNullException(nameof(workerTypeName));
			var workerTypeParts = workerTypeName.Split(',');
			if (workerTypeParts.Length < 2) throw new ArgumentNullException(nameof(workerTypeName) + ": Invalid format for assembly qualified name [" + workerTypeName + "]");

			var workerName = workerTypeParts[1].Trim();
			if (workerTypeParts.Length > 2)
			{
				var workerVersion = workerTypeParts[2].ToUpper().Replace(" ", "").Replace(".", "_");
				if (workerVersion.StartsWith("VERSION")) workerVersion = workerVersion.Replace("VERSION=", "");
				workerName += "_v" + workerVersion;
			}

			return workerName;
		}
	}
}
