using LibreHardwareMonitor.Hardware;

namespace HardwareInfo.Helpers;

public class HardwareMonitor
{
    #region monitor options

    public bool CpuEnabled
    {
        get => _computer.IsCpuEnabled;
        set => _computer.IsCpuEnabled = value;
    }

    public bool GpuEnabled
    {
        get => _computer.IsGpuEnabled;
        set => _computer.IsGpuEnabled = value;
    }

    public bool MemoryEnabled
    {
        get => _computer.IsMemoryEnabled;
        set => _computer.IsMemoryEnabled = value;
    }

    public bool NetworkEnabled
    {
        get => _computer.IsNetworkEnabled;
        set => _computer.IsNetworkEnabled = value;
    }

    #endregion

    private readonly Computer _computer = new();
    private readonly UpdateVisitor _updateVisitor = new();

    private IList<IHardware> Hardware => _computer.Hardware;

    public HardwareMonitor() { }

    public void Open()
    {
        _computer.Open();
    }

    public void Close()
    {
        _computer.Close();
    }

    public void Update()
    {
        _computer.Accept(_updateVisitor);
    }

    #region cpu info

    /// <summary>
    /// Get cpu infomation.
    /// </summary>
    public (float? CpuLoad, float? CpuTemperature) GetCpuInfo()
    {
        float? cpuLoad = null;
        float? cpuTemperature = null;

        if (CpuEnabled)
        {
            foreach (var hardware in Hardware)
            {
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Value != null)
                        {
                            cpuLoad = sensor.Value;
                        }
                        else if (sensor.SensorType == SensorType.Temperature && sensor.Value != null)
                        {
                            cpuTemperature = sensor.Value;
                        }
                    }
                }
            }
        }

        return (cpuLoad, cpuTemperature);
    }

    #endregion

    #region gpu info

    /// <summary>
    /// Get gpu infomation.
    /// </summary>
    public (HardwareType? GpuType, float? GpuLoad, float? GpuTemperature) GetGPUInfo()
    {
        HardwareType? gpuType = null;
        float? gpuLoad = null;
        float? gpuTemperature = null;

        if (GpuEnabled)
        {
            foreach (var hardware in Hardware)
            {
                switch (hardware.HardwareType)
                {
                    case HardwareType.GpuIntel:
                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Load && sensor.Value != null)
                            {
                                gpuLoad = sensor.Value;
                            }
                            else if (sensor.SensorType == SensorType.Temperature && sensor.Value != null)
                            {
                                gpuTemperature = sensor.Value;
                            }
                        }
                        break;
                    default:
                        continue;
                }
            }
        }

        return (gpuType, gpuLoad, gpuTemperature);
    }

    #endregion

    #region memory info

    private readonly string MemoryUsedSensorName = "Memory Used";
    private readonly string MemoryAvailableSensorName = "Memory Available";

    /// <summary>
    /// Get memory infomation.
    /// </summary>
    public (float? MemoryLoad, float? MemoryUsed, float? MemoryAvailable) GetMemoryInfo()
    {
        float? memoryLoad = null;
        float? memoryUsed = null;
        float? memoryAvailable = null;

        if (MemoryEnabled)
        {
            foreach (var hardware in Hardware)
            {
                if (hardware.HardwareType == HardwareType.Memory)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Value != null)
                        {
                            memoryLoad = sensor.Value;
                        }
                        else if (sensor.Name == MemoryUsedSensorName && sensor.Value != null)
                        {
                            memoryUsed = sensor.Value;
                        }
                        else if (sensor.Name == MemoryAvailableSensorName && sensor.Value != null)
                        {
                            memoryAvailable = sensor.Value;
                        }
                    }
                }
            }
        }

        return (memoryLoad, memoryUsed, memoryAvailable);
    }

    #endregion

    #region network info

    private readonly string UploadSpeedSensorName = "Upload Speed";
    private readonly string DownloadSpeedSensorName = "Download Speed";

    /// <summary>
    /// Get network infomation in K/s unit.
    /// </summary>
    public (float TotalUploadSpeed, float TotalDownloadSpeed) GetNetworkInfo()
    {
        float totalUploadSpeed = 0;
        float totalDownloadSpeed = 0;

        if (NetworkEnabled)
        {
            foreach (var hardware in Hardware)
            {
                if (hardware.HardwareType == HardwareType.Network)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.Name == UploadSpeedSensorName && sensor.Value != null)
                        {
                            totalUploadSpeed += (float)sensor.Value;
                        }
                        else if (sensor.Name == DownloadSpeedSensorName && sensor.Value != null)
                        {
                            totalDownloadSpeed += (float)sensor.Value;
                        }
                    }
                }
            }
        }

        return (totalUploadSpeed, totalDownloadSpeed);
    }

    #endregion
}

internal class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var subHardware in hardware.SubHardware)
        {
            subHardware.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor)
    {
    }

    public void VisitParameter(IParameter parameter)
    {
    }
}
