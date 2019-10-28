using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;

namespace mosh
{
    internal class MoshClientNotFound : Exception
    {
        public MoshClientNotFound(string message) : base(message) { }
    }

    internal static class MoshClientWrapper
    {
        private const string MoshClientAppSettingsKey = "mosh-client";
        private const string MoshClientExeName = "mosh-client.exe";

        private static string FindMoshClientExe()
        {
            // Locating mosh-client from mosh.config
            var path = GetMoshClientPathFromSettings();

            if (!string.IsNullOrEmpty(path)) return path;

            // Locating mosh-client from the current working directory

            var moshClientFileInfo = new FileInfo(MoshClientExeName);

            if (moshClientFileInfo.Exists) return moshClientFileInfo.FullName;

            // Locating mosh-client next to mosh.exe

            path = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;

            if (!string.IsNullOrEmpty(path))
            {
                moshClientFileInfo = new FileInfo(Path.Combine(path, MoshClientExeName));

                if (moshClientFileInfo.Exists) return moshClientFileInfo.FullName;
            }

            // Locating mosh-client in the path

            path = MoshClientExeName.WhereIs();

            if (!string.IsNullOrEmpty(path)) return path;

            throw new MoshClientNotFound(string.Join(
                "\n",
                $"{MoshClientExeName} file cannot be found. Possible solutions are:",
                $"  - Specify full file path in appSettings section of mosh.config file, with key \"{MoshClientAppSettingsKey}\";",
                $"  - Copy {MoshClientExeName} file (with this exact name) into the current working directory;",
                $"  - Copy {MoshClientExeName} file (with this exact name) into the same directory where current executable (mosh.exe) is;",
                $"  - Add {MoshClientExeName} to path;",
                "  - Use '-command=' parameter to specify the full command path."
            ));
        }

        private static string GetMoshClientPathFromSettings()
        {
            var asr = new AppSettingsReader();

            string path;

            try
            {
                path = (string) asr.GetValue(MoshClientAppSettingsKey, typeof(string));
            }
            catch (InvalidOperationException)
            {
                return null; // key is missing or wrong type, ignore
            }

            return string.IsNullOrEmpty(path) || !File.Exists(path) ? null : path;
        }

        internal static int Start(string moshClient, IPAddress host, string moshPort, StringDictionary environmentVariables)
        {
            if (string.IsNullOrEmpty(moshClient))
            {
                moshClient = FindMoshClientExe();
            }

            using (var clientProcess = new Process())
            {
                clientProcess.StartInfo.FileName = moshClient;
                clientProcess.StartInfo.UseShellExecute = false;
                clientProcess.StartInfo.RedirectStandardInput = false;
                clientProcess.StartInfo.RedirectStandardOutput = false;
                clientProcess.StartInfo.RedirectStandardError = false;
                clientProcess.StartInfo.Arguments = $"{host} {moshPort}";

                foreach (string environmentVariable in environmentVariables.Keys)
                {
                    clientProcess.StartInfo.EnvironmentVariables[environmentVariable] =
                        environmentVariables[environmentVariable];
                }

                clientProcess.Start();

                clientProcess.WaitForExit();

                return clientProcess.ExitCode;
            }
        }
    }
}