using System.Diagnostics;
using System.Reflection;
using Microsoft.UI.Xaml.Markup;
using ResourceManager = Windows.ApplicationModel.Resources.Core.ResourceManager;
using StorageFile = Windows.Storage.StorageFile;

namespace CustomExtensions.WinUI;

internal partial class ExtensionAssembly : IExtensionAssembly
{
	public Assembly ForeignAssembly { get; }

	private readonly ExtensionLoadContext? ExtensionContext;
	private readonly string ForeignAssemblyDir;
	private readonly string ForeignAssemblyName;
	private bool? IsHotReloadAvailable;
	private readonly DisposableCollection Disposables = new();
	private bool IsDisposed;

	internal ExtensionAssembly(string assemblyPath)
	{
		// TODO: For some reason WinUI gets very angry when loading via AssemblyLoadContext,
		// even if using AssemblyLoadContext.Default which *should* have no difference than
		// Assembly.LoadFrom(), but it does.
		//
		// ExtensionContext = new(assemblyPath);
		// ForeignAssembly = ExtensionContext.LoadFromAssemblyPath(assemblyPath);
		ForeignAssembly = Assembly.LoadFrom(assemblyPath);
		ForeignAssemblyDir = Path.GetDirectoryName(ForeignAssembly.Location.AssertDefined()).AssertDefined();
		ForeignAssemblyName = ForeignAssembly.GetName().Name.AssertDefined();
	}

	public async Task LoadAsync()
	{
		ObjectDisposedException.ThrowIf(IsDisposed, new ObjectDisposedException(nameof(ExtensionAssembly)));

        await LoadResources();
		RegisterXamlTypeMetadataProviders();
	}

	private async Task LoadResources()
	{
		FileInfo resourcePriFileInfo = new(Path.Combine(ForeignAssemblyDir, "resources.pri"));
		if (!resourcePriFileInfo.Exists)
		{
			resourcePriFileInfo = new(Path.Combine(ForeignAssemblyDir, $"{ForeignAssemblyName}.pri"));
		}

		if (!resourcePriFileInfo.Exists)
		{
			return;
		}

		var file = await StorageFile.GetFileFromPathAsync(resourcePriFileInfo.FullName);
		ResourceManager.Current.LoadPriFiles([file]);
	}

	private void RegisterXamlTypeMetadataProviders()
	{
		ObjectDisposedException.ThrowIf(IsDisposed, new ObjectDisposedException(nameof(ExtensionAssembly)));

		_ = Disposables.AddRange(ForeignAssembly.ExportedTypes
					.Where(type => type.IsAssignableTo(typeof(IXamlMetadataProvider)))
					.Select(metadataType => (Activator.CreateInstance(metadataType) as IXamlMetadataProvider).AssertDefined())
					.Select(ApplicationExtensionHost.Current.RegisterXamlTypeMetadataProvider));
	}

	public bool TryEnableHotReload()
	{
		if (IsHotReloadAvailable.HasValue)
		{
			return IsHotReloadAvailable.Value;
		}

		if (!ApplicationExtensionHost.IsHotReloadEnabled)
		{
			IsHotReloadAvailable = false;
			return false;
		}

		if (ForeignAssemblyDir == HostingProcessDir)
		{
			Trace.TraceWarning($"HotReload(Debug) : Output directory for {ForeignAssembly.FullName} appears to be in the same location as the application directory. HotReload may not function in this environment.");
			IsHotReloadAvailable = false;
			return false;
		}

		// Note: this assumes all your resources exist under the current assembly name
		// this won't be true for nested dependencies or the like, so they will need to 
		// enable the same capabilities or they may crash when using hot reload
		var assemblyResDir = Path.Combine(ForeignAssemblyDir, ForeignAssemblyName);
		if (!Directory.Exists(assemblyResDir))
		{
			Trace.TraceError($"HotReload(Debug) : Cannot enable hot reload for {ForeignAssembly.FullName} because {assemblyResDir} does not exist on the system");
			IsHotReloadAvailable = false;
			return false;
		}
		var debugTargetResDir = Path.Combine(HostingProcessDir, ForeignAssemblyName);
		DirectoryInfo debugTargetResDirInfo = new(debugTargetResDir);
		if (debugTargetResDirInfo.Exists)
		{
			if (!debugTargetResDirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
			{
				Trace.TraceError($"HotReload(Debug) : Cannot enable hot reload for {ForeignAssembly.FullName} because {debugTargetResDir} already exists as a non-symbolic linked directory");
				IsHotReloadAvailable = false;
				return false;
			}
			Directory.Delete(debugTargetResDir, recursive: true);
		}
		_ = Directory.CreateSymbolicLink(debugTargetResDir, assemblyResDir);
		IsHotReloadAvailable = true;
		return true;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				Disposables?.Dispose();
				ExtensionContext?.Unload();
			}

			IsDisposed = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
