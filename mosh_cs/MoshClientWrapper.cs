﻿using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;

namespace mosh
{
    class MoshClientNotFound : Exception
    {
        public MoshClientNotFound(string message) : base(message) { }
    }

    internal class MoshClientWrapper
    {
        private const string MoshClientAppSettingsKey = "mosh-client";
        private const string MoshClientExeName = "mosh-client.exe";

        private readonly string MoshClientExePath;

        public MoshClientWrapper()
        {
            MoshClientExePath = FindMoshClientExe();
        }

        private string FindMoshClientExe() {
            string path = GetMoshClientPathFromSettings();
            if (path != null)
            {
                return path;
            }

            // Look for mosh-client.exe next to mosh.exe.
            path = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;
            if (string.IsNullOrEmpty(path))
            {
                throw new MoshClientNotFound($"Could not determine program name while looking for {MoshClientExeName}");
            }
            path = Path.Combine(path, MoshClientExeName);
            if (!File.Exists(path)) {
                throw new MoshClientNotFound(String.Join(
                    Environment.NewLine,
                    $"{MoshClientExeName} file cannot be found. Possible solutions are:",
                    $"  - Specify full file path in appSettings section of mosh.config file, with key \"{MoshClientAppSettingsKey}\"",
                    $"  - Copy {MoshClientExeName} file (with this exact name) into the current working directory",
                    $"  - Copy {MoshClientExeName} file (with this exact name) into the same directory where current executable (mosh.exe) is."
                ));
            }
            return path;
        }

        private string GetMoshClientPathFromSettings() { 
            AppSettingsReader asr = new AppSettingsReader();

            string path;
            try
            {
                path = (string)asr.GetValue(MoshClientAppSettingsKey, typeof(string));
            }
            catch (InvalidOperationException)
            {
                return null; // key is missing or wrong type, ignore
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }
            return path;
        }

        internal int Start(string user, IPAddress host, string moshPort, string moshKey)
        {
            using (Process clientProcess = new Process())
            {
                clientProcess.StartInfo.FileName = MoshClientExePath;
                clientProcess.StartInfo.UseShellExecute = false;
                clientProcess.StartInfo.RedirectStandardInput = false;
                clientProcess.StartInfo.RedirectStandardOutput = false;
                clientProcess.StartInfo.RedirectStandardError = false;
                clientProcess.StartInfo.Arguments = $"{host} {moshPort}";
                clientProcess.StartInfo.EnvironmentVariables.Add("MOSH_KEY", moshKey);
                clientProcess.StartInfo.EnvironmentVariables.Add("MOSH_USER", user);

                clientProcess.Start();
                clientProcess.WaitForExit();
                return clientProcess.ExitCode;
            }
        }

    }
}