using ChipGauge.Core.Storage;

namespace ChipGauge.Core.Tests.Storage;

public sealed class SmartDriveTests
{
    [Fact]
    public void Health_NoAttributes_IsUnknown()
    {
        var drive = new SmartDrive("d", "Empty");
        Assert.Equal(DriveHealth.Unknown, drive.Health);
    }

    [Fact]
    public void Health_AllHealthy_IsGood()
    {
        var drive = new SmartDrive("d", "Healthy SSD")
            .AddAttribute(new SmartAttribute(0x05, "Reallocated Sectors", 100, 100, 10, 0))
            .AddAttribute(new SmartAttribute(0x09, "Power-On Hours", 99, 99, 0, 1000))
            .AddAttribute(new SmartAttribute(0xC5, "Pending Sectors", 100, 100, 0, 0));

        Assert.Equal(DriveHealth.Good, drive.Health);
    }

    [Fact]
    public void Health_CriticalRawNonZero_IsCaution()
    {
        var drive = new SmartDrive("d", "Worn SSD")
            .AddAttribute(new SmartAttribute(0x05, "Reallocated Sectors", 100, 100, 10, 8));

        Assert.Equal(DriveHealth.Caution, drive.Health);
    }

    [Fact]
    public void Health_PendingSectors_IsCaution()
    {
        var drive = new SmartDrive("d", "Suspect HDD")
            .AddAttribute(new SmartAttribute(0xC5, "Current Pending Sector Count", 100, 100, 0, 3));

        Assert.Equal(DriveHealth.Caution, drive.Health);
    }

    [Fact]
    public void Health_BelowThreshold_IsBad()
    {
        var drive = new SmartDrive("d", "Failing SSD")
            .AddAttribute(new SmartAttribute(0x05, "Reallocated Sectors", 5, 5, 10, 2048));

        Assert.Equal(DriveHealth.Bad, drive.Health);
    }

    [Fact]
    public void Health_BadDominatesOverCaution()
    {
        var drive = new SmartDrive("d", "Dying SSD")
            .AddAttribute(new SmartAttribute(0x05, "Reallocated Sectors", 100, 100, 10, 40))   // caution signal
            .AddAttribute(new SmartAttribute(0xC5, "Pending Sectors", 1, 1, 5, 100));           // below threshold

        Assert.Equal(DriveHealth.Bad, drive.Health);
    }

    [Fact]
    public void Attribute_IsBelowThreshold_RespectsZeroThreshold()
    {
        // Threshold 0 means "no threshold" and must never count as failing.
        var attribute = new SmartAttribute(0x09, "Power-On Hours", 0, 0, 0, 5000);
        Assert.False(attribute.IsBelowThreshold);
    }

    [Fact]
    public void Attribute_IsBelowThreshold_TrueAtBoundary()
    {
        var attribute = new SmartAttribute(0x05, "Reallocated Sectors", 10, 10, 10, 100);
        Assert.True(attribute.IsBelowThreshold);
    }
}
