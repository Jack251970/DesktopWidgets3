using DesktopWidgets3.Core.Helpers;

namespace Files.App.Services;

/// <inheritdoc/>
// TODO: change to internal.
public sealed class ApplicationService : IApplicationService
{
	// Workaround to help improve code clarity
	internal static readonly AppEnvironment AppEnvironment =
#if STORE
		AppEnvironment.Store;
#elif PREVIEW
		AppEnvironment.Preview;
#elif STABLE
		AppEnvironment.Stable;
#else
		AppEnvironment.Dev;
#endif

	/// <inheritdoc/>
	public AppEnvironment Environment { get; } = AppEnvironment;

    /// <inheritdoc/>
    public Version AppVersion { get; } = InfoHelper.GetVersion();

    /// <inheritdoc/>
    public string AppIcoPath { get; } = AppEnvironment switch
	{
		AppEnvironment.Dev => Constants.AssetPaths.DevLogo,
		AppEnvironment.Preview => Constants.AssetPaths.PreviewLogo,
		_ => Constants.AssetPaths.StableLogo
	};
}
