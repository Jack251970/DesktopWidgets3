using System.Reflection;

namespace DesktopWidgets3.Core.Widgets.Helpers;

public static class WidgetsLoader
{
    private static string ClassName => typeof(WidgetsLoader).Name;

    public static (List<WidgetPair> allWidgets, List<string> errorWidgets) Widgets(List<WidgetMetadata> metadatas)
    {
        (var dotnetWidgets, var errorDotNetWidgets) = DotNetWidgets(metadatas);

        var allWidgets = dotnetWidgets;
        var errorWidgets = errorDotNetWidgets;

        return (allWidgets, errorWidgets);
    }

    private static (List<WidgetPair> dotNetWidgets, List<string> errorDotNetWidgets) DotNetWidgets(List<WidgetMetadata> source)
    {
        var erroredWidgets = new List<string>();

        var widgets = new List<WidgetPair>();
        var metadatas = source.Where(o => AllowedLanguage.IsDotNet(o.Language));

        foreach (var metadata in metadatas)
        {
            Assembly? assembly = null;
            IAsyncWidget? widget = null;

            try
            {
                var assemblyLoader = new WidgetAssemblyLoader(metadata.ExecuteFilePath);
                assembly = assemblyLoader.LoadAssemblyAndDependencies();

                var type = WidgetAssemblyLoader.FromAssemblyGetTypeOfInterface(assembly,
                    typeof(IAsyncWidget));

                widget = Activator.CreateInstance(type) as IAsyncWidget;
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
                erroredWidgets.Add(metadata.Name);
                continue;
            }

            widgets.Add(new WidgetPair { Widget = widget, Metadata = metadata });
        }

        return (widgets, erroredWidgets);
    }
}
