using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FocusDeck.Server.Services
{
    public class VersionService
    {
        private readonly string _version;

        public VersionService()
        {
            _version = GetGitCommitHash() ?? "unknown";
        }

        public string GetVersion() => _version;

        private static string? GetGitCommitHash()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "dev"; // Not on a Linux server, return dev version
            }

            try
            {
                var repoPath = Environment.GetEnvironmentVariable("FOCUSDECK_REPO") ?? "/home/focusdeck/FocusDeck";
                if (!Directory.Exists(repoPath))
                {
                    return "no-repo";
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"-C {repoPath} rev-parse --short=7 HEAD",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                string version = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return !string.IsNullOrEmpty(version) ? version : "local";
            }
            catch
            {
                return "error";
            }
        }
    }
}
