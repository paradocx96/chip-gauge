using ChipGauge.Core.Monitoring;

namespace ChipGauge.Core.Tests.Monitoring;

public sealed class RingBufferTests
{
    [Fact]
    public void Constructor_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(-1));
    }

    [Fact]
    public void Add_BelowCapacity_GrowsCountAndPreservesOrder()
    {
        var buffer = new RingBuffer<int>(4);
        buffer.Add(10);
        buffer.Add(20);
        buffer.Add(30);

        Assert.Equal(3, buffer.Count);
        Assert.False(buffer.IsFull);
        Assert.Equal([10, 20, 30], buffer.ToArray());
        Assert.Equal(10, buffer.Oldest);
        Assert.Equal(30, buffer.Newest);
    }

    [Fact]
    public void Add_BeyondCapacity_OverwritesOldestAndKeepsWindow()
    {
        var buffer = new RingBuffer<int>(3);
        for (var i = 1; i <= 5; i++)
        {
            buffer.Add(i);
        }

        Assert.True(buffer.IsFull);
        Assert.Equal(3, buffer.Count);
        Assert.Equal([3, 4, 5], buffer.ToArray());
        Assert.Equal(3, buffer.Oldest);
        Assert.Equal(5, buffer.Newest);
    }

    [Fact]
    public void Indexer_ReturnsOldestToNewest()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4); // evicts 1

        Assert.Equal(2, buffer[0]);
        Assert.Equal(3, buffer[1]);
        Assert.Equal(4, buffer[2]);
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.Add(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[1]);
    }

    [Fact]
    public void Enumerator_YieldsOldestToNewest()
    {
        var buffer = new RingBuffer<int>(3);
        for (var i = 1; i <= 4; i++)
        {
            buffer.Add(i);
        }

        Assert.Equal([2, 3, 4], buffer.ToList());
    }

    [Fact]
    public void EmptyBuffer_HasDefaultEndpoints()
    {
        var buffer = new RingBuffer<int>(2);

        Assert.Empty(buffer);
        Assert.Equal(0, buffer.Oldest);
        Assert.Equal(0, buffer.Newest);
        Assert.Empty(buffer.ToArray());
    }

    [Fact]
    public void Clear_ResetsBuffer()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.Add(1);
        buffer.Add(2);

        buffer.Clear();

        Assert.Empty(buffer);
        Assert.False(buffer.IsFull);
        Assert.Empty(buffer.ToArray());
    }

    [Fact]
    public void CapacityOne_KeepsOnlyLatest()
    {
        var buffer = new RingBuffer<string>(1);
        buffer.Add("a");
        buffer.Add("b");

        Assert.Single(buffer);
        Assert.Equal(["b"], buffer.ToArray());
    }
}
