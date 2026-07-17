using System.Runtime.Versioning;
using ChipGauge.Core.Abstractions;
using ChipGauge.Hardware.Mock;
using ChipGauge.Hardware.Windows;

namespace ChipGauge.App.Composition;

/// <summary>
/// Selects the hardware provider for the current machine: the live
/// LibreHardwareMonitor provider on Windows (when it initializes and finds
/// hardware), otherwise the deterministic <see cref="MockSensorProvider"/>. The
/// returned instance implements both <see cref="ISensorProvider"/> and
/// <see cref="IHardwareInfoProvider"/>.
/// </summary>
public static class HardwareProviderFactory
{
    /// <summary>Creates the best available provider for this OS.</summary>
    public static ISensorProvider Create()
    {
        if (OperatingSystem.IsWindows() && TryCreateLive() is { } live)
        {
            return live;
        }

        return new MockSensorProvider();
    }

    [SupportedOSPlatform("windows")]
    private static LibreHardwareProvider? TryCreateLive()
    {
        var provider = new LibreHardwareProvider();
        if (provider.IsAvailable && provider.GetHardware().Count > 0)
        {
            return provider;
        }

        provider.Dispose();
        return null;
    }
}
