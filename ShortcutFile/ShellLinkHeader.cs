using System;
using System.Runtime.InteropServices;

namespace ShortcutFile
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    struct ShellLinkHeader
    {
        public static int Size => Marshal.SizeOf<ShellLinkHeader>();

        public int HeaderSize;
        public Guid LinkClsid;
        public LinkFlag LinkFlags;
        public uint FileAttributes;
        public ulong CreationTime;
        public ulong AccessTime;
        public ulong WriteTime;
        public int FileSize;
        public int IconIndex;
        public uint ShowCommand;
        public ushort HotKey;
        public ushort Reserved1;
        public uint Reserved2;
        public uint Reserved3;
    }

    [Flags]
    public enum LinkFlag : uint
    {
        HasLinkTargetIdList         = 1<<0,
        HasLinkInfo                 = 1<<1,
        HasName                     = 1<<2,
        HasRelativePath             = 1<<3,
        HasWorkingDir               = 1<<4,
        HasArguments                = 1<<5,
        HasIconLocation             = 1<<6,
        IsUnicode                   = 1<<7,
        ForceNoLinkInfo             = 1<<8,
        HasExpString                = 1<<9,
        RunInSeparateProcess        = 1<<10,
        // Unused1
        HasDarwinId                 = 1<<12,
        RunAsUser                   = 1<<13,
        HasExpIcon                  = 1<<14,
        NoPidlAlias                 = 1<<15,
        // Unused2
        RunWithShimLayer            = 1<<17,
        ForceNoLinkTrack            = 1<<18,
        EnableTargetMetadata        = 1<<19,
        DisableLinkPathTracking     = 1<<20,
        DisableKnownFolderTracking  = 1<<21,
        DisableKnownFolderAlias     = 1<<22,
        AllowLinkToLink             = 1<<23,
        UnaliasOnSave               = 1<<24,
        PreferEnvironmentPath       = 1<<25,
        KeepLocalIdListForUncTarget = 1<<26,
    }
}
