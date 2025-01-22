namespace DesktopWidgets3.Widget;

/// <summary>
/// The widget metadata model.
/// </summary>
public class WidgetGroupMetadata
{
    public string ID { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IcoPath { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string ExecuteFileName { get; set; } = string.Empty;

    public List<WidgetMetaData> Widgets { get; set; } = [];

    public bool Disabled { get; set; } = false;

    public bool Preinstalled { get; set; } = false;

    public bool Installed { get; set; } = true;

    public string ExecuteFilePath { get; private set; } = string.Empty;

    public string AssemblyName { get; set; } = string.Empty;

    private string _widgetDirectory = string.Empty;
    public string WidgetDirectory
    {
        get => _widgetDirectory;
        set
        {
            _widgetDirectory = value;
            ExecuteFilePath = Path.Combine(value, ExecuteFileName);
            if (!string.IsNullOrEmpty(IcoPath))
            {
                IcoPath = Path.Combine(value, IcoPath);
            }
            AssemblyName = Path.GetFileNameWithoutExtension(ExecuteFileName);
            foreach (var widget in Widgets)
            {
                widget.WidgetDirectory = value;
            }
        }
    }

    public List<string> WidgetTypes { get; set; } = null!;

    public override string ToString()
    {
        return Name;
    }
}

public class WidgetMetaData
{
    public string Type { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IcoPath { get; set; } = string.Empty;

    public string IcoPathDark { get; set; } = string.Empty;

    public float DefaultHeight { get; set; } = 342;

    public float DefaultWidth { get; set; } = 201;

    public float? MinHeight { get; set; }

    public float? MinWidth { get; set; }

    public float? MaxHeight { get; set; }

    public float? MaxWidth { get; set; }

    public string ScreenshotPath { get; set; } = string.Empty;

    public string ScreenshotPathDark { get; set; } = string.Empty;

    public bool AllowMultiple { get; set; } = false;

    private string _widgetDirectory = string.Empty;
    public string WidgetDirectory
    {
        get => _widgetDirectory;
        set
        {
            _widgetDirectory = value;
            if (!string.IsNullOrEmpty(IcoPath))
            {
                IcoPath = Path.Combine(value, IcoPath);
            }
            if (!string.IsNullOrEmpty(IcoPathDark))
            {
                IcoPathDark = Path.Combine(value, IcoPathDark);
            }
            if (!string.IsNullOrEmpty(ScreenshotPath))
            {
                ScreenshotPath = Path.Combine(value, ScreenshotPath);
            }
            if (!string.IsNullOrEmpty(ScreenshotPathDark))
            {
                ScreenshotPathDark = Path.Combine(value, ScreenshotPathDark);
            }
        }
    }

    public override string ToString()
    {
        return Name;
    }
}
