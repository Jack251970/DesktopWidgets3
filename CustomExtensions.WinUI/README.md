# CustomExtensions.WinUI

This package provides the ability to load loose "extension" (or addin) assemblies which may contain WinUI components and allow them to correctly render in the hosting process. Additionally, it provides some limited support for Hot Reload, depending on how your extensions are packaged.

This package has been tested under Microsoft.WindowsAppSdk package 1.5.240802000.

Thanks for dnchattan and this project is based on his project [winui-extensions](https://github.com/dnchattan/winui-extensions)!

## How to use

### 1. Initializing the application extension host

Before any other APIs are called, you must first initialize the host application by calling `ApplicationExtensionHost.Initialize()` with your host application instance.

### 2. Loading an extension

Where you would normally loaded the extension assembly (`Assembly.LoadFrom` recommanded), you should instead call the `ApplicationExtensionHost.Current.LoadExtension` method, which will return an `IExtensionAssembly` handle that can be used to unload the extension later.

If you want to load the pri resources in the extension, you should call `ApplicationExtensionHost.Current.LoadExtensionAndResourcesAsync` method instead of `LoadExtension` method.

> Note: For some reason, AssemblyLoadContext will cause issues, such as unable to find secondary reference.
Even if using AssemblyLoadContext.Default which should have no difference thanAssembly.LoadFrom(), but it does.
So, use Assembly.LoadFrom() instead.

Then you can get the actual assembly object and create an instance of your extension if it implements a known interface IExtension:

```cs
using CustomExtensions.WinUI.Contracts;

/* ... */

IExtension? LoadMyExtensionAndCreateInstance(string assemblyLoadPath, bool loadXamlResources)
{
    // save off the handle so we can clean up our registration with the hosting process later if desired.
    // LoadExtensionAndResourcesAsync will load extension assembly and pri resources to the host process.
    // LoadExtension will only load extension assembly to the host process.
    IExtensionAssembly extensionAssembly = await ApplicationExtensionHost.Current.LoadExtensionAndResourcesAsync(assemblyLoadPath);

    // load xaml files when the extension is loading
    if (loadXamlResources)
    {
        // resourceFolder is the symbolic path to the resource folder in the host project directory.
        string? resourceFolder = extensionAssembly.TryLoadXamlResources();
    }

    // get the actual assembly object
    Assembly assembly = extensionAssembly.ForeignAssembly;

    // get the type of the extension
    Type? type = ApplicationExtensionHost.Current.FromAssemblyGetTypeOfInterface(assembly, typeof(IExtension));

    // create an instance of the extension
    IExtension? extension = Activator.CreateInstance(type) as IExtension;

    return extension;
}
```

The `IExtensionAssembly` interface also implements `IDisposable` to remove your extension's resources and Xaml type metadata registration from the hosting assembly. This will not unload the extension assembly, however.

When your application are closing, it is recommanded to dispose the extensionAssembly to remove your extension's resources and Xaml type metadata registration from the hosting assembly.

### 3. Extension UI Requirements

* Method 1: Load the Xaml files when the extension is loading. (Recommended)

When loading the extension, the extension assembly can attempt to enable hot reload and create symbolic link to the according path in the host project directory by calling the `TryEnableHotReload` method.

The codes is in the `LoadMyExtensionAndCreateInstance()` function as above.

* Method 2: Load the Xaml files every time when they are needed.

Any extension assembly must disable the generated `InitializeComponent()` method from their codebehind, and instead call the extension method:

```cs
using Microsoft.UI.Xaml.Controls;
using CustomExtensions.WinUI.Extensions;

public sealed partial class SamplePage : Page
{
    public SamplePage()
    {
        // Will attempt infer the correct path to the Xaml file based on the `CallerFilePath` attribute.
        this.LoadComponent(ref _contentLoaded);  // Don't use this.InitializeComponent(); here!
    }
}
```

### 4. Using pri resources in an extension

* Method 1: Use `Microsoft.Windows.ApplicationModel.ResourceMap` (Recommended)

Pri resources can be accessed via the `ApplicationExtensionHost.GetWinResourceMapForAssembly` method, which will return a `Microsoft.Windows.ApplicationModel.ResourceMap` for the extension's resources:

```cs
private void Loaded(object sender, RoutedEventArgs e)
{
	Microsoft.Windows.ApplicationModel.ResourceMap resources = ApplicationExtensionHost.GetWinResourceMapForAssembly();
	Greeting.Text = resources.GetValue("Greeting/Text").ValueAsString;
}
```

* Method 2: Use `Windows.ApplicationModel.Resources.Core.ResourceMap`

Pri resources can be accessed via the `ApplicationExtensionHost.GetCoreResourceMapForAssembly` method, which will return a `Windows.ApplicationModel.Resources.Core.ResourceMap` for the extension's resources:

```cs
private void Loaded(object sender, RoutedEventArgs e)
{
	Windows.ApplicationModel.Resources.Core.ResourceMap resources = ApplicationExtensionHost.GetCoreResourceMapForAssembly();
	Greeting.Text = resources.GetValue("Greeting/Text").ValueAsString;
}
```

Rememeber you have called `LoadExtensionAndResourcesAsync` function instead of `LoadExtension`.

> Note: The `x:Uid="Greeting"` pattern will **not** work for extensions to bind their resources to `FrameworkElements` in their own UI.
So, do **not** use x:Uid in the Xaml files, like this:

```xml
<UserControl
	x:Class="SampleExtension.UI.Greeter"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	Loaded="Loaded">
        <!-- Here x:Uid is unable to load resources.  -->
		<TextBlock x:Uid="Greeting" x:Name="Greeting" />
</UserControl>
```

### 5. Hot-reload

Hot-reload will function as long as your application is loading the extension directly from its output directory.

If not, it is expected that your dll has all the required resource files adjacent to it, and hot reload will likely not work. If you have any issues check the trace log for any messages regarding Hot Reload.

## How it works

There's two main things that need to be accounted for when loading extensions: registering the generated `XamlTypeInfo.g.cs` into the host process, and changing the way your Xaml components load themselves.

### 1. XamlTypeInfo

The generated `XamlTypeInfo.g.cs` file for a WinUI assembly contains all kinds of generated type and metadata mappings that the host process will need to be able to properly find things by type.

This needs to be connected to the same kind of generated code in the parent process, however since it is generated late in the build process, it's rather difficult to get a project to reference these artifacts in code.

In order to make this easier, there is an extension method which the hosting XamlApplication can call on itself to connect another assembly's type information into its own registrations by using some reflection on both the host and extension assemblies to find the correct types.

### 2. ~~InitializeComponent~~ -> LoadComponent

The generated code for your Xaml's `InitializeComponent` will look something like this:

```cs
[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
public void InitializeComponent()
{
    if (_contentLoaded)
        return;

    _contentLoaded = true;

    global::System.Uri resourceLocator = new global::System.Uri("ms-appx:///SampleExtension.SampleAppExtension/UI/SamplePage.xaml");
    global::Microsoft.UI.Xaml.Application.LoadComponent(this, resourceLocator, global::Microsoft.UI.Xaml.Controls.Primitives.ComponentResourceLocation.Nested);
}
```

This won't work if your extension isn't placed side-by-side with your application resources (usually you want to put them in their own isolated directories so they can be easily added/removed), because the `ms:appx///` path derives from the host application directory.

The resource loader will accept absolute paths from a drive root (e.g. `ms-appx://c:/MyApp/Extensions/Foo/FooPage.xaml`).

Fortunately, Xaml is pretty consistent in how it gets packaged, so the `LoadComponent` extension method can fill the place of `InitializeComponent`, and will infer the correct information based on reflection.

It will also re-use the generated `_contentLoaded` variable, which it accepts as a `ref`.
