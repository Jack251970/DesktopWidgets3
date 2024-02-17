using System.Reflection;

using Windows.ApplicationModel;

namespace DesktopWidgets3.Core.Helpers;

/// <summary>
/// Helper for getting assembly/package information, supports packaged mode(MSIX)/unpackaged mode.
/// </summary>
public class InfoHelper
{
    public static string GetDisplayName()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.DisplayName;
        }
        else
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
    }

    public static string GetFamilyName()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Id.FamilyName;
        }
        else
        {
            return GetDisplayName();
        }
    }

    public static string GetName()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Id.Name;
        }
        else
        {
            return GetDisplayName();
        }
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
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Description;
        }
        else
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            return (attributes.Length == 0) ? "" : ((AssemblyDescriptionAttribute)attributes[0]).Description;
        }
    }

    public static string GetProduct()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.DisplayName;
        }
        else
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            return (attributes.Length == 0) ? "" : ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    public static string GetCopyright()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.PublisherDisplayName;
        }
        else
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return (attributes.Length == 0) ? "" : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    public static string GetCompany()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.PublisherDisplayName;
        }
        else
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            return (attributes.Length == 0) ? "" : ((AssemblyCompanyAttribute)attributes[0]).Company;
        }  
    }

    public static string GetConfiguration()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Id.Architecture.ToString();
        }
        else
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
            return (attributes.Length == 0) ? "" : ((AssemblyConfigurationAttribute)attributes[0]).Configuration;
        }
    }

    public static string GetTrademark()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Id.PublisherId;
        }
        else
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTrademarkAttribute), false);
            return (attributes.Length == 0) ? "" : ((AssemblyTrademarkAttribute)attributes[0]).Trademark;
        }
    }

    public static string GetCulture()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return "";
        }
        else
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCultureAttribute), false);
            return (attributes.Length == 0) ? "" : ((AssemblyCultureAttribute)attributes[0]).Culture;
        }
    }

    public static string GetInstalledLocation()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.InstalledLocation.Path;
        }
        else
        {
            return AppContext.BaseDirectory; //Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
