using LibreHardwareMonitor.Hardware;

namespace ChipGauge.Hardware.Windows;

/// <summary>
/// The LibreHardwareMonitor visitor that refreshes every hardware node (and its
/// sub-hardware) so sensor values are current before a sampling pass reads them.
/// </summary>
internal sealed class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        ArgumentNullException.ThrowIfNull(computer);
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        ArgumentNullException.ThrowIfNull(hardware);
        hardware.Update();
        foreach (IHardware sub in hardware.SubHardware)
        {
            sub.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor)
    {
    }

    public void VisitParameter(IParameter parameter)
    {
    }
}
