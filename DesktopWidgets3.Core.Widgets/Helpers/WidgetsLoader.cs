using System.Collections.Concurrent;
using System.Reflection;

namespace DesktopWidgets3.Core.Widgets.Helpers;

public static class WidgetsLoader
{
    private static string ClassName => typeof(WidgetsLoader).Name;

    private static readonly ConcurrentQueue<WidgetPair> Widgets = [];

    private static readonly ConcurrentQueue<string> ErrorWidgets = [];

    public static async Task<(List<WidgetPair> allWidgets, List<string> errorWidgets)> WidgetsAsync(List<WidgetMetadata> metadatas)
    {
        Widgets.Clear();
        ErrorWidgets.Clear();

        var tasks = DotNetWidgets(metadatas);

        await Task.WhenAll(tasks);

        return (Widgets.ToList(), ErrorWidgets.ToList());
    }

    private static IEnumerable<Task> DotNetWidgets(List<WidgetMetadata> metadatas)
    {
        var dotnetMetadatas = metadatas.Where(o => AllowedLanguage.IsDotNet(o.Language)).ToList();

        return metadatas.Select(async metadata =>
        {
            Assembly? assembly = null;
            IAsyncWidget? widget = null;

            try
            {
                var extensionAssembly = await ApplicationExtensionHost.Current.LoadExtensionAsync(metadata.ExecuteFilePath);

                assembly = extensionAssembly.ForeignAssembly;

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
                ErrorWidgets.Enqueue(metadata.Name);
                return;
            }

            Widgets.Enqueue(new WidgetPair { Widget = widget, Metadata = metadata });
        });
    }
}
