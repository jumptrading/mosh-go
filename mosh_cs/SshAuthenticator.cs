using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace mosh
{
    internal static class SshAuthenticator
    {
        private static readonly Regex MoshConnectRx =
            new Regex(@"^\s*MOSH\s+CONNECT\s+(?<mosh_port>\d{1,5})\s+(?<mosh_key>\S+)\s*$");

        internal static string GetSshLocation()
        {
            //
            // See https://stackoverflow.com/a/25919981
            //

            string system32Folder = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Sysnative")
                : Environment.GetFolderPath(Environment.SpecialFolder.System);

            return Path.Combine(system32Folder, @"OpenSSH\ssh.exe");
        }

        internal static Tuple<string,string> GetMoshPortAndKey(string sshArgs, string moshPortRange)
        {
            Tuple<string, string> portAndKey = null;

            using (Process sshProcess = new Process())
            {
                sshProcess.StartInfo.FileName = GetSshLocation();
                sshProcess.StartInfo.UseShellExecute = false;
                sshProcess.StartInfo.RedirectStandardInput = false;
                sshProcess.StartInfo.RedirectStandardOutput = true;
                sshProcess.StartInfo.RedirectStandardError = false;
                sshProcess.StartInfo.Arguments = $"-T {sshArgs} \"mosh-server -p {moshPortRange}\"";
                sshProcess.Start();

                // Find the MOSH_CONNECT string from mosh-server.
                string line;
                while ((line = sshProcess.StandardOutput.ReadLine()) != null) {
                    Match match = MoshConnectRx.Match(line);
                    if (match.Success)
                    {
                        portAndKey = Tuple.Create(match.Groups["mosh_port"].Value, match.Groups["mosh_key"].Value);
                    }
                }
                sshProcess.WaitForExit();

                if (portAndKey == null)
                {
                    throw new ConnectionError("Remote server has not returned a valid MOSH CONNECT response.");
                }
                return portAndKey;
            }
        }
    }
}