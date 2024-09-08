namespace DesktopWidgets3.Widget.Models.Main;

public class WidgetMetadata
{
    public string Name { get; set; } = string.Empty;

    public string ExecuteFileName { get; set; } = string.Empty;

    public string ExecuteFilePath { get; private set; } = string.Empty;

    private string _widgetDirectory = string.Empty;
    public string WidgetDirectory
    {
        get => _widgetDirectory;
        internal set
        {
            _widgetDirectory = value;
            ExecuteFilePath = Path.Combine(value, ExecuteFileName);
            IcoPath = Path.Combine(value, IcoPath);
        }
    }

    public string IcoPath { get; set; } = string.Empty;

    public override string ToString()
    {
        return Name;
    }
}
