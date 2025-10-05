using System;
using NAudio.Wave;

namespace BalatroSeedOracle.Features.VibeOut
{
    /// <summary>
    /// Stream that loops audio seamlessly
    /// </summary>
    public class LoopStream : WaveStream
    {
        private readonly WaveStream _sourceStream;
        private bool _enableLooping;

        public LoopStream(WaveStream sourceStream)
        {
            _sourceStream = sourceStream;
            _enableLooping = true;
        }

        public bool EnableLooping
        {
            get => _enableLooping;
            set => _enableLooping = value;
        }

        public override WaveFormat WaveFormat => _sourceStream.WaveFormat;

        public override long Length => _sourceStream.Length;

        public override long Position
        {
            get => _sourceStream.Position;
            set => _sourceStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    if (_enableLooping)
                    {
                        // Reset to beginning for seamless loop
                        _sourceStream.Position = 0;
                    }
                    else
                    {
                        break;
                    }
                }
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sourceStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
