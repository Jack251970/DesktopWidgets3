using LibreHardwareMonitor.Hardware;

namespace DesktopWidgets3.Models.Widget;

public class HardwareMonitor
{
    #region monitor options

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

    #endregion

    private readonly Computer _computer = new();
    private readonly UpdateVisitor _updateVisitor = new();

    private IList<IHardware> Hardware => _computer.Hardware;

    private readonly string UploadSpeedSensorName = "Upload Speed";
    private readonly string DownloadSpeedSensorName = "Download Speed";

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

    public (float UploadSpeed, float DownloadSpeed) GetNetworkSpeed()
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
