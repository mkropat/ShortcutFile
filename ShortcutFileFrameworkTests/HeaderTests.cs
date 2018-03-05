using ShortcutFile;
using System;
using System.IO;
using Xunit;

namespace ShortcutFileFrameworkTests
{
    public class HeaderTests
    {
        ShortcutFileParser subject = new ShortcutFileParser();

        [Fact]
        public void ItThrowsArgumentException_WhenStreamIsEmpty()
        {
            using (var s = new MemoryStream())
            {
                var exception = Assert.Throws<InvalidShortcutFileException>(() => subject.Parse(s));
                Assert.Equal("Unable to read header", exception.Message);
            }
        }

        [Fact]
        public void ItThrowsArgumentException_WhenSizeIsLessThanLinkHeaderSize()
        {
            using (var s = new MemoryStream(new byte[0x4c - 1]))
            {
                var exception = Assert.Throws<InvalidShortcutFileException>(() => subject.Parse(s));
                Assert.Equal("Unable to read header", exception.Message);
            }
        }

        [Fact]
        public void ItThrowsArgumentException_WhenHeaderSizeIsNot4c()
        {
            var headerSize = new byte[] { 0x42, 0, 0, 0 };
            var rest = new byte[0x4c - headerSize.Length];
            using (var s = CreateStream(headerSize, rest))
            {
                var exception = Assert.Throws<InvalidShortcutFileException>(() => subject.Parse(s));
                Assert.Equal("Unexpected header size", exception.Message);
            }
        }

        [Fact]
        public void ItThrowsArgumentException_WhenLinkClsidIsWrong()
        {
            var headerSize = new byte[] { 0x4c, 0, 0, 0 };
            var linkClsid = Guid.NewGuid().ToByteArray();
            var rest = new byte[0x4c - headerSize.Length - linkClsid.Length];
            using (var s = CreateStream(headerSize, linkClsid, rest))
            {
                var exception = Assert.Throws<InvalidShortcutFileException>(() => subject.Parse(s));
                Assert.Equal("Unexpected header magic value", exception.Message);
            }
        }

        [Fact]
        public void ItDoesntThrowException_WhenHeaderOfSufficientSizeIsPresent()
        {
            var headerSize = new byte[] { 0x4c, 0, 0, 0 };
            var linkClsid = Guid.Parse("00021401-0000-0000-C000-000000000046").ToByteArray();
            var rest = new byte[0x4c - headerSize.Length - linkClsid.Length];
            var terminal = new byte[] { 0, 0, 0, 0 };
            using (var s = CreateStream(headerSize, linkClsid, rest, terminal))
            {
                subject.Parse(s);
            }
        }

        Stream CreateStream(params byte[][] buffers)
        {
            var s = new MemoryStream(capacity: 0x4c);
            foreach (var buf in buffers)
                s.Write(buf, 0, buf.Length);
            s.Seek(0, SeekOrigin.Begin);
            return s;
        }
    }
}