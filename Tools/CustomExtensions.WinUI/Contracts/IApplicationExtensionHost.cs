using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Markup;

namespace CustomExtensions.WinUI.Contracts;

public interface IApplicationExtensionHost
{
    IExtensionAssembly LoadExtension(string pathToAssembly);

    Task<IExtensionAssembly> LoadExtensionAndResourcesAsync(string pathToAssembly);

    Type FromAssemblyGetTypeOfInterface(Assembly assembly, Type type);

    IDisposable RegisterXamlTypeMetadataProvider(IXamlMetadataProvider provider);

    Uri LocateResource(object component, [CallerFilePath] string callerFilePath = "");
}
