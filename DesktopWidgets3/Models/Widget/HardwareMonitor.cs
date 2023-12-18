using LibreHardwareMonitor.Hardware;

namespace DesktopWidgets3.Models.Widget;

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

    public bool NetworkEnabled
    {
        get => _computer.IsNetworkEnabled;
        set => _computer.IsNetworkEnabled = value;
    }

    public bool MemoryEnabled
    {
        get => _computer.IsMemoryEnabled;
        set => _computer.IsMemoryEnabled = value;
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

    private readonly string CpuTotalName = "CPU Total";

    /// <summary>
    /// Get cpu infomation.
    /// </summary>
    public float GetCpuInfo()
    {
        if (CpuEnabled)
        {
            float cpuTotal = 0;

            foreach (var hardware in Hardware)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.Name == CpuTotalName && sensor.Value != null)
                    {
                        cpuTotal = (float)sensor.Value;
                    }
                }
            }

            return cpuTotal;
        }
        return 0;
    }

    #endregion

    #region network info

    private readonly string UploadSpeedSensorName = "Upload Speed";
    private readonly string DownloadSpeedSensorName = "Download Speed";

    /// <summary>
    /// Get network infomation in K/s unit.
    /// </summary>
    public (float UploadSpeed, float DownloadSpeed) GetNetworkInfo()
    {
        if (NetworkEnabled)
        {
            float totalUploadSpeed = 0;
            float totalDownloadSpeed = 0;

            foreach (var hardware in Hardware)
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

            return (totalUploadSpeed, totalDownloadSpeed);
        }
        return (0, 0);
    }

    #endregion

    #region memory info

    private readonly string MemoryUsedName = "Memory Used";
    private readonly string MemoryAvailableName = "Memory Available";

    /// <summary>
    /// Get memory infomation in GB unit.
    /// </summary>
    public (float UsedMemory, float AvailableMemory) GetMemoryInfo()
    {
        if (MemoryEnabled)
        {
            float memoryUsed = 0;
            float memoryAvailable = 0;

            foreach (var hardware in Hardware)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.Name == MemoryUsedName && sensor.Value != null)
                    {
                        memoryUsed = (float)sensor.Value;
                    }
                    else if (sensor.Name == MemoryAvailableName && sensor.Value != null)
                    {
                        memoryAvailable = (float)sensor.Value;
                    }
                }
            }

            return (memoryUsed, memoryAvailable);
        }
        return (0, 0);
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
