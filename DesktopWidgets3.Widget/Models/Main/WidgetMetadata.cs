namespace DesktopWidgets3.Widget.Models.Main;

public class WidgetMetadata
{
    public string ID { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string ExecuteFileName { get; set; } = string.Empty;

    public string IcoPath { get; set; } = string.Empty;

    // TODO: Set default height & width here and check if they are legal.

    public float DefaultHeight { get; set; }

    public float DefaultWidth { get; set; }

    public float MinHeight { get; set; }

    public float MinWidth { get; set; }

    public float MaxHeight { get; set; }

    public float MaxWidth { get; set; }

    public bool InNewThread { get; set; } = false;

    public bool Disabled { get; set; } = false;

    public string ExecuteFilePath { get; private set; } = string.Empty;

    private string _widgetDirectory = string.Empty;
    public string WidgetDirectory
    {
        get => _widgetDirectory;
        set
        {
            _widgetDirectory = value;
            ExecuteFilePath = Path.Combine(value, ExecuteFileName);
            IcoPath = Path.Combine(value, IcoPath);
        }
    }

    public override string ToString()
    {
        return Name;
    }
}
