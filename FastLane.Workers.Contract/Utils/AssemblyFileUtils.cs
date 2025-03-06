
using System.IO.Compression;

namespace FastLane.Workers.Contract.Utils;

public static class AssemblyFileUtils
{
	public static bool AssemblyDistributiveExists(string assemblyRootLocation, string assemblyName)
	{
		return Directory.Exists(GetFolderName(assemblyRootLocation, assemblyName));
	}
	public static bool AssemblyPackedExists(string assemblyRootLocation, string assemblyName)
	{
		return File.Exists(GetZipName(assemblyRootLocation, assemblyName));
	}

	public static void CompressAssemblyFolder(string assemblyRootLocation, string assemblyName)
	{
		ZipFile.CreateFromDirectory(GetFolderName(assemblyRootLocation, assemblyName), GetZipName(assemblyRootLocation, assemblyName));
	}

	public static void SaveAssemblyZip(byte[] contents, string assemblyRootLocation, string assemblyName)
	{
		File.WriteAllBytes(GetZipName(assemblyRootLocation, assemblyName), contents);
	}

	public static void DecompressAssemblyFolder(string assemblyRootLocation, string assemblyName)
	{
		ZipFile.ExtractToDirectory(GetZipName(assemblyRootLocation, assemblyName), GetFolderName(assemblyRootLocation, assemblyName));
	}

	public static string GetZipName(string assemblyRootLocation, string assemblyName) => Path.Combine(assemblyRootLocation, assemblyName) + ".zip";
	public static string GetFolderName(string assemblyRootLocation, string assemblyName) => Path.Combine(assemblyRootLocation, assemblyName);
}
