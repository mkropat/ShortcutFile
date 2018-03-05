using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ShortcutFile
{
    internal class BinFileReader : StreamWrapper
    {
        byte[] _bigBuf = new byte[0];
        readonly byte[] _scratchBuf = new byte[16];

        public BinFileReader(Stream stream, bool leaveOpen = false)
            : base(stream, leaveOpen)
        {
        }

        public void ReadExactly(byte[] buf, int offset, int n, Func<Exception> getEndOfStreamException = null)
        {
            if (n == 0)
                return;

            getEndOfStreamException = getEndOfStreamException ?? (() => new EndOfStreamException());

            var totalRead = 0;
            do
            {
                var numRead = Read(buf, offset + totalRead, n - totalRead);
                if (numRead == 0)
                    throw getEndOfStreamException();
                totalRead += numRead;
            } while (totalRead < n);
        }

        public short ReadInt16(Func<Exception> getEndOfStreamException = null)
        {
            ReadExactly(_scratchBuf, 0, sizeof(Int16), getEndOfStreamException);
            return BitConverter.ToInt16(_scratchBuf, 0);
        }

        public ushort ReadUInt16(Func<Exception> getEndOfStreamException = null)
        {
            ReadExactly(_scratchBuf, 0, sizeof(UInt16), getEndOfStreamException);
            return BitConverter.ToUInt16(_scratchBuf, 0);
        }

        public int ReadInt32(Func<Exception> getEndOfStreamException = null)
        {
            ReadExactly(_scratchBuf, 0, sizeof(Int32), getEndOfStreamException);
            return BitConverter.ToInt32(_scratchBuf, 0);
        }

        public uint ReadUint32(Func<Exception> getEndOfStreamException = null)
        {
            ReadExactly(_scratchBuf, 0, sizeof(UInt32), getEndOfStreamException);
            return BitConverter.ToUInt32(_scratchBuf, 0);
        }

        public string ReadByteCountPrefixString(int numSizeBytes, Encoding encoding, Func<Exception> getEndOfStreamException = null)
        {
            ReadExactly(_scratchBuf, 0, numSizeBytes, getEndOfStreamException);
            var size = GetSize(numSizeBytes, _scratchBuf);

            if (_bigBuf.Length < size)
                _bigBuf = new byte[size];

            return encoding.GetString(_bigBuf);
        }

        static int GetSize(int numSizeBytes, byte[] buf, int offset = 0)
        {
            switch (numSizeBytes)
            {
                case 1:
                    return buf[0];
                case 2:
                    return BitConverter.ToUInt16(buf, offset);
                case 4:
                    return BitConverter.ToInt32(buf, offset);
                default:
                    throw new Exception($"Unhandled size: {numSizeBytes}");
            }
        }

        public string ReadCharCountPrefixString(int numSizeBytes, Encoding encoding, Func<Exception> getEndOfStreamException = null)
        {
            ReadExactly(_scratchBuf, 0, numSizeBytes, getEndOfStreamException);
            var size = GetSize(numSizeBytes, _scratchBuf);
            return ReadNCharacters(size, encoding, getEndOfStreamException);
        }

        string ReadNCharacters(int n, Encoding encoding, Func<Exception> getEndOfStreamException)
        {
            var result = new StringBuilder();

            var decoder = encoding.GetDecoder();

            int read = 0;
            while (read < n)
            {
                var chars = ReadCharacter(decoder, getEndOfStreamException);
                read += chars.Length;
                result.Append(chars);
            }

            return result.ToString();
        }

        char[] ReadCharacter(Decoder decoder, Func<Exception> getEndOfStreamException)
        {
            int i = 0;
            int charCount = 0;
            do
            {
                ReadExactly(_scratchBuf, i, 1, getEndOfStreamException);
                i++;
                charCount = decoder.GetCharCount(_scratchBuf, 0, i);
            } while (charCount == 0);

            var chars = new char[charCount];

            decoder.GetChars(_scratchBuf, 0, i, chars, 0);
            return chars;
        }

        public string ReadNullTerminatedString(Encoding encoding, Func<Exception> getEndOfStreamException = null)
        {
            var result = new StringBuilder();
            var decoder = encoding.GetDecoder();

            while (true)
            {
                var chars = ReadCharacter(decoder, getEndOfStreamException);
                if (chars[0] == 0)
                    break;
                result.Append(chars);
            }

            return result.ToString();
        }

        public string ReadNullTerminatedString(int byteCount, Encoding encoding, Func<Exception> getEndOfStreamException = null)
        {
            if (_bigBuf.Length < byteCount)
                _bigBuf = new byte[byteCount];

            ReadExactly(_bigBuf, 0, byteCount, getEndOfStreamException);
            var str = encoding.GetString(_bigBuf, 0, byteCount);
            return str.Split(new[] { '\0' }, 2)[0];
        }

        public T ReadStructure<T>(Func<Exception> getEndOfStreamException = null) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            if (_bigBuf.Length < size)
                _bigBuf = new byte[size];
            ReadExactly(_bigBuf, 0, size, getEndOfStreamException);
            return ByteArrayToStructure<T>(_bigBuf);
        }

        static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
