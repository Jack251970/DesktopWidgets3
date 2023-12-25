namespace DesktopWidgets3.Helpers;

public class SelectFolderDialogHelper
{
    public static async Task<string> PickSingleFolderDialog()
    {
        // This function was changed to use the shell32 API to open folder dialog
        // as the old one (PickSingleFolderAsync) can't work when the process is elevated
        // TODO: go back PickSingleFolderAsync when it's fixed
        var hwnd = App.MainWindow.GetWindowHandle();
        var r = await Task.FromResult<string>(ShellGetFolder.GetFolderDialog(hwnd)!);
        return r;
    }
}
