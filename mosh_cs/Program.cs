using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using NDesk.Options;

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

        private static readonly Regex MoshPortRangeRx = new Regex(@"^\d{1,5}(:\d{1,5})?$");
        private static readonly Regex UserHostRx = new Regex(@"^(?:(?<user>[^\s;@]+)(?:;[^\s@]*)?@)?(?<host>[^@\s:]+)$");

        private class Args
        {
            public string MoshPortRange;
            public string SshArgs;
            public string User;
            public string Host;

            internal void SetTarget(string target)
            {
                var match = UserHostRx.Match(target);
                if (!match.Success)
                {
                    throw new InvalidArgsException("target ([user@]host) is invalid");
                }
                User = match.Groups["user"].Value;
                Host = match.Groups["host"].Value;
                SshArgs = (SshArgs + " " + target).Trim();
            }

        }

        static int Main(string[] args)
        {
            try
            {
                return Run(args);
            } catch (Exception ex)
            {
                if (ex is OptionException || 
                    ex is InvalidArgsException)
                {
                    Console.Error.Write(ex.Message + " (try --help)");
                    return (int)ExitCode.InvalidArgs;
                }
                Console.Error.Write(ex.Message);
                return (int)ExitCode.ConnectionError;
            }
        }

        static int Run(string[] rawArgs)
        {
            var moshClient = new MoshClientWrapper();
            var args = ParseArgs(rawArgs);

            var portAndKey = SshAuthenticator.GetMoshPortAndKey(args.SshArgs, args.MoshPortRange);

            Console.Clear();

            var ip = HostToIP(args.Host);
            return moshClient.Start(args.User, ip, portAndKey.Port, portAndKey.Key);
        }

        static Args ParseArgs(string []rawArgs)
        {
            var args = new Args
            {
                MoshPortRange = DefaultMoshPortRange
            };

            var showHelp = false;
            var p = new OptionSet() {
                { "p|port=", "server-side UDP port or {RANGE} (e.g. '60001:60005')", v => args.MoshPortRange = v },
                { "ssh=", "ssh options to pass through when setting up session (e.g. '-p 2222')", v => args.SshArgs = v },
                { "help", "Show help", v => showHelp = v != null },
            };

            List<string> extraArgs;
            extraArgs = p.Parse(rawArgs);

            if (showHelp) ShowHelp(p);

            if (!MoshPortRangeRx.IsMatch(args.MoshPortRange))
            {
                throw new InvalidArgsException("invalid mosh port range - expected PORT or PORT[:PORT2]");
            }

            if (extraArgs.Count < 1) throw new InvalidArgsException("missing target ([user@]host)");
            if (extraArgs.Count > 1) throw new InvalidArgsException("unexpected extra arguments");
            args.SetTarget(extraArgs[0]);

            return args;
        }


        private static void ShowHelp(OptionSet p)
        {
            Console.Error.WriteLine("Usage: mosh [options] [user@]host");
            Console.Error.WriteLine();
            p.WriteOptionDescriptions(Console.Out); 
            Console.Error.WriteLine();
            Console.Error.WriteLine("Exit codes:");
            Console.Error.WriteLine("  254 - Invalid command line arguments");
            Console.Error.WriteLine("  255 - Initial SSH of Mosh connection setup failed");
            Console.Error.WriteLine("  All other values - Exit code returned by remote shell");
            System.Environment.Exit(0);
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
    }
}
