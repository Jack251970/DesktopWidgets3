namespace DesktopWidgets3.Widget;

/// <summary>
/// Asyncchoronous widget group model.
/// </summary>
public interface IWidgetGroup : IAsyncWidgetGroup
{
    /// <summary>
    /// Initialize the widget group model.
    /// </summary>
    /// <param name="context">A object that provides information and functions for the widget group.</param>
    /// <returns>The list of the widget types that the widget group contains.</returns>
    void InitWidgetGroup(WidgetInitContext widgetInitContext);

    /// <summary>
    /// Initialize the widget group model asynchrously.
    /// </summary>
    /// <param name="context">A object that provides information and functions for the widget group.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task IAsyncWidgetGroup.InitWidgetGroupAsync(WidgetInitContext widgetInitContext) => Task.Run(() => InitWidgetGroup(widgetInitContext));
}
