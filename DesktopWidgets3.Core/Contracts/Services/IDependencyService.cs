namespace Files.Core.Services;

public interface IDependencyService
{
    T GetService<T>() where T : class;
}
