using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace nLogViewer.Infrastructure.Collections;

/// <summary>
/// Кольцевой буфер фиксированного размера для ограничения использования памяти
/// </summary>
internal class CircularBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int _start;
    private int _count;
    private readonly object _lock = new object();

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero", nameof(capacity));
            
        _buffer = new T[capacity];
        _start = 0;
        _count = 0;
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    public int Capacity => _buffer.Length;

    public void Add(T item)
    {
        lock (_lock)
        {
            var index = (_start + _count) % _buffer.Length;
            _buffer[index] = item;
            
            if (_count < _buffer.Length)
            {
                _count++;
            }
            else
            {
                _start = (_start + 1) % _buffer.Length;
            }
        }
    }

    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _start = 0;
            _count = 0;
        }
    }

    public T this[int index]
    {
        get
        {
            lock (_lock)
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                    
                var actualIndex = (_start + index) % _buffer.Length;
                return _buffer[actualIndex];
            }
        }
    }

    public IEnumerable<T> GetRange(int startIndex, int count)
    {
        lock (_lock)
        {
            if (startIndex < 0 || startIndex >= _count)
                return Enumerable.Empty<T>();
                
            var endIndex = Math.Min(startIndex + count, _count);
            var result = new List<T>(endIndex - startIndex);
            
            for (int i = startIndex; i < endIndex; i++)
            {
                var actualIndex = (_start + i) % _buffer.Length;
                result.Add(_buffer[actualIndex]);
            }
            
            return result;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock)
        {
            for (int i = 0; i < _count; i++)
            {
                var actualIndex = (_start + i) % _buffer.Length;
                yield return _buffer[actualIndex];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public List<T> ToList()
    {
        lock (_lock)
        {
            return new List<T>(this);
        }
    }
}