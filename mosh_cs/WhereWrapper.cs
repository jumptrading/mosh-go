using System.Diagnostics;
using System.IO;

namespace mosh
{
    internal static class WhereWrapper
    {
        internal static string WhereIs(this string command)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "where";
                process.StartInfo.Arguments = command;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = false;

                process.Start();

                var path = process.StandardOutput.ReadLine();

                return process.ExitCode == 0 && !string.IsNullOrEmpty(path) && File.Exists(path) ? path : null;
            }
        }
    }
}