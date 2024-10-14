using System.Reflection;

namespace DesktopWidgets3.Core.Widgets.Helpers;

public static class WidgetsLoader
{
    private static string ClassName => typeof(WidgetsLoader).Name;

    public static (List<WidgetPair> allWidgets, List<string> errorWidgets, Dictionary<string, string> installedWidgets) Widgets(List<WidgetMetadata> metadatas, List<string> installingIds)
    {
        (var dotnetWidgets, var dotnetErrorWidgets, var dotnetInstalledWidgets) = DotNetWidgets(metadatas, installingIds);

        var widgets = dotnetWidgets;

        var errorWidgets = dotnetErrorWidgets;

        var installedWidgets = dotnetInstalledWidgets;

        return (widgets, errorWidgets, installedWidgets);
    }

    private static (List<WidgetPair> dotnetWidgets, List<string> dotnetErrorWidgets, Dictionary<string, string> dotnetInstalledWidgets) DotNetWidgets(List<WidgetMetadata> metadatas, List<string> installingIds)
    {
        var dotnetMetadatas = metadatas.Where(o => AllowedLanguage.IsDotNet(o.Language)).ToList();

        var dotnetWidgets = new List<WidgetPair>();
        var dotnetErrorWidgets = new List<string>();
        var dotnetInstalledWidgets = new Dictionary<string, string>();

        foreach (var metadata in dotnetMetadatas)
        {
            IExtensionAssembly? extensionAssembly = null;
            Assembly? assembly = null;
            IAsyncWidget? widget = null;
            var resourcesFolder = string.Empty;

            try
            {
                extensionAssembly = ApplicationExtensionHost.Current.LoadExtension(metadata.ExecuteFilePath, false, false);

                assembly = extensionAssembly.ForeignAssembly;

                var type = ApplicationExtensionHost.Current.FromAssemblyGetTypeOfInterface(assembly, typeof(IAsyncWidget));

                widget = Activator.CreateInstance(type) as IAsyncWidget;

                resourcesFolder = InstallResourceFolder(extensionAssembly);
                if (installingIds.Contains(metadata.ID))
                {
                    dotnetInstalledWidgets.TryAdd(metadata.ID, resourcesFolder);
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

            if (widget == null || resourcesFolder == string.Empty)
            {
                dotnetErrorWidgets.Add(metadata.Name);
                continue;
            }

            var assemblyName = assembly!.GetName().Name;
            if (assemblyName != null)
            {
                metadata.AssemblyName = assemblyName;
            }

            ResourceExtensions.AddExternalResource(assembly!);

            dotnetWidgets.Add(new WidgetPair { Metadata = metadata, ExtensionAssembly = extensionAssembly!, Widget = widget });
        };

        return (dotnetWidgets, dotnetErrorWidgets, dotnetInstalledWidgets);
    }

    private static string InstallResourceFolder(IExtensionAssembly extensionAssembly)
    {
        var resourceFolder = extensionAssembly.TryLoadXamlResources();

        return resourceFolder ?? string.Empty;
    }
}
