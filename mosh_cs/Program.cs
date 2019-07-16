﻿using System;
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
            var moshClient = new MoshClientWrapper();

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

            var sshArgs = string.Join(" ", argList.Select(QuoteIfNeeded));
            var portAndKey = SshAuthenticator.GetMoshPortAndKey(sshArgs, moshPortRange);

            Console.Clear();

            var user = userHostMatch.Groups["user"].Value;
            var ip = HostToIP(userHostMatch.Groups["host"].Value);
            return moshClient.Start(user, ip, portAndKey.Port, portAndKey.Key);
        }

        private static IPAddress HostToIP(string host)
        {
            if (IPAddress.TryParse(host, out IPAddress ip))
            {
                return ip; // already in IP form
            }

            // Attempt DNS resolution.
            IPHostEntry hostInfo;
            hostInfo = Dns.GetHostEntry(host);
            ip = hostInfo.AddressList.FirstOrDefault();
            if (ip == null)
            {
                throw new ConnectionError($"Failed to resolve host '{host}'");
            }
            return ip;
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