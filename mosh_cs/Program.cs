using System;
using System.Collections.Specialized;
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

    enum ExitCode 
    {
        InvalidArgs = 254,
        ConnectionError = 255,
    }

    class Program
    {
        private static readonly Regex UserHostRx = new Regex(@"^(?<user>[^\s@]+)@(?<host>[^@\s]+)$");

        private class Args
        {
            internal string Client { get; set; }

            internal string Server { get; set; }

            internal string SshArgs { get; set; }

            internal string Predict { get; set; }

            internal string MoshPortRange { get; set; } = "60000:61000";

            internal bool NoInit { get; set; }

            internal string User { get; set; }

            internal string Host { get; set; }
        }

        static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception ex)
            {
                if (ex is OptionException || ex is InvalidArgsException)
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
            var args = ParseArgs(rawArgs);

            string sshCommand;
            string sshArguments;

            if (string.IsNullOrEmpty(args.SshArgs))
            {
                sshCommand = "ssh";
                sshArguments = string.Empty;
            }
            else
            {
                var commandAndArguments = args.SshArgs.SplitCommandAndArguments();

                sshCommand = commandAndArguments.Item1;
                sshArguments = commandAndArguments.Item2;
            }

            sshArguments +=
                $" {args.User}@{args.Host} \"{(string.IsNullOrEmpty(args.Server) ? "mosh-server new" : args.Server)} -p {args.MoshPortRange}\"";

            var portAndKey = SshAuthenticator.GetMoshPortAndKey(sshCommand, sshArguments.Trim());

            Console.Clear();

            var environmentVariables = new StringDictionary {{"MOSH_USER", args.User}, {"MOSH_KEY", portAndKey.Key}};

            if (!string.IsNullOrEmpty(args.Predict))
            {
                environmentVariables["MOSH_PREDICTION_DISPLAY"] = args.Predict;
            }

            if (args.NoInit)
            {
                environmentVariables["MOSH_NO_TERM_INIT"] = "1";
            }

            var ip = HostToIp(args.Host);

            return MoshClientWrapper.Start(args.Client, ip, portAndKey.Port, environmentVariables);
        }

        static Args ParseArgs(string []rawArgs)
        {
            var args = new Args();

            var showHelp = false;
            var p = new OptionSet
            {
                {
                    "client=", 
                    "Path to client helper on local machine (default: \"mosh-client\")", 
                    v => args.Client = v
                },
                {
                    "server=",
                    "Command to run server helper on remote machine (default: \"mosh-server\").\n\tExample: '--server=\"mosh-server new -v -c 256\"'.\n\tSee https://linux.die.net/man/1/mosh-server for more details.",
                    v => args.Server = v
                },
                {
                    "ssh=",
                    "OpenSSH command to remotely execute mosh-server on remote machine (default: \"ssh\").\n\tExample: ''--ssh=\"ssh -p 2222\"'.\n\tSee https://man.openbsd.org/ssh for more details.",
                    v => args.SshArgs = v
                },
                {
                    "predict=",
                    "Controls use of speculative local echo. Defaults to 'adaptive' (show predictions on slower links and to smooth out network glitches) and can also be 'always' or 'never'.",
                    v => args.Predict = v
                },
                {
                    "a", 
                    "Synonym 'for --predict=always'.",
                    v =>
                    {
                        if (v != null) args.Predict = "always";
                    }
                },
                {
                    "n", 
                    "Synonym 'for --predict=never'.",
                    v =>
                    {
                        if (v != null)  args.Predict = "never";
                    }
                },
                {
                    "p|port=", 
                    "Use a particular server-side UDP port or port range, for example, if this is the only port that is forwarded through a firewall to the server. Otherwise, mosh will choose a port between 60000 and 61000.\n\tExample: '--port=60000:60100'", 
                    v => args.MoshPortRange = v
                },
                {
                    "help",
                    "Show help.",
                    v => showHelp = v != null
                },
                {
                    "no-init",
                    "Do not send the smcup initialization string and rmcup deinitialization string to the client's terminal. On many terminals this disables alternate screen mode.",
                    v => args.NoInit = v != null
                }
            };

            var extraArgs = p.Parse(rawArgs);

            if (showHelp) ShowHelp(p);

            if (extraArgs.Count < 1) throw new InvalidArgsException("Missing target ([user@]host).");
            if (extraArgs.Count > 1) throw new InvalidArgsException("Unexpected extra arguments.");

            var targetMatch = UserHostRx.Match(extraArgs[0]);

            if (!targetMatch.Success) throw new InvalidArgsException("Target ([user@]host) is invalid");

            args.User = targetMatch.Groups["user"].Value;
            args.Host = targetMatch.Groups["host"].Value;

            return args;
        }


        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: mosh [options] [user@]host");
            Console.WriteLine();
            p.WriteOptionDescriptions(Console.Out); 
            Console.WriteLine();
            Console.WriteLine("Exit codes:");
            Console.WriteLine("  254 - Invalid command line arguments");
            Console.WriteLine("  255 - Initial SSH of Mosh connection setup failed");
            Console.WriteLine("  All other values - Exit code returned by remote shell");

            Console.WriteLine();
            Console.WriteLine("Press [Enter] to exit...");
            Console.ReadLine();

            Environment.Exit(0);
        }

        private static IPAddress HostToIp(string host)
        {
            if (IPAddress.TryParse(host, out var ip))
            {
                return ip; // already in IP form
            }

            // Attempt DNS resolution.
            var hostInfo = Dns.GetHostEntry(host);

            ip = hostInfo.AddressList.FirstOrDefault();

            return ip ?? throw new ConnectionError($"Failed to resolve host '{host}'.");
        }
    }
}
