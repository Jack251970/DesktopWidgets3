namespace DesktopWidgets3.Core.Contracts.Services;

public interface IDependencyService
{
    T GetService<T>() where T : class;
}
