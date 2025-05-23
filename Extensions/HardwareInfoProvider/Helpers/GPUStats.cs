// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Management.Infrastructure;

namespace HardwareInfoProvider.Helpers;

public sealed class GPUStats : IDisposable
{
    // GPU counters
    private readonly Dictionary<int, List<PerformanceCounter>> _gpuCounters = [];

    private readonly List<Data> _stats = [];

    public sealed class Data
    {
        public string? Name { get; set; }

        public int PhysId { get; set; }

        public float Usage { get; set; }

        public float Temperature { get; set; }
    }

    public GPUStats()
    {
        LoadGPUs();
        GetGPUPerfCounters();
    }

    public void LoadGPUs()
    {
        using var session = CimSession.Create(null);
        var i = 0;
        _stats.Clear();

        foreach (var obj in session.QueryInstances("root/cimv2", "WQL", "select * from Win32_VideoController"))
        {
            var gpuName = (string)obj.CimInstanceProperties["name"].Value;
            _stats.Add(new Data() { Name = gpuName, PhysId = i++ });
        }
    }

    public void GetGPUPerfCounters()
    {
        _gpuCounters.Clear();

        var pcg = new PerformanceCounterCategory("GPU Engine");
        var instanceNames = pcg.GetInstanceNames();

        foreach (var instanceName in instanceNames)
        {
            if (!instanceName.EndsWith("3D", StringComparison.InvariantCulture))
            {
                continue;
            }

            foreach (var counter in pcg.GetCounters(instanceName).Where(x => x.CounterName.StartsWith("Utilization Percentage", StringComparison.InvariantCulture)))
            {
                var counterKey = counter.InstanceName;

                // skip these values
                GetKeyValueFromCounterKey("pid", ref counterKey);
                GetKeyValueFromCounterKey("luid", ref counterKey);

                int phys;
                var success = int.TryParse(GetKeyValueFromCounterKey("phys", ref counterKey), out phys);
                if (success)
                {
                    GetKeyValueFromCounterKey("eng", ref counterKey);
                    var engtype = GetKeyValueFromCounterKey("engtype", ref counterKey);
                    if (engtype != "3D")
                    {
                        continue;
                    }

                    if (!_gpuCounters.TryGetValue(phys, out var value))
                    {
                        value = [];
                        _gpuCounters.Add(phys, value);
                    }

                    value.Add(counter);
                }
            }
        }
    }

    public void GetData()
    {
        foreach (var gpu in _stats)
        {
            List<PerformanceCounter>? counters;
            var success = _gpuCounters.TryGetValue(gpu.PhysId, out counters);

            if (success && counters != null)
            {
                // DevHomeTODO: This outer try/catch should be replaced with more secure locking around shared resources.
                try
                {
                    var sum = 0.0f;
                    var countersToRemove = new List<PerformanceCounter>();
                    foreach (var counter in counters)
                    {
                        try
                        {
                            // NextValue() can throw an InvalidOperationException if the counter is no longer there.
                            sum += counter.NextValue();
                        }
                        catch (InvalidOperationException ex)
                        {
                            // We can't modify the list during the loop, so save it to remove at the end.
                            Debug.WriteLine($"Failed to get next value, remove: {ex.Message}");
                            countersToRemove.Add(counter);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to get next value: {ex.Message}");
                        }
                    }

                    foreach (var counter in countersToRemove)
                    {
                        counters.Remove(counter);
                        counter.Dispose();
                    }

                    gpu.Usage = sum / 100;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error summing process counters: {ex.Message}");
                }
            }
        }
    }

    public string GetGPUName(int gpuActiveIndex)
    {
        if (_stats.Count <= gpuActiveIndex)
        {
            return string.Empty;
        }

        return _stats[gpuActiveIndex].Name ?? string.Empty;
    }

    public int GetPrevGPUIndex(int gpuActiveIndex)
    {
        if (_stats.Count == 0)
        {
            return 0;
        }

        if (gpuActiveIndex == 0)
        {
            return _stats.Count - 1;
        }

        return gpuActiveIndex - 1;
    }

    public int GetNextGPUIndex(int gpuActiveIndex)
    {
        if (_stats.Count == 0)
        {
            return 0;
        }

        if (gpuActiveIndex == _stats.Count - 1)
        {
            return 0;
        }

        return gpuActiveIndex + 1;
    }

    public float GetGPUUsage(int gpuActiveIndex)
    {
        if (_stats.Count <= gpuActiveIndex)
        {
            return 0;
        }

        return _stats[gpuActiveIndex].Usage;
    }

    public float GetGPUTemperature(int gpuActiveIndex)
    {
        if (_stats.Count <= gpuActiveIndex)
        {
            return 0;
        }

        return _stats[gpuActiveIndex].Temperature;
    }

    private static string GetKeyValueFromCounterKey(string key, ref string counterKey)
    {
        if (!counterKey.StartsWith(key, StringComparison.InvariantCulture))
        {
            // throw new Exception();
            return "error";
        }

        counterKey = counterKey[(key.Length + 1)..];
        if (key.Equals("engtype", StringComparison.Ordinal))
        {
            return counterKey;
        }

        var pos = counterKey.IndexOf('_');
        if (key.Equals("luid", StringComparison.Ordinal))
        {
            pos = counterKey.IndexOf('_', pos + 1);
        }

        var retValue = counterKey[..pos];
        counterKey = counterKey[(pos + 1)..];
        return retValue;
    }

    public void Dispose()
    {
        foreach (var counterPair in _gpuCounters)
        {
            foreach (var counter in counterPair.Value)
            {
                counter.Dispose();
            }
        }
    }
}
