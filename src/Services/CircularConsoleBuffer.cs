using System;
using System.Collections.Generic;
using System.Linq;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Efficient circular buffer for console output - no memory leaks!
    /// </summary>
    public class CircularConsoleBuffer
    {
        private readonly string[] _lines;
        private readonly object _lock = new();
        private int _head = 0;
        private int _count = 0;
        private readonly int _capacity;

        public event Action<string>? LineAdded;
        public event Action? BufferChanged;

        public CircularConsoleBuffer(int capacity = 1000)
        {
            _capacity = capacity;
            _lines = new string[capacity];
        }

        /// <summary>
        /// Add a line to the buffer
        /// </summary>
        public void AddLine(string line)
        {
            lock (_lock)
            {
                _lines[_head % _capacity] = line;
                _head++;
                if (_count < _capacity)
                    _count++;
            }
            
            // Fire events outside lock
            LineAdded?.Invoke(line);
            BufferChanged?.Invoke();
        }

        /// <summary>
        /// Get all lines in order (oldest to newest)
        /// </summary>
        public string[] GetAllLines()
        {
            lock (_lock)
            {
                if (_count == 0)
                    return Array.Empty<string>();

                var result = new string[_count];
                var start = _count < _capacity ? 0 : _head - _capacity;
                
                for (int i = 0; i < _count; i++)
                {
                    var idx = (start + i) % _capacity;
                    result[i] = _lines[idx] ?? string.Empty;
                }
                
                return result;
            }
        }

        /// <summary>
        /// Get the last N lines
        /// </summary>
        public string[] GetLastLines(int count)
        {
            lock (_lock)
            {
                if (_count == 0)
                    return Array.Empty<string>();

                var take = Math.Min(count, _count);
                var result = new string[take];
                var start = _head - take;

                for (int i = 0; i < take; i++)
                {
                    var idx = (start + i) % _capacity;
                    if (idx < 0) idx += _capacity;
                    result[i] = _lines[idx] ?? string.Empty;
                }

                return result;
            }
        }

        /// <summary>
        /// Get all text as a single string
        /// </summary>
        public string GetText()
        {
            return string.Join(Environment.NewLine, GetAllLines());
        }

        /// <summary>
        /// Clear the buffer
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                Array.Clear(_lines, 0, _lines.Length);
                _head = 0;
                _count = 0;
            }
            BufferChanged?.Invoke();
        }

        public int Count => _count;
        public int Capacity => _capacity;
    }
}