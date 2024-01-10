using Files.Core.Services;

namespace DesktopWidgets3.Services;

internal class DependencyService : IDependencyService
{
    public T GetService<T>() where T : class
    {
        return App.GetService<T>();
    }
}
