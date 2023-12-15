using LibreHardwareMonitor.Hardware;

namespace DesktopWidgets3.Models.Widget;

public enum MonitorType
{
    Gpu,
    NetWork
}

public abstract class BaseMonitor
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor;

    protected IList<IHardware> Hardware => _computer.Hardware;

    public BaseMonitor(MonitorType monitorType)
    {
        _computer = new Computer();
        switch (monitorType)
        {
            case MonitorType.Gpu:
                _computer.IsGpuEnabled = true;
                break;
            case MonitorType.NetWork:
                _computer.IsNetworkEnabled = true;
                break;
        }
        _updateVisitor = new UpdateVisitor();
    }

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
}

public class NetWorkMonitor : BaseMonitor
{
    private readonly string UploadSpeedSensorName = "Upload Speed";
    private readonly string DownloadSpeedSensorName = "Download Speed";

    public NetWorkMonitor() : base(MonitorType.NetWork)
    {
    }

    public (float UploadSpeed, float DownloadSpeed) GetNetworkSpeed()
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
