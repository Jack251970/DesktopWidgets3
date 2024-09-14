using System.Runtime.CompilerServices;

namespace CustomExtensions.WinUI;

internal partial class ExtensionAssembly
{
	public Uri LocateResource(object component, [CallerFilePath] string callerFilePath = "")
	{
		return new Uri($"ms-appx:///{LocateResourcePath(component, callerFilePath).Replace('\\', '/')}");
	}

	private string LocateResourcePath(object component, [CallerFilePath] string callerFilePath = "")
	{
		component.AssertDefined();
		if (component.GetType().Assembly != ForeignAssembly)
		{
			throw new InvalidProgramException();
		}
		var resourceName = Path.GetFileName(callerFilePath)[..^3];
		TryEnableHotReload();

		var pathParts = callerFilePath.Split('\\')[..^1];
		for (var i = pathParts.Length - 1; i > 1; i++)
		{
			var pathCandidate = Path.Join(pathParts[i..pathParts.Length].Append(resourceName).Prepend(ForeignAssemblyName).ToArray());
			FileInfo sourceResource = new(Path.Combine(ForeignAssemblyDir, pathCandidate));
			FileInfo colocatedResource = new(Path.Combine(HostingProcessDir, pathCandidate));
			if (colocatedResource.Exists)
			{
				return pathCandidate;
			}
			if (sourceResource.Exists)
			{
				return sourceResource.FullName;
			}
			throw new FileNotFoundException("Could not find resource", resourceName);
		}
		throw new FileNotFoundException("Could not find resource", resourceName);
	}
}
