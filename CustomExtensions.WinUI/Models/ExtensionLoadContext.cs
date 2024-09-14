using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace CustomExtensions.WinUI.Models;

internal class ExtensionLoadContext(string assemblyPath) : AssemblyLoadContext(true)
{
    private readonly AssemblyDependencyResolver ParentResolver = new(Assembly.GetEntryAssembly().AssertDefined().Location);
    private readonly AssemblyDependencyResolver Resolver = new(assemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var defaultAssemblyPath = ParentResolver.ResolveAssemblyToPath(assemblyName);
        if (defaultAssemblyPath != null)
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }

        var assemblyPath = Resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            Trace.WriteLine($"Loading from ${assemblyPath}");
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = ParentResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        libraryPath ??= Resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            Trace.WriteLine($"Loading (unmanaged) from ${libraryPath}");
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return nint.Zero;
    }
}
