using System.Collections.Concurrent;
using System.Reflection;

namespace DesktopWidgets3.Core.Widgets.Helpers;

public static class WidgetsLoader
{
    private static string ClassName => typeof(WidgetsLoader).Name;

    private static readonly ConcurrentQueue<WidgetPair> Widgets = [];

    private static readonly ConcurrentQueue<string> ErrorWidgets = [];

    private static readonly ConcurrentDictionary<string, string> InstalledWidgets = [];

    private static readonly ConcurrentQueue<IExtensionAssembly> ExtensionAssemblies = [];

    public static async Task<(List<WidgetPair> allWidgets, List<string> errorWidgets, Dictionary<string, string> installedWidgets)> WidgetsAsync(List<WidgetMetadata> metadatas, List<string> installingIds)
    {
        Widgets.Clear();
        ErrorWidgets.Clear();

        var tasks = DotNetWidgets(metadatas, installingIds);

        await Task.WhenAll(tasks);

        return (Widgets.ToList(), ErrorWidgets.ToList(), InstalledWidgets.ToDictionary());
    }

    public static void DisposeExtensionAssemblies()
    {
        foreach (var extensionAssembly in ExtensionAssemblies)
        {
            extensionAssembly?.Dispose();
        }
    }

    private static IEnumerable<Task> DotNetWidgets(List<WidgetMetadata> metadatas, List<string> installingIds)
    {
        var dotnetMetadatas = metadatas.Where(o => AllowedLanguage.IsDotNet(o.Language)).ToList();

        return metadatas.Select(async metadata =>
        {
            IExtensionAssembly? extensionAssembly = null;
            Assembly? assembly = null;
            IAsyncWidget? widget = null;

            try
            {
                extensionAssembly = await ApplicationExtensionHost.Current.LoadExtensionAsync(metadata.ExecuteFilePath);

                assembly = extensionAssembly.ForeignAssembly;

                var type = ApplicationExtensionHost.Current.FromAssemblyGetTypeOfInterface(assembly, typeof(IAsyncWidget));

                widget = Activator.CreateInstance(type) as IAsyncWidget;

                var resourcesFolder = InstallResourceFolder(extensionAssembly);
                if (installingIds.Contains(metadata.ID))
                {
                    InstalledWidgets.TryAdd(metadata.ID, resourcesFolder);
                }
            }
            catch (Exception e) when (extensionAssembly == null)
            {
                LogExtensions.LogError(ClassName, e, $"Couldn't load extension assembly for the widget: {metadata.Name}");
            }
            catch (Exception e) when (assembly == null)
            {
                LogExtensions.LogError(ClassName, e, $"Couldn't load assembly for the widget: {metadata.Name}");
            }
            catch (InvalidOperationException e)
            {
                LogExtensions.LogError(ClassName, e, $"Can't find the required IWidget interface for the widget: <{metadata.Name}>");
            }
            catch (ReflectionTypeLoadException e)
            {
                LogExtensions.LogError(ClassName, e, $"The GetTypes method was unable to load assembly types for the widget: <{metadata.Name}>");
            }
            catch (Exception e)
            {
                LogExtensions.LogError(ClassName, e, $"The following widget has errored and can not be loaded: <{metadata.Name}>");
            }

            if (widget == null)
            {
                ErrorWidgets.Enqueue(metadata.Name);
                return;
            }

            var assemblyName = assembly!.GetName().Name;
            if (assemblyName != null)
            {
                metadata.AssemblyName = assemblyName;
            }

            ResourceExtensions.AddExternalResource(assembly!);

            ExtensionAssemblies.Enqueue(extensionAssembly!);

            Widgets.Enqueue(new WidgetPair { Widget = widget, Metadata = metadata });
        });
    }

    private static string InstallResourceFolder(IExtensionAssembly extensionAssembly)
    {
        (var hotReloadAvailable, var resourceFolder) = extensionAssembly.TryEnableHotReload();

        return hotReloadAvailable == true ? resourceFolder! : string.Empty;
    }
}
