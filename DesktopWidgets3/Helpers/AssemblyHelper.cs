using System.Reflection;
using Windows.ApplicationModel;

namespace DesktopWidgets3.Helpers;

public class AssemblyHelper
{
    public static string GetTitle()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
        if (attributes.Length > 0)
        {
            var titleAttribute = (AssemblyTitleAttribute)attributes[0];
            if (titleAttribute.Title != "")
            {
                return titleAttribute.Title;
            }
        }
        return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
    }

    public static Version GetVersion()
    {
        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;
            return new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            return Assembly.GetExecutingAssembly().GetName().Version!;
        }
    }

    public static string GetDescription()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyDescriptionAttribute)attributes[0]).Description;
    }

    public static string GetProduct()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyProductAttribute)attributes[0]).Product;
    }

    public static string GetCopyright()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyCopyrightAttribute) attributes[0]).Copyright;
    }

    public static string GetCompany()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyCompanyAttribute) attributes[0]).Company;
    }

    public static string GetConfiguration()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyConfigurationAttribute)attributes[0]).Configuration;
    }

    public static string GetTrademark()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTrademarkAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyTrademarkAttribute)attributes[0]).Trademark;
    }

    public static string GetCulture()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCultureAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyCultureAttribute) attributes[0]).Culture;
    }
}
