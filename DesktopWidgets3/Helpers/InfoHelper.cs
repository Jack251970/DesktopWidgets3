// Copyright (c) 2024 Jack251970
// Licensed under the GPL License. See the LICENSE.

using System.Globalization;
using System.Reflection;

using Windows.ApplicationModel;

namespace DesktopWidgets3.Helpers;

/// <summary>
/// Helper for getting assembly/package information, supports packaged mode(MSIX)/unpackaged mode.
/// </summary>
public static class InfoHelper
{
    #region name

    public static string GetName()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Id.Name;
        }
        else
        {
            return GetAssemblyName();
        }
    }

    public static string GetDisplayName()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.DisplayName;
        }
        else
        {
            return GetAssemblyName();
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
            return GetAssemblyName();
        }
    }

    public static string GetFullName()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Id.FullName;
        }
        else
        {
            return GetAssemblyName();
        }
    }

    private static string GetAssemblyName()
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

    #endregion

    #region version

    public static Version GetVersion()
    {
        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;
            return new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            return GetAssemblyVersion();
        }
    }

    private static Version GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version!;
    }

    #endregion

    #region description

    public static string GetDescription()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Description;
        }
        else
        {
            return GetAssemblyDescription();
        }
    }

    private static string GetAssemblyDescription()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyDescriptionAttribute)attributes[0]).Description;
    }

    #endregion

    #region product

    public static string GetProduct()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.DisplayName;
        }
        else
        {
            return GetAssemblyProduct();
        }
    }

    private static string GetAssemblyProduct()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyProductAttribute)attributes[0]).Product;
    }

    #endregion

    #region copyright

    public static string GetCopyright()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.PublisherDisplayName;
        }
        else
        {
            return GetAssemblyCopyright();
        }
    }

    private static string GetAssemblyCopyright()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
    }

    #endregion

    #region company

    public static string GetCompany()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.PublisherDisplayName;
        }
        else
        {
            return GetAssemblyCompany();
        }
    }

    private static string GetAssemblyCompany()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyCompanyAttribute)attributes[0]).Company;
    }

    #endregion

    #region configuration

    public static string GetConfiguration()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Id.Architecture.ToString();
        }
        else
        {
            return GetAssemblyConfiguration();
        }
    }

    private static string GetAssemblyConfiguration()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyConfigurationAttribute)attributes[0]).Configuration;
    }

    #endregion

    #region trademark

    public static string GetTrademark()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.Id.PublisherId;
        }
        else
        {
            return GetAssemblyTrademark();
        }
    }

    private static string GetAssemblyTrademark()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTrademarkAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyTrademarkAttribute)attributes[0]).Trademark;
    }

    #endregion

    #region culture

    public static string GetCulture()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return CultureInfo.CurrentCulture.ToString();
        }
        else
        {
            return GetAssemblyCulture();
        }
    }

    private static string GetAssemblyCulture()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCultureAttribute), false);
        return (attributes.Length == 0) ? "" : ((AssemblyCultureAttribute)attributes[0]).Culture;
    }

    #endregion

    #region path

    public static string GetInstalledLocation()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.InstalledLocation.Path;
        }
        else
        {
            return GetAssemblyPath();
        }
    }

    public static string GetEffectivePath()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return Package.Current.EffectivePath;
        }
        else
        {
            return GetAssemblyPath();
        }
    }

    private static string GetAssemblyPath()
    {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
    }

    #endregion
}
