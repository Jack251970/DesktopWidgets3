using System.Reflection;
using System.Runtime.Loader;

namespace DesktopWidgets3.Core.Widgets.Helpers;

// TODO: Integrate this class into the ExtensionAssembly in the CustomExtensions.WinUI project
public class WidgetAssemblyLoader : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver dependencyResolver;

    private readonly AssemblyName assemblyName;

    internal WidgetAssemblyLoader(string assemblyFilePath)
    {
        dependencyResolver = new AssemblyDependencyResolver(assemblyFilePath);
        assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(assemblyFilePath));
    }

    internal Assembly LoadAssemblyAndDependencies()
    {
        return LoadFromAssemblyName(assemblyName);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = dependencyResolver.ResolveAssemblyToPath(assemblyName);

        // When resolving dependencies, ignore assembly depenedencies that already exits with core assembly
        // Otherwise duplicate assembly will be loaded and some weird behavior will occur, such as WinRT.Runtime.dll
        // will fail due to loading multiple versions in process, each with their own static instance of registration state
        var existAssembly = Default.Assemblies.FirstOrDefault(x => x.FullName == assemblyName.FullName);

        return existAssembly ?? (assemblyPath == null ? null : LoadFromAssemblyPath(assemblyPath));
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = dependencyResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (!string.IsNullOrEmpty(path))
        {
            return LoadUnmanagedDllFromPath(path);
        }

        return IntPtr.Zero;
    }

    internal static Type FromAssemblyGetTypeOfInterface(Assembly assembly, Type type)
    {
        var allTypes = assembly.ExportedTypes;
        return allTypes.First(o => o.IsClass && !o.IsAbstract && o.GetInterfaces().Any(t => t == type));
    }
}
