namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(IAppSettingsService appSettingsService) : IWidgetResourceService
{
    private readonly IAppSettingsService _appSettingsService = appSettingsService;

    private readonly List<IWidget> Widgets = [];

    public void Initalize()
    {

    }

    public string GetWidgetLabel(string widgetId)
    {
        return "Clock";
    }

    public string GetWidgetIconSource(string widgetId)
    {
        return "ms-appx:///Assets/Icons/Clock.png";
    }

    public static RectSize GetDefaultSize(string widgetId)
    {
        return new RectSize(240, 240);
    }

    public RectSize GetMinSize(string widgetId)
    {
        return new RectSize(240, 240);
    }

    public static BaseWidgetSettings GetDefaultSettings(string widgetId)
    {
        return new BaseWidgetSettings();
    }

    public bool GetWidgetInNewThread(string widgetId)
    {
        if (!_appSettingsService.MultiThread)
        {
            return false;
        }

        return true;
    }
}
