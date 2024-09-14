using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace CustomExtensions.WinUI;

internal class ExtensionLoadContext : AssemblyLoadContext
{
	private readonly AssemblyDependencyResolver ParentResolver;
	private readonly AssemblyDependencyResolver Resolver;

	public ExtensionLoadContext(string assemblyPath) : base(true)
	{
		ParentResolver = new AssemblyDependencyResolver(Assembly.GetEntryAssembly().AssertDefined().Location);
		Resolver = new AssemblyDependencyResolver(assemblyPath);
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		string? defaultAssemblyPath = ParentResolver.ResolveAssemblyToPath(assemblyName);
		if (defaultAssemblyPath != null)
		{
			return Default.LoadFromAssemblyName(assemblyName);
		}
		string? assemblyPath = Resolver.ResolveAssemblyToPath(assemblyName);
		if (assemblyPath != null)
		{
			Trace.WriteLine($"Loading from ${assemblyPath}");
			return LoadFromAssemblyPath(assemblyPath);
		}
		return null;
	}

	protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
	{
		string? libraryPath = ParentResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
		libraryPath ??= Resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
		if (libraryPath != null)
		{
			Trace.WriteLine($"Loading (unmanaged) from ${libraryPath}");
			return LoadUnmanagedDllFromPath(libraryPath);
		}
		return IntPtr.Zero;
	}
}
