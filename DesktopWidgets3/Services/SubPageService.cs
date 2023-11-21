using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Views.Pages;
using DesktopWidgets3.Views.SubPages;

namespace DesktopWidgets3.Services;

public class SubPageService : ISubPageService
{
    private readonly Dictionary<Type, string> _subPages = new();

    private readonly List<string> parentPages = new();
    public List<string> ParentPages => parentPages;

    public SubPageService()
    {
        // TODO: Register your services of new subpages here.
        // Subpages of timing page
        Configure<CompleteTimingPage, TimingPage> ();
        Configure<MainTimingPage, TimingPage>();
        Configure<SetMinutesPage, TimingPage>();
        Configure<StartSettingPage, TimingPage>();
    }

    public string GetParentPage(Type subPage)
    {
        string? parentPage;
        lock (_subPages)
        {
            if (!_subPages.TryGetValue(subPage, out parentPage))
            {
                throw new ArgumentException($"Page not found: {subPage}. Did you forget to call SubPageService.Configure?");
            }
        }

        return parentPage;
    }

    private void Configure<SPV, PV>()
        where SPV : Page
        where PV : Page
    {
        lock (_subPages)
        {
            var subPage = typeof(SPV);
            if (_subPages.ContainsKey(subPage))
            {
                throw new ArgumentException($"The key {subPage} is already configured in SubPageService!");
            }

            var parentPage = typeof(PV).FullName!;
            if (!_subPages.ContainsValue(parentPage))
            {
                parentPages.Add(parentPage);
            }

            _subPages.Add(subPage, parentPage);
        }
    }
}
