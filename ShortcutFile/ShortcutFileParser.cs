using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ShortcutFile
{
    public class InvalidShortcutFileException : Exception
    {
        public InvalidShortcutFileException(string message)
            : base(message)
        { }
    }

    public class ShortcutParameters
    {
        public string TargetPath { get; set; }
        public string RelativePath { get; set; }
        public string EnvironmentVariable { get; set; }
    }

    public class ShortcutFileParser
    {
        readonly Guid _expectedLinkClsid = Guid.ParseExact("00021401-0000-0000-C000-000000000046", "D");

        public ShortcutParameters Parse(Stream s)
        {
            using (var reader = new BinFileReader(s, leaveOpen: true))
            {
                var header = reader.ReadStructure<ShellLinkHeader>(() => new InvalidShortcutFileException("Unable to read header"));

                if (header.HeaderSize != ShellLinkHeader.Size)
                    throw new InvalidShortcutFileException("Unexpected header size");

                if (header.LinkClsid != _expectedLinkClsid)
                    throw new InvalidShortcutFileException("Unexpected header magic value");

                var result = new ShortcutParameters();

                var systemDefaultEncoding = Encoding.GetEncoding("Windows-1252");
                var encoding = header.LinkFlags.HasFlag(LinkFlag.IsUnicode)
                    ? Encoding.Unicode
                    : systemDefaultEncoding;

                if (header.LinkFlags.HasFlag(LinkFlag.HasLinkTargetIdList))
                {
                    var idListSize = reader.ReadUInt16();
                    reader.Seek(idListSize, SeekOrigin.Current);
                }

                if (header.LinkFlags.HasFlag(LinkFlag.HasLinkInfo))
                {
                    var linkInfoHeaderBuf = new byte[BaseLinkInfoHeader.Size];
                    reader.ReadExactly(linkInfoHeaderBuf, 0, BaseLinkInfoHeader.Size);
                    var baseLinkInfoHeader = ByteArrayToStructure<BaseLinkInfoHeader>(linkInfoHeaderBuf);

                    var linkInfoBuf = new byte[baseLinkInfoHeader.LinkInfoSize];
                    Array.Copy(linkInfoHeaderBuf, linkInfoHeaderBuf, linkInfoHeaderBuf.Length);
                    if (baseLinkInfoHeader.LinkInfoHeaderSize > linkInfoHeaderBuf.Length)
                        reader.Read(linkInfoBuf, linkInfoHeaderBuf.Length, baseLinkInfoHeader.LinkInfoHeaderSize - linkInfoHeaderBuf.Length);

                    var linkInfoHeader = ByteArrayToStructure<FullLinkInfoHeader>(linkInfoBuf);

                    reader.Read(linkInfoBuf, baseLinkInfoHeader.LinkInfoHeaderSize, linkInfoBuf.Length - baseLinkInfoHeader.LinkInfoHeaderSize);

                    var commonPathSuffix = GetNullTerminatedUtf16(linkInfoBuf, linkInfoHeader.CommonPathSuffixOffsetUnicode);
                    var localBasePath = GetNullTerminatedUtf16(linkInfoBuf, linkInfoHeader.LocalBasePathOffsetUnicode);

                    result.TargetPath = commonPathSuffix + localBasePath;
                }

                if (header.LinkFlags.HasFlag(LinkFlag.HasName))
                {
                    var name = reader.ReadCharCountPrefixString(2, encoding);
                }

                if (header.LinkFlags.HasFlag(LinkFlag.HasRelativePath))
                {
                    result.RelativePath = reader.ReadCharCountPrefixString(2, encoding);
                }

                while (true)
                {
                    var size = reader.ReadInt32();
                    if (size < 4)
                        break;

                    var signature = (ExtraDataSignature)reader.ReadUint32();

                    var offsetToNextBlock = size - sizeof(Int32) - sizeof(UInt32);

                    switch (signature)
                    {
                        case ExtraDataSignature.EnvironmentDataBlock:
                            var targetAnsi = reader.ReadNullTerminatedString(260, systemDefaultEncoding);
                            var targetUnicode = reader.ReadNullTerminatedString(520, Encoding.Unicode);
                            result.EnvironmentVariable = targetUnicode;
                            break;

                        default:
                            reader.Seek(offsetToNextBlock, SeekOrigin.Current);
                            break;
                    }
                }

                return result;
            }
        }

        static string GetNullTerminatedUtf16(byte[] buf, int offset = 0)
        {
            var count = 0;
            while (BitConverter.ToChar(buf, offset + count) != 0)
                count += 2;
            return Encoding.Unicode.GetString(buf, offset, count);
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

    internal enum ExtraDataSignature : uint
    {
        EnvironmentDataBlock            = 0xA0000001,
        ConsoleDataBlock                = 0xA0000002,
        TrackerDataBlock                = 0xA0000003,
        SpecialFolderDataBlock          = 0xA0000005,
        DarwinDataBlock                 = 0xA0000006,
        IconEnvironmentDataBlock        = 0xA0000007,
        ShimDataBlock                   = 0xA0000008,
        PropertyStoreDataBlock          = 0xA0000009,
        KnownFolderDataBlock            = 0xA000000B,
        VistaAndAboveIDListDataBlock    = 0xA000000C,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BaseLinkInfoHeader
    {
        public static int Size => Marshal.SizeOf<BaseLinkInfoHeader>();

        public int LinkInfoSize;
        public int LinkInfoHeaderSize;
        public LinkInfoFlags LinkInfoFlags;
        public uint VolumeIdOffset;
        public int LocalBasePathOffset;
        public int CommonNetworkRelativeLinkOffset;
        public int CommonPathSuffixOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FullLinkInfoHeader
    {
        public static int Size => Marshal.SizeOf<FullLinkInfoHeader>();

        public int LinkInfoSize;
        public int LinkInfoHeaderSize;
        public LinkInfoFlags LinkInfoFlags;
        public uint VolumeIdOffset;
        public int LocalBasePathOffset;
        public int CommonNetworkRelativeLinkOffset;
        public int CommonPathSuffixOffset;
        public int LocalBasePathOffsetUnicode;
        public int CommonPathSuffixOffsetUnicode;
    }

    [Flags]
    enum LinkInfoFlags : uint
    {
        VolumeIdAndLocalBasePath                = 1<<0,
        CommonNetworkRelativeLinkAndPathSuffix  = 1<<1,
    }
}
