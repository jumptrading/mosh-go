using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace mosh
{
    internal static class CommandHelper
    {
        private static readonly Regex WhiteSpaceRx = new Regex(@"\s", RegexOptions.Compiled);

        internal static Tuple<string, string> SplitCommandAndArguments(string commandAndArguments)
        {
            commandAndArguments = commandAndArguments?.Trim();

            if (commandAndArguments == null || commandAndArguments.Length < 3)
            {
                // Either null or too short to split
                return Tuple.Create(commandAndArguments, string.Empty);
            }

            var whiteSpaceMatch = WhiteSpaceRx.Match(commandAndArguments);

            if (!whiteSpaceMatch.Success)
            {
                // No white spaces, so nothing to split
                return Tuple.Create(commandAndArguments, string.Empty);
            }

            if (commandAndArguments[0] == '\'' || commandAndArguments[0] == '"')
            {
                var idx = commandAndArguments.IndexOf(commandAndArguments[0], 1);

                if (idx < 2)
                {
                    throw new InvalidArgsException($"Invalid command string: {commandAndArguments}");
                }

                var command = commandAndArguments.Substring(1, commandAndArguments.Length - 2).Trim();

                if (idx == commandAndArguments.Length - 1)
                {
                    // No arguments
                    return Tuple.Create(command, string.Empty);
                }

                whiteSpaceMatch = WhiteSpaceRx.Match(commandAndArguments, idx + 1);

                if (!whiteSpaceMatch.Success || whiteSpaceMatch.Index != idx + 1)
                {
                    // Command quote isn't followed by a space
                    throw new InvalidArgsException($"Invalid command string: {commandAndArguments}");
                }

                return Tuple.Create(command, commandAndArguments.Substring(idx + 1).Trim());
            }

            return Tuple.Create(commandAndArguments.Substring(0, whiteSpaceMatch.Index).Trim(),
                commandAndArguments.Substring(whiteSpaceMatch.Index + 1).Trim());
        }

        internal static string FindCommandInThePath(string command)
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

                process.WaitForExit();

                return process.ExitCode == 0 && !string.IsNullOrEmpty(path) && File.Exists(path) ? path : null;
            }
        }
    }
}