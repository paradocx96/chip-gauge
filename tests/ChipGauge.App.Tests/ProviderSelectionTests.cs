using ChipGauge.App.Composition;
using ChipGauge.Core.Abstractions;

namespace ChipGauge.App.Tests;

public sealed class ProviderSelectionTests
{
    [Fact]
    public void Factory_ReturnsAUsableProvider()
    {
        using ISensorProvider provider = HardwareProviderFactory.Create();

        Assert.NotNull(provider);
        Assert.NotEmpty(provider.GetHardware());

        // Off Windows there is no live provider, so it must fall back to the
        // simulated one. On Windows either outcome is valid.
        if (!OperatingSystem.IsWindows())
        {
            Assert.True(provider.IsSimulated);
        }
    }

    [Fact]
    public void Factory_ProviderAlsoExposesInfoSeam()
    {
        using ISensorProvider provider = HardwareProviderFactory.Create();
        Assert.IsAssignableFrom<IHardwareInfoProvider>(provider);
    }
}
