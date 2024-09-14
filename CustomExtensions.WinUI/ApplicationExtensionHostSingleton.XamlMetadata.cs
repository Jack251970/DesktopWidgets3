using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

using System.Reflection;

namespace CustomExtensions.WinUI;

internal partial class ApplicationExtensionHostSingleton<T> where T : Application
{
	private static readonly PropertyInfo MetadataProviderProperty =
		typeof(T).GetProperty(
			"_AppProvider",
			BindingFlags.NonPublic | BindingFlags.Instance,
			null,
			null,
			Array.Empty<Type>(),
			null)
		?? throw new AccessViolationException();

	private static readonly PropertyInfo TypeInfoProviderProperty =
		MetadataProviderProperty.PropertyType.GetProperty(
			"Provider",
			BindingFlags.NonPublic | BindingFlags.Instance,
			null,
			null,
			Array.Empty<Type>(),
			null)
		?? throw new AccessViolationException();

	private static readonly PropertyInfo OtherProvidersProperty =
		TypeInfoProviderProperty.PropertyType.GetProperty(
			"OtherProviders",
			BindingFlags.NonPublic | BindingFlags.Instance,
			null,
			typeof(List<IXamlMetadataProvider>),
			Array.Empty<Type>(),
			null)
		?? throw new AccessViolationException();

	private List<IXamlMetadataProvider> OtherProviders
	{
		get
		{
			object appProvider = MetadataProviderProperty.GetValue(Application) ?? throw new AccessViolationException();
			object provider = TypeInfoProviderProperty.GetValue(appProvider) ?? throw new AccessViolationException();
			List<IXamlMetadataProvider> otherProviders = (OtherProvidersProperty.GetValue(provider) as List<IXamlMetadataProvider>) ?? throw new AccessViolationException();
			return otherProviders;
		}
	}

	public IDisposable RegisterXamlTypeMetadataProvider(IXamlMetadataProvider provider)
	{
		OtherProviders.Add(provider);
		return new RunOnDispose(() => OtherProviders.Remove(provider));
	}
}