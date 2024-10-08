using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Contracts.Services;

public interface IBackdropSelectorService
{
    BackdropType BackdropType { get; }

    public event EventHandler<BackdropType>? BackdropTypeChanged;

    Task SetRequestedBackdropTypeAsync(Window window);

    Task SetBackdropTypeAsync(BackdropType type);
}
