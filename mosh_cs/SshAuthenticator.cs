using System.Diagnostics;
using System.Text.RegularExpressions;

namespace mosh
{
    internal class PortKeyPair
    {
        internal string Port { get; }

        internal string Key { get; }

        internal PortKeyPair(string port, string key)
        {
            Port = port;
            Key = key;
        }
    } 

    internal static class SshAuthenticator
    {
        private static readonly Regex MoshConnectRx =
            new Regex(@"^\s*MOSH\s+CONNECT\s+(?<mosh_port>\d{1,5})\s+(?<mosh_key>\S+)\s*$");

        internal static PortKeyPair GetMoshPortAndKey(string sshCommand, string sshArguments)
        {
            PortKeyPair portAndKey = null;

            using (var sshProcess = new Process())
            {
                sshProcess.StartInfo.FileName = sshCommand;
                sshProcess.StartInfo.Arguments = sshArguments;
                sshProcess.StartInfo.UseShellExecute = false;
                sshProcess.StartInfo.RedirectStandardInput = false;
                sshProcess.StartInfo.RedirectStandardOutput = true;
                sshProcess.StartInfo.RedirectStandardError = true;
                sshProcess.Start();

                // Find the MOSH_CONNECT string from mosh-server.
                string line;

                while ((line = sshProcess.StandardOutput.ReadLine()) != null) 
                {
                    var match = MoshConnectRx.Match(line);

                    if (match.Success)
                    {
                        portAndKey = new PortKeyPair(match.Groups["mosh_port"].Value, match.Groups["mosh_key"].Value);

                        break;
                    }
                }

                return portAndKey ?? throw new ConnectionError("Remote server has not returned a valid MOSH CONNECT response.");
            }
        }
    }
}