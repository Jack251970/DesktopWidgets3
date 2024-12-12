using System.Reflection;
using Serilog;

namespace DesktopWidgets3.Core.Widgets.Helpers;

public static class WidgetsLoader
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetsLoader));

    public static (List<WidgetGroupPair> allWidgets, List<string> errorWidgets, Dictionary<string, string> installedWidgets) Widgets(List<WidgetGroupMetadata> metadatas, List<string> installingIds)
    {
        (var dotnetWidgets, var dotnetErrorWidgets, var dotnetInstalledWidgets) = DotNetWidgets(metadatas, installingIds);

        var widgets = dotnetWidgets;

        var errorWidgets = dotnetErrorWidgets;

        var installedWidgets = dotnetInstalledWidgets;

        return (widgets, errorWidgets, installedWidgets);
    }

    private static (List<WidgetGroupPair> dotnetWidgets, List<string> dotnetErrorWidgets, Dictionary<string, string> dotnetInstalledWidgets) DotNetWidgets(List<WidgetGroupMetadata> metadatas, List<string> installingIds)
    {
        var dotnetMetadatas = metadatas.Where(o => AllowedLanguage.IsDotNet(o.Language)).ToList();

        var dotnetWidgets = new List<WidgetGroupPair>();
        var dotnetErrorWidgets = new List<string>();
        var dotnetInstalledWidgets = new Dictionary<string, string>();

        foreach (var metadata in dotnetMetadatas)
        {
            IExtensionAssembly? extensionAssembly = null;
            Assembly? assembly = null;
            IAsyncWidgetGroup? widget = null;
            var resourcesFolder = string.Empty;

            try
            {
                extensionAssembly = ApplicationExtensionHost.Current.LoadExtension(metadata.ExecuteFilePath, false, false);

                assembly = extensionAssembly.ForeignAssembly;

                var type = ApplicationExtensionHost.Current.FromAssemblyGetTypeOfInterface(assembly, typeof(IAsyncWidgetGroup));

                widget = Activator.CreateInstance(type) as IAsyncWidgetGroup;

                resourcesFolder = InstallResourceFolder(extensionAssembly);
                if (installingIds.Contains(metadata.ID))
                {
                    dotnetInstalledWidgets.TryAdd(metadata.ID, resourcesFolder);
                }
            }
            catch (Exception e) when (extensionAssembly == null)
            {
                _log.Error(e, $"Couldn't load extension assembly for the widget: {metadata.Name}");
            }
            catch (Exception e) when (assembly == null)
            {
                _log.Error(e, $"Couldn't load assembly for the widget: {metadata.Name}");
            }
            catch (InvalidOperationException e)
            {
                _log.Error(e, $"Can't find the required IWidget interface for the widget: <{metadata.Name}>");
            }
            catch (ReflectionTypeLoadException e)
            {
                _log.Error(e, $"The GetTypes method was unable to load assembly types for the widget: <{metadata.Name}>");
            }
            catch (Exception e)
            {
                _log.Error(e, $"The following widget has errored and can not be loaded: <{metadata.Name}>");
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

            dotnetWidgets.Add(new WidgetGroupPair { Metadata = metadata, ExtensionAssembly = extensionAssembly!, WidgetGroup = widget });
        };

        return (dotnetWidgets, dotnetErrorWidgets, dotnetInstalledWidgets);
    }

    private static string InstallResourceFolder(IExtensionAssembly extensionAssembly)
    {
        var resourceFolder = extensionAssembly.TryLoadXamlResources();

        return resourceFolder ?? string.Empty;
    }
}
