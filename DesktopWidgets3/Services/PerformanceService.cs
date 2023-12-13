using System.Diagnostics;
using DesktopWidgets3.Contracts.Services;

namespace DesktopWidgets3.Services;

public class PerformanceService : IPerformanceService
{
    private readonly List<NetworkAdapter> adapters = new();

    public PerformanceService()
    {
        InitAdapters();
    }

    private void InitAdapters()
    {
        var category = new PerformanceCounterCategory("Network Interface");

        foreach (var name in category.GetInstanceNames())
        {
            // This one exists on every computer.  
            if (name == "MS TCP Loopback interface" || name.Contains("isatap") || name.Contains("Interface"))
            {
                continue;
            }
            // Create an instance of NetworkAdapter class, and create performance counters for it.  
            var adapter = new NetworkAdapter(name)
            {
                dlCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", name),
                ulCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", name)
            };
            adapters.Add(adapter); // Add it to ArrayList adapter  
        }

        foreach (var adapter in adapters)
        {
            adapter.Init();
        }
    }

    public (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed()
    {
        var Upload = 0.0;
        foreach (var adapter in adapters)
        {
            adapter.Refresh();
            Upload += Math.Round(Convert.ToDouble(adapter.DownloadSpeed), 2);
        }

        var Down = 0.0;
        foreach (var adapter in adapters)
        {
            adapter.Refresh();
            Down += Math.Round(Convert.ToDouble(adapter.DownloadSpeed), 2);
        }

        return (FormatBytes(Upload), FormatBytes(Down));
    }

    private static string FormatBytes(double bytes)
    {
        const long kilobyte = 1024;
        const long megabyte = 1024 * kilobyte;
        const long gigabyte = 1024 * megabyte;

        if (bytes < kilobyte)
        {
            return $"{bytes:0.##} B/s";
        }
        else if (bytes < megabyte)
        {
            return $"{bytes / kilobyte:0.##} KB/s";
        }
        else if (bytes < gigabyte)
        {
            return $"{bytes / megabyte:0.##} MB/s";
        }
        else
        {
            return $"{bytes / gigabyte:0.##} GB/s";
        }
    }

    private class NetworkAdapter
    {
        internal NetworkAdapter(string instanceName)
        {
            name = instanceName;
        }

        private long dlSpeed, ulSpeed;       // Download/Upload speed in bytes per second.  
        private long dlValue, ulValue;       // Download/Upload counter value in bytes.  
        private long dlValueOld, ulValueOld; // Download/Upload counter value one second earlier, in bytes.  

        internal string name;                               // The name of the adapter.  
        internal PerformanceCounter dlCounter = null!, ulCounter = null!;   // Performance counters to monitor download and upload speed.

        /// <summary>  
        /// Preparations for monitoring.  
        /// </summary>  
        internal void Init()
        {
            // Since dlValueOld and ulValueOld are used in method refresh() to calculate network speed, they must have be initialized.  
            dlValueOld = dlCounter.NextSample().RawValue;
            ulValueOld = ulCounter.NextSample().RawValue;
        }
        /// <summary>  
        /// Obtain new sample from performance counters, and refresh the values saved in dlSpeed, ulSpeed, etc.  
        /// This method is supposed to be called only in NetworkMonitor, one time every second.  
        /// </summary>  
        internal void Refresh()
        {
            dlValue = dlCounter.NextSample().RawValue;
            ulValue = ulCounter.NextSample().RawValue;

            // Calculates download and upload speed.  
            dlSpeed = dlValue - dlValueOld;
            ulSpeed = ulValue - ulValueOld;

            dlValueOld = dlValue;
            ulValueOld = ulValue;
        }

        /// <summary>  
        /// Overrides method to return the name of the adapter.  
        /// </summary>  
        /// <returns>The name of the adapter.</returns>  
        public override string ToString()
        {
            return name;
        }

        /// <summary>  
        /// The name of the network adapter.  
        /// </summary>  
        public string Name => name;

        /// <summary>  
        /// Current download speed in bytes per second.  
        /// </summary>  
        public long DownloadSpeed => dlSpeed;

        /// <summary>  
        /// Current upload speed in bytes per second.  
        /// </summary>  
        public long UploadSpeed => ulSpeed;

        /// <summary>  
        /// Current download speed in kbytes per second.  
        /// </summary>  
        public double DownloadSpeedKbps => dlSpeed / 1024.0;

        /// <summary>  
        /// Current upload speed in kbytes per second.  
        /// </summary>  
        public double UploadSpeedKbps => ulSpeed / 1024.0;
    }
}
