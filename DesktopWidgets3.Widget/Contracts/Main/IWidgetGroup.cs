namespace DesktopWidgets3.Widget;

/// <summary>
/// Asyncchoronous widget group model.
/// </summary>
public interface IWidgetGroup : IAsyncWidgetGroup
{
    /// <summary>
    /// Initialize the widget group model.
    /// </summary>
    /// <param name="widgetInitContext">Context to provide information and functions for the widget group.</param>
    void InitWidgetGroup(IWidgetInitContext widgetInitContext);

    /// <summary>
    /// Initialize the widget group model asynchrously.
    /// </summary>
    /// <param name="context">A object that provides information and functions for the widget group.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task IAsyncWidgetGroup.InitWidgetGroupAsync(IWidgetInitContext widgetInitContext) => Task.Run(() => InitWidgetGroup(widgetInitContext));
}
