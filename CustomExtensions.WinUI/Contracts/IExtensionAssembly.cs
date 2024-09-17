using System.Reflection;
using System.Runtime.CompilerServices;

namespace CustomExtensions.WinUI.Contracts;

public interface IExtensionAssembly : IDisposable
{
    Assembly ForeignAssembly { get; }

    void LoadResources();

    Task LoadResourcesAsync();

    (bool isHotReloadAvailable, string? targetResDir) TryEnableHotReload();

    Uri LocateResource(object component, [CallerFilePath] string callerFilePath = "");
}
