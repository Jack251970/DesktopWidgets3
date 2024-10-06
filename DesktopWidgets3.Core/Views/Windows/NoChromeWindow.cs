using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
using WinUIEx;

namespace DesktopWidgets3.Core.Views.Windows;

/// <summary>
/// A full screen window that shows with no chrome, with dialog theme support.
/// Codes are edited from: https://github.com/dotMorten/WinUIEx.
/// </summary>
public class NoChromeWindow : Window
{
    private readonly WindowManager _manager;

    private readonly IThemeSelectorService _themeSelectorService = DependencyExtensions.GetRequiredService<IThemeSelectorService>();

    public NoChromeWindow()
    {
        Activated += SplashScreen_Activated;
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, Activate);
        _manager = WindowManager.Get(this);
    }

    private void Content_Loaded(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement f)
        {
            if (double.IsNaN(Width) && f.DesiredSize.Width > 0)
            {
                DesiredWidth = f.DesiredSize.Width;
            }

            if (double.IsNaN(Height) && f.DesiredSize.Height > 0)
            {
                DesiredHeight = f.DesiredSize.Height;
            }
        }
    }

    private void SplashScreen_Activated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= SplashScreen_Activated;
        this.Hide(); // Hides at the first time
        var hwnd = this.GetWindowHandle();
        HwndExtensions.ToggleWindowStyle(hwnd, false, WindowStyle.TiledWindow);
        if (Content is not FrameworkElement content || content.IsLoaded)
        {
            Content_Loaded(this, new RoutedEventArgs());
        }
        else
        {
            content.Loaded += Content_Loaded;
        }
    }

    /// <summary>
    /// Shows a message dialog
    /// </summary>
    /// <param name="content">The message displayed to the user.</param>
    /// <param name="title">The title to display on the dialog, if any.</param>
    /// <returns>An object that represents the asynchronous operation.</returns>
    public Task ShowMessageDialogAsync(string content, string title = "") => ShowMessageDialogAsync(content, null, title: title);

    /// <summary>
    /// Shows a message dialog
    /// </summary>
    /// <param name="content">The message displayed to the user.</param>
    /// <param name="commands">an array of commands that appear in the command bar of the message dialog. These commands makes the dialog actionable.</param>
    /// <param name="defaultCommandIndex">The index of the command you want to use as the default. This is the command that fires by default when users press the ENTER key.</param>
    /// <param name="cancelCommandIndex">The index of the command you want to use as the cancel command. This is the command that fires when users press the ESC key.</param>
    /// <param name="title">The title to display on the dialog, if any.</param>
    /// <returns>An object that represents the asynchronous operation.</returns>
    public async Task<IUICommand> ShowMessageDialogAsync(string content, IList<IUICommand>? commands, uint defaultCommandIndex = 0, uint cancelCommandIndex = 1, string title = "")
    {
        if (commands != null && commands.Count > 3)
        {
            throw new InvalidOperationException("A maximum of 3 commands can be specified");
        }

        IUICommand defaultCommand = new UICommand("OK");
        IUICommand? secondaryCommand = null;
        IUICommand? cancelCommand = null;
        if (commands != null)
        {
            defaultCommand = commands.Count > defaultCommandIndex ? commands[(int)defaultCommandIndex] : commands.FirstOrDefault() ?? defaultCommand;
            cancelCommand = commands.Count > cancelCommandIndex ? commands[(int)cancelCommandIndex] : null;
            secondaryCommand = commands.Where(c => c != defaultCommand && c != cancelCommand).FirstOrDefault();
        }
        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = _themeSelectorService.Theme,
            Content = new TextBlock() { Text = content, TextWrapping = TextWrapping.Wrap },
            Title = title,
            PrimaryButtonText = defaultCommand.Label
        };
        if (secondaryCommand != null)
        {
            dialog.SecondaryButtonText = secondaryCommand.Label;
        }
        if (cancelCommand != null)
        {
            dialog.CloseButtonText = cancelCommand.Label;
        }
        var dialogTask = dialog.ShowAsync(ContentDialogPlacement.InPlace);
        BringToFront();
        var result = await dialogTask;
        return result switch
        {
            ContentDialogResult.Primary => defaultCommand,
            ContentDialogResult.Secondary => secondaryCommand!,
            _ => cancelCommand ?? new UICommand(),
        };
    }

    /// <summary>
    /// Gets a reference to the AppWindow for the app
    /// </summary>
    public new AppWindow AppWindow => base.AppWindow; // Kept here for binary compatibility

    /// <summary>
    /// Brings the window to the front
    /// </summary>
    /// <returns></returns>
    public bool BringToFront() => WindowExtensions.SetForegroundWindow(this);

    private Icon? _TaskBarIcon;

    /// <summary>
    /// Gets or sets the task bar icon.
    /// </summary>
    public Icon? TaskBarIcon
    {
        get => _TaskBarIcon;
        set
        {
            _TaskBarIcon = value;
            this.SetTaskBarIcon(value);
        }
    }

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public new string Title // Old Workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/3689. Needs to stay for binary compat
    {
        get => base.Title;
        set => base.Title = value;
    }

    /// <summary>
    /// Gets or sets a unique ID used for saving and restoring window size and position
    /// across sessions.
    /// </summary>
    /// <remarks>
    /// The ID must be set before the window activates. The window size and position
    /// will only be restored if the monitor layout hasn't changed between application settings.
    /// The property uses ApplicationData storage, and therefore is currently only functional for
    /// packaged applications.
    /// </remarks>
    public string? PersistenceId
    {
        get => _manager.PersistenceId;
        set => _manager.PersistenceId = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this window is shown in task switchers.
    /// </summary>
    public bool IsShownInSwitchers
    {
        get => _manager.AppWindow.IsShownInSwitchers;
        set => _manager.AppWindow.IsShownInSwitchers = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this window is always on top.
    /// </summary>
    public bool IsAlwaysOnTop
    {
        get => _manager.IsAlwaysOnTop;
        set => _manager.IsAlwaysOnTop = value;
    }

    /// <summary>
    /// Gets or sets the width of the window
    /// </summary>
    public double Width
    {
        get => _manager.Width;
        set => _manager.Width = value;
    }

    /// <summary>
    /// Gets or sets the height of the window
    /// </summary>
    public double Height
    {
        get => _manager.Height;
        set => _manager.Height = value;
    }

    /// <summary>
    /// Gets the desired width of the window. NaN if not set.
    /// </summary>
    public double DesiredWidth { get; private set; } = double.NaN;

    /// <summary>
    /// Gets the desired height of the window. NaN if not set.
    /// </summary>
    public double DesiredHeight { get; private set; } = double.NaN;
}
