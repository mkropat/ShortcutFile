using System;
using System.IO;

namespace ShortcutFile
{
    internal class StreamWrapper : IDisposable
    {
        readonly bool _leaveOpen;
        readonly Stream _stream;

        public StreamWrapper(Stream stream, bool leaveOpen)
        {
            _leaveOpen = leaveOpen;
            _stream = stream;
        }

        public virtual bool CanRead => _stream.CanRead;
        public virtual bool CanSeek => _stream.CanSeek;
        public virtual bool CanWrite => _stream.CanWrite;
        public virtual long Length => _stream.Length;

        public virtual long Position { get => _stream.Position; set => _stream.Position = value; }

        public virtual void Flush()
        {
            _stream.Flush();
        }

        public virtual int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public virtual long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public virtual void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public virtual void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        #region IDisposable Support
        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && !_leaveOpen)
                    _stream.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
