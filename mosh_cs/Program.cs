using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace mosh
{

    class InvalidArgsException : Exception
    {
        public InvalidArgsException(string message) : base(message) { }
    }

    class ConnectionError : Exception
    {
        public ConnectionError(string message) : base(message) { }
    }

    enum ExitCode {
        InvalidArgs = 254,
        ConnectionError = 255,
    }

    class Program
    {
        private const string DefaultMoshPortRange = "60000:60999";

        private static readonly Regex ShouldQuoteRx = new Regex("[\\s\"]", RegexOptions.Compiled);
        private static readonly Regex MoshPortRangeRx = new Regex(@"^\s*\d{1,5}:\d{1,5}\s*");
        private static readonly Regex UserHostRx = new Regex(@"^\s*(?<user>[^\s;@]+)(;[^\s@]*)?@(?<host>[^\s:]+)\s*$");

        /// <summary>
        /// App entry point.
        /// </summary>
        /// <param name="args">
        /// Input arguments are the same as for OpenSSH (<see cref="!https://man.openbsd.org/ssh">https://man.openbsd.org/ssh</see>),
        /// with one optional argument added at the very end: mosh ports range in format <c>#####:#####</c> (i.e. <c>60000:60050</c>).
        /// If mosh ports range argument isn't specified, it will default to <c>60000:60099</c>.</param>
        /// <returns>Return codes:
        /// <list type="bullet">
        /// <item>254 - Invalid command line arguments.</item>
        /// <item>255 - Initial SSH or Mosh connection setup failed.</item>
        /// <item>All other values - Exit code returned by remote shell.</item>
        /// </list>
        /// </returns>
        static int Main(string[] args)
        {
            try
            {
                return Run(args);
            } catch (InvalidArgsException e) {
                Console.Error.Write(e.Message);
                return (int)ExitCode.InvalidArgs;
            } catch (Exception e)
            {
                Console.Error.Write(e.Message);
                return (int)ExitCode.ConnectionError;
            }
        }

        static int Run(string[] args)
        {
            MoshClientWrapper moshClient;
            try
            {
                moshClient = new MoshClientWrapper();
            } catch (MoshClientNotFound) { 
                throw new ConnectionError(String.Join(
                    Environment.NewLine,
                    "mosh-client.exe file cannot be found. Possible solutions are:",
                    $"  - Specify full file path in appSettings section of mosh.config file, with key \"{MoshClientWrapper.MoshClientAppSettingsKey}\"",
                    $"  - Copy mosh-client.exe file (with this exact name) into the current working directory",
                    $"  - Copy mosh-client.exe file (with this exact name) into the same directory where current executable (mosh.exe) is."
                ));
            }

            List<string> argList = args.ToList();

            string moshPortRange = argList.LastOrDefault();
            if (moshPortRange == null)
            {
                throw new InvalidArgsException("At least user and host have to be specified.");
            }

            if (MoshPortRangeRx.IsMatch(moshPortRange))
            {
                argList.RemoveAt(argList.Count - 1);
                moshPortRange = moshPortRange.Trim();
            }
            else
            {
                moshPortRange = DefaultMoshPortRange;
            }

            Match userHostMatch = argList.Select(arg => UserHostRx.Match(arg)).FirstOrDefault(m => m.Success);
            if (userHostMatch == null)
            {
                throw new InvalidArgsException("Determining user and host from the specified arguments failed");
            }

            string sshArgs = string.Join(" ", argList.Select(QuoteIfNeeded));

            var moshPortAndKey = SshAuthenticator.GetMoshPortAndKey(sshArgs, moshPortRange);
            if (moshPortAndKey == null)
            {
                throw new ConnectionError("Remote server has not returned a valid MOSH CONNECT response.");
            }

            Console.Clear();

            string strHost = userHostMatch.Groups["host"].Value;
            if (!IPAddress.TryParse(strHost, out IPAddress host))
            {
                IPHostEntry hostInfo;
                hostInfo = Dns.GetHostEntry(strHost);
                host = hostInfo.AddressList.FirstOrDefault();
                if (host == null)
                {
                    throw new ConnectionError($"Failed to resolve host '{strHost}'.");
                }
            }

            return moshClient.Start(userHostMatch.Groups["user"].Value, host,
                    moshPortAndKey.Item1, moshPortAndKey.Item2);
        }


        private static string QuoteIfNeeded(string input)
        {
            if (!ShouldQuoteRx.IsMatch(input))
                return input;

            input = input.Replace("\"", "\"\"");

            return string.Concat("\"", input, "\"");
        }
    }
}
