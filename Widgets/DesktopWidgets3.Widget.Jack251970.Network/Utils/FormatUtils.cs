using System.Globalization;

namespace DesktopWidgets3.Widget.Jack251970.Network.Utils;

public static class FormatUtils
{
    private const ulong Kilo = 1024;
    private const ulong Mega = 1024 * Kilo;
    private const ulong Giga = 1024 * Mega;
    private const ulong KiloGiga = 1024 * Giga;
    private const float RecKilo = 1f / Kilo;
    private const float RecMega = 1f / Mega;
    private const float RecGiga = 1f / Giga;
    private const float RecKiloGiga = 1f / KiloGiga;

    private static readonly string PercentageFormat = "{0:F2} %";
    private static readonly string CpuSpeedFormat = "{0:F2} GHz";
    private static readonly string BytesFormat = "{0:F2} {1}";
    private static readonly string CelsiusTemperatureFormat = "{0:F2} °C";
    private static readonly string FahrenheitTemperatureFormat = "{0:F2} °C";
    private static readonly string UsedInfoFormat = "{0:F2} / {1:F2} {2}";

    public static string FormatPercentage(float percentage)
    {
        return string.Format(CultureInfo.InvariantCulture, PercentageFormat, percentage * 100);
    }

    public static string FormatCpuSpeed(float cpuSpeed)
    {
        return string.Format(CultureInfo.InvariantCulture, CpuSpeedFormat, cpuSpeed / 1000);
    }

    public static string FormatBytes(float bytes, string unit)
    {
        if (bytes < Kilo)
        {
            return string.Format(CultureInfo.InvariantCulture, BytesFormat, bytes, unit);
        }
        else if (bytes < Mega)
        {
            return string.Format(CultureInfo.InvariantCulture, BytesFormat, bytes / Kilo, $"K{unit}");
        }
        else if (bytes < Giga)
        {
            return string.Format(CultureInfo.InvariantCulture, BytesFormat, bytes / Mega, $"M{unit}");
        }
        else
        {
            return string.Format(CultureInfo.InvariantCulture, BytesFormat, bytes / Giga, $"G{unit}");
        }
    }

    public static string FormatTemperature(float celsiusDegree, bool useCelsius)
    {
        if (useCelsius)
        {
            return string.Format(CultureInfo.InvariantCulture, CelsiusTemperatureFormat, celsiusDegree);
        }
        else
        {
            var fahrenheitDegree = celsiusDegree * 9 / 5 + 32;
            return string.Format(CultureInfo.InvariantCulture, FahrenheitTemperatureFormat, fahrenheitDegree);
        }
    }

    public static string FormatUsedInfoByte(ulong used, ulong total)
    {
        if (total < Kilo)
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used, total, "B");
        }
        else if (total < Mega)
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used * RecKilo, total * RecKilo, "KB");
        }
        else if (total < Giga)
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used * RecMega, total * RecMega, "MB");
        }
        else if (total < KiloGiga)
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used * RecGiga, total * RecGiga, "GB");
        }
        else
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used * RecKiloGiga, total * RecKiloGiga, "TB");
        }
    }
}
