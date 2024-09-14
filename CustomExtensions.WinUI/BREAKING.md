# Breaking changes

# Breaking changes in 0.2.0

- Application host must call `ApplicationExtensionHost.Initialize<TApplication>(TApplication app)` before calling any other extension APIs.
- `ApplicationExtensions.LoadExtension(this Application, Assembly)` extension method removed. Callers should instead call `ApplicationExtensionHost.Current.LoadExtensionAsync`.
- `WinUIExtensions.LoadComponent<T>(this T, ref bool, [CallerFilePath]string="")` is no longer templated, any explicit template specifications must be removed.

