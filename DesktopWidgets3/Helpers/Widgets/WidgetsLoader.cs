using System.Reflection;
using System.Windows.Forms;

namespace DesktopWidgets3.Helpers.Widgets;

internal static class WidgetsLoader
{
    private static string ClassName => typeof(WidgetsLoader).Name;

    public static List<WidgetPair> Widgets(List<WidgetMetadata> metadatas)
    {
        var dotnetWidgets = DotNetWidgets(metadatas);

        var widgets = dotnetWidgets;
        return widgets;
    }

    private static List<WidgetPair> DotNetWidgets(List<WidgetMetadata> source)
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
                LogExtensions.LogError(ClassName, e, $"|WidgetsLoader.DotNetWidgets|Couldn't load assembly for the widget: {metadata.Name}");
            }
            catch (InvalidOperationException e)
            {
                LogExtensions.LogError(ClassName, e, $"|WidgetsLoader.DotNetWidgets|Can't find the required IWidget interface for the widget: <{metadata.Name}>");
            }
            catch (ReflectionTypeLoadException e)
            {
                LogExtensions.LogError(ClassName, e, $"|WidgetsLoader.DotNetWidgets|The GetTypes method was unable to load assembly types for the widget: <{metadata.Name}>");
            }
            catch (Exception e)
            {
                LogExtensions.LogError(ClassName, e, $"|WidgetsLoader.DotNetWidgets|The following widget has errored and can not be loaded: <{metadata.Name}>");
            }

            if (widget == null)
            {
                erroredWidgets.Add(metadata.Name);
                continue;
            }

            widgets.Add(new WidgetPair { Widget = widget, Metadata = metadata });
        }

        if (erroredWidgets.Count > 0)
        {
            var errorWidgetString = string.Join(Environment.NewLine, erroredWidgets);

            var errorMessage = "The following "
                               + (erroredWidgets.Count > 1 ? "widgets have " : "widget has ")
                               + "errored and cannot be loaded:";

            _ = Task.Run(() =>
            {
                MessageBox.Show($"{errorMessage}{Environment.NewLine}{Environment.NewLine}" +
                                $"{errorWidgetString}{Environment.NewLine}{Environment.NewLine}" +
                                $"Please refer to the logs for more information", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            });
        }

        return widgets;
    }
}
