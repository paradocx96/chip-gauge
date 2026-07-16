using System.Collections;

namespace ChipGauge.Core.Monitoring;

/// <summary>
/// A fixed-capacity circular buffer. Once full, each <see cref="Add"/> overwrites
/// the oldest element in place, so appending is O(1) and never allocates — the
/// property that keeps the sampling hot path garbage-free while feeding the
/// scrolling charts. Indexing is oldest-to-newest: <c>this[0]</c> is the oldest
/// retained element and <c>this[Count - 1]</c> is the newest.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class RingBuffer<T> : IReadOnlyList<T>
{
    private readonly T[] _buffer;
    private int _start;
    private int _count;

    /// <summary>Creates a ring buffer with the given fixed capacity.</summary>
    /// <param name="capacity">Maximum number of retained elements; must be positive.</param>
    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _buffer = new T[capacity];
    }

    /// <summary>The maximum number of elements the buffer retains.</summary>
    public int Capacity => _buffer.Length;

    /// <summary>The number of elements currently retained (0..<see cref="Capacity"/>).</summary>
    public int Count => _count;

    /// <summary>True once the buffer has reached capacity and further adds overwrite the oldest element.</summary>
    public bool IsFull => _count == _buffer.Length;

    /// <summary>The newest element, or <see langword="default"/> when empty.</summary>
    public T? Newest => _count > 0 ? this[_count - 1] : default;

    /// <summary>The oldest retained element, or <see langword="default"/> when empty.</summary>
    public T? Oldest => _count > 0 ? this[0] : default;

    /// <summary>Gets the element at <paramref name="index"/>, where 0 is the oldest.</summary>
    public T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);
            return _buffer[(_start + index) % _buffer.Length];
        }
    }

    /// <summary>
    /// Appends an element. If the buffer is full, the oldest element is overwritten
    /// and the window advances by one.
    /// </summary>
    public void Add(T item)
    {
        if (_count == _buffer.Length)
        {
            _buffer[_start] = item;
            _start = (_start + 1) % _buffer.Length;
        }
        else
        {
            _buffer[(_start + _count) % _buffer.Length] = item;
            _count++;
        }
    }

    /// <summary>Removes all elements and clears retained references.</summary>
    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _start = 0;
        _count = 0;
    }

    /// <summary>Copies the retained elements, oldest first, into a new array.</summary>
    public T[] ToArray()
    {
        var result = new T[_count];
        for (var i = 0; i < _count; i++)
        {
            result[i] = _buffer[(_start + i) % _buffer.Length];
        }

        return result;
    }

    /// <summary>Enumerates the retained elements from oldest to newest.</summary>
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
        {
            yield return _buffer[(_start + i) % _buffer.Length];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
