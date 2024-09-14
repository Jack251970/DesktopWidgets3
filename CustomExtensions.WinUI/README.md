# winui-extensions

This package provides the ability to load loose "extension" (or addin) assemblies which may contain WinUI components and allow them to correctly render in the hosting process. Additionally, it provides some limited support for Hot Reload, depending on how your extensions are packaged.

## Initializing the application for extension usage

Before any other APIs are called, you must first initialize the host application by calling `ApplicationExtensionHost.Initialize()` with your host application instance.


## Loading an extension

Where you would normally loaded the extension assembly (such as via `Assembly.LoadFrom`), you should instead call the `ApplicationExtensionHost.Current.LoadExtensionAsync` method, which will return an `IExtensionAssembly` handle that can be used to unload the extension later.

```cs
using CustomExtensions.WinUI;

/* ... */

void LoadMyExtension(string assemblyLoadPath)
{
    // save off the handle so we can clean up our registration with the hosting process later if desired.
    IExtensionAssembly asmHandle = await ApplicationExtensionHost.Current.LoadExtensionAsync(assemblyLoadPath);
}
```

The `IExtensionAssembly` interface also implements `IDisposable` to remove your extension's resources and Xaml type metadata registration from the hosting assembly. This will not unload the extension assembly, however.

## Extension UI Requirements

Any extension assembly must disable the generated `InitializeComponent()` method from their codebehind, and instead call the extension method:

```cs
using Microsoft.UI.Xaml.Controls;
using CustomExtensions.WinUI;

namespace SampleExtension.UI;

public sealed partial class SamplePage : Page
{
    public SamplePage()
    {
        // this.InitializeComponent();
        // Will attempt infer the correct path to the Xaml file based on the `CallerFilePath` attribute.
        this.LoadComponent(ref _contentLoaded);
    }
}
```

## Using resources in an extension

The `x:Uid="Greeting"` pattern will not work for extensions to bind their resources to `FrameworkElements` in their own UI. They can however be accessed via the `ApplicationExtensionHost.GetResourceMapForAssembly` method, which will return a `ResourceMap` for the extension's resources:

```xml
<UserControl
	x:Class="SampleExtension.UI.Greeter"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	Loaded="Loaded">
		<TextBlock x:Uid="Greeting" x:Name="Greeting" />
</UserControl>
```

```cs
private void Loaded(object sender, RoutedEventArgs e)
{
	ResourceMap resources = ApplicationExtensionHost.GetResourceMapForAssembly();
	Greeting.Text = resources.GetValue("Greeting/Text").ValueAsString;
}
```


## Hot-reload

Hot-reload will function as long as your application is loading the extension directly from its output directory. If not, it is expected that your dll has all the required resource files adjacent to it, and hot reload will likely not work. If you have any issues check the trace log for any messages regarding Hot Reload.

# How it works

There's two main things that need to be accounted for when loading extensions: registering the generated `XamlTypeInfo.g.cs` into the host process, and changing the way your Xaml components load themselves.

## XamlTypeInfo

The generated `XamlTypeInfo.g.cs` file for a WinUI assembly contains all kinds of generated type and metadata mappings that the host process will need to be able to properly find things by type. This needs to be connected to the same kind of generated code in the parent process, however since it is generated late in the build process, it's rather difficult to get a project to reference these artifacts in code.

In order to make this easier, there is an extension method which the hosting XamlApplication can call on itself to connect another assembly's type information into its own registrations by using some reflection on both the host and extension assemblies to find the correct types.

## ~~InitializeComponent~~ -> LoadComponent

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

The resource loader will accept absolute paths from a drive root (e.g. `ms-appx://c:/MyApp/Extensions/Foo/FooPage.xaml`). Fortunately, Xaml is pretty consistent in how it gets packaged, so the `LoadComponent` extension method can fill the place of `InitializeComponent`, and will infer the correct information based on reflection. It will also re-use the generated `_contentLoaded` variable, which it accepts as a `ref`.

