using IWshRuntimeLibrary;
using ShortcutFile;
using System;
using System.IO;
using Xunit;

namespace ShortcutFileFrameworkTests
{
    public class ReadTests
    {
        [Fact]
        public void CreateShortcutDoesntError()
        {
            var link = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".lnk");
            var shell = new WshShell();
            var shortcut = (IWshShortcut)shell.CreateShortcut(link);
            shortcut.TargetPath = Path.GetFullPath("foo.txt");

            shortcut.Save();

            System.IO.File.Delete(link);
        }

        [Fact]
        public void ParseReturnsTarget()
        {
            var link = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".lnk");
            var shell = new WshShell();
            var shortcut = (IWshShortcut)shell.CreateShortcut(link);
            //shortcut.Hotkey = "ALT+CTRL+SHIFT+Q";
            shortcut.TargetPath = Path.Combine(Path.GetTempPath(), "foo.txt");

            shortcut.Save();

            var subject = new ShortcutFileParser();
            using (var f = System.IO.File.OpenRead(link))
            {
                var actual = subject.Parse(f);
                Assert.Equal(@".\foo.txt", actual.RelativePath);
            }

            System.IO.File.Delete(link);
        }

        [Fact]
        public void ParseReturnsTargetToNetworkShare()
        {
            var link = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".lnk");
            var shell = new WshShell();
            var shortcut = (IWshShortcut)shell.CreateShortcut(link);
            //shortcut.Hotkey = "ALT+CTRL+SHIFT+Q";
            shortcut.TargetPath = @"\\127.0.0.1\someshare\foo.txt";

            shortcut.Save();

            var subject = new ShortcutFileParser();
            using (var f = System.IO.File.OpenRead(link))
            {
                var actual = subject.Parse(f);
                Assert.Equal(@"\\127.0.0.1\someshare\foo.txt", actual.EnvironmentVariable);
            }

            System.IO.File.Delete(link);
        }
    }
}
