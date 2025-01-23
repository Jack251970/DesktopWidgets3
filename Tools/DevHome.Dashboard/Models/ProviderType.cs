namespace DevHome.Dashboard.Models;

// TODO: Remove this?
// From Microsoft.Windows.DevHome.SDK
public enum ProviderType
{
    Repository,
    DeveloperId,
    Settings,
    FeaturedApplications,
    ComputeSystem,
    QuickStartProject,
    LocalRepository
}

public interface IExtension
{
    object GetProvider(ProviderType providerType);

    void Dispose();
}
