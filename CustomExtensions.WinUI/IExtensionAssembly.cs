using System.Reflection;
using System.Runtime.CompilerServices;

namespace CustomExtensions.WinUI;

public interface IExtensionAssembly : IDisposable
{
	Assembly ForeignAssembly { get; }
	Task LoadAsync();
	bool TryEnableHotReload();
	Uri LocateResource(object component, [CallerFilePath] string callerFilePath = "");
}
