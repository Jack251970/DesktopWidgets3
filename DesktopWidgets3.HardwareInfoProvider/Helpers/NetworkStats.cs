// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace DesktopWidgets3.HardwareInfoProvider.Helpers;

public sealed class NetworkStats : IDisposable
{
    private string ClassName => GetType().Name;

    private readonly Dictionary<string, List<PerformanceCounter>> _networkCounters = [];

    private Dictionary<string, Data> NetworkUsages { get; set; } = [];

    private Dictionary<string, List<float>> NetChartValues { get; set; } = [];

    public sealed class Data
    {
        public float Usage
        {
            get; set;
        }

        public float Sent
        {
            get; set;
        }

        public float Received
        {
            get; set;
        }
    }

    public NetworkStats()
    {
        InitNetworkPerfCounters();
    }

    private void InitNetworkPerfCounters()
    {
        var pcc = new PerformanceCounterCategory("Network Interface");
        var instanceNames = pcc.GetInstanceNames();
        foreach (var instanceName in instanceNames)
        {
            var instanceCounters = new List<PerformanceCounter>
            {
                new("Network Interface", "Bytes Sent/sec", instanceName),
                new("Network Interface", "Bytes Received/sec", instanceName),
                new("Network Interface", "Current Bandwidth", instanceName)
            };
            _networkCounters.Add(instanceName, instanceCounters);
            NetChartValues.Add(instanceName, []);
            NetworkUsages.Add(instanceName, new Data());
        }
    }

    public void GetData()
    {
        float maxUsage = 0;
        foreach (var networkCounterWithName in _networkCounters)
        {
            try
            {
                var sent = networkCounterWithName.Value[0].NextValue();
                var received = networkCounterWithName.Value[1].NextValue();
                var bandWidth = networkCounterWithName.Value[2].NextValue();
                if (bandWidth == 0)
                {
                    continue;
                }

                var usage = 8 * (sent + received) / bandWidth;
                var name = networkCounterWithName.Key;
                NetworkUsages[name].Sent = sent;
                NetworkUsages[name].Received = received;
                NetworkUsages[name].Usage = usage;

                var chartValues = NetChartValues[name];

                if (usage > maxUsage)
                {
                    maxUsage = usage;
                }
            }
            catch (Exception ex)
            {
                LogExtensions.LogError(ClassName, ex, "Error getting network data.");
            }
        }
    }
    
    public int GetNetworkCount()
    {
        return NetworkUsages.Count;
    }

    public string GetNetworkName(int networkIndex)
    {
        if (NetChartValues.Count <= networkIndex)
        {
            return string.Empty;
        }

        return NetChartValues.ElementAt(networkIndex).Key;
    }

    public Data GetNetworkUsage(int networkIndex)
    {
        if (NetChartValues.Count <= networkIndex)
        {
            return new Data();
        }

        var currNetworkName = NetChartValues.ElementAt(networkIndex).Key;
        if (!NetworkUsages.TryGetValue(currNetworkName, out var value))
        {
            return new Data();
        }

        return value;
    }

    public int GetPrevNetworkIndex(int networkIndex)
    {
        if (NetChartValues.Count == 0)
        {
            return 0;
        }

        if (networkIndex == 0)
        {
            return NetChartValues.Count - 1;
        }

        return networkIndex - 1;
    }

    public int GetNextNetworkIndex(int networkIndex)
    {
        if (NetChartValues.Count == 0)
        {
            return 0;
        }

        if (networkIndex == NetChartValues.Count - 1)
        {
            return 0;
        }

        return networkIndex + 1;
    }

    public void Dispose()
    {
        foreach (var counterPair in _networkCounters)
        {
            foreach (var counter in counterPair.Value)
            {
                counter.Dispose();
            }
        }
    }
}
