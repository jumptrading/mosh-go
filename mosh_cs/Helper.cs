using System;
using System.Text.RegularExpressions;

namespace mosh
{
    internal static class Helper
    {
        private static readonly Regex WhiteSpaceRx = new Regex(@"\s", RegexOptions.Compiled);

        internal static string QuoteIfNeeded(this string input)
        {
            if (string.IsNullOrEmpty(input) || !WhiteSpaceRx.IsMatch(input))
            {
                // No need to quote
                return input;
            }

            if (input[0] == '"' && input[0] == input[input.Length - 1])
            {
                // Already quoted
                return input;
            }

            // TODO: Here it's assumed that input does not contain double quote character, which is OK for us (for paths and mosh-server command).
            return $"\"{input}\"";
        }

        internal static Tuple<string, string> SplitCommandAndArguments(this string commandAndArguments)
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
    }
}