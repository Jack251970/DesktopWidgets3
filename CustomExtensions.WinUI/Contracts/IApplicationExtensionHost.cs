using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Markup;

namespace CustomExtensions.WinUI.Contracts;

public interface IApplicationExtensionHost
{
    Task<IExtensionAssembly> LoadExtensionAsync(string pathToAssembly);

    IDisposable RegisterXamlTypeMetadataProvider(IXamlMetadataProvider provider);

    Uri LocateResource(object component, [CallerFilePath] string callerFilePath = "");
}
