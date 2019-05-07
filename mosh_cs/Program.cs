using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace mosh
{
    class Program
    {
        private const string DefaultMoshPortRange = "60000:60050";

        private static readonly Regex ShouldQuoteRx = new Regex("[\\s\"]", RegexOptions.Compiled);
        private static readonly Regex MoshPortRangeRx = new Regex(@"^\s*\d{1,5}:\d{1,5}\s*");
        private static readonly Regex UserHostRx = new Regex(@"^\s*(?<user>[^\s;@]+)(;[^\s@]*)?@(?<host>[^\s:]+)\s*$");

        /// <summary>
        /// App entry point.
        /// </summary>
        /// <param name="args">
        /// Input arguments are the same as for OpenSSH (<see cref="!https://man.openbsd.org/ssh">https://man.openbsd.org/ssh</see>),
        /// with one optional argument added at the very end: mosh ports range in format <c>#####:#####</c> (i.e. <c>60000:60050</c>).
        /// If mosh ports range argument isn't specified, it will default to <c>60000:60050</c>.</param>
        /// <returns>Return codes:
        /// <list type="bullet">
        /// <item>0 - Success;</item>
        /// <item>1 - Invalid arguments;</item>
        /// <item>2 - mosh-client.exe not found;</item>
        /// <item>100 - Other exception.</item>
        /// </list>
        /// </returns>
        static int Main(string[] args)
        {
            if (string.IsNullOrEmpty(MoshClientWrapper.MoshClientExePath))
            {
                Console.Error.WriteLine("mosh-client.exe file cannot be found. Possible solutions are:");
                Console.Error.WriteLine($"  - Specify full file path in appSettings section of mosh.config file, with key \"{MoshClientWrapper.MoshClientAppSettingsKey}\";");
                Console.Error.WriteLine($"  - Copy mosh-client.exe file (with this exact name) into the current working directory;");
                Console.Error.WriteLine($"  - Copy mosh-client.exe file (with this exact name) into the same directory where current executable (mosh.exe) is.");

                WaitForEnter();

                return 2;
            }

            List<string> argList = args.ToList();

            string moshPortRange = argList.LastOrDefault();

            if (moshPortRange == null)
            {
                Console.Error.WriteLine("At least user and host have to be specified.");
                WaitForEnter();

                return 1;
            }

            if (MoshPortRangeRx.IsMatch(moshPortRange))
            {
                argList.RemoveAt(argList.Count - 1);

                moshPortRange = moshPortRange.Trim();
            }
            else
                moshPortRange = DefaultMoshPortRange;

            Match userHostMatch = argList.Select(arg => UserHostRx.Match(arg)).FirstOrDefault(m => m.Success);

            if (userHostMatch == null)
            {
                Console.Error.WriteLine("Determining user and host from the specified arguments failed.");
                WaitForEnter();

                return 1;
            }

            string sshArgs = string.Join(" ", argList.Select(QuoteIfNeeded));

            Tuple<string, string> moshPortAndKey;

            try
            {
                moshPortAndKey = SshAuthenticator.GetMoshPortAndKey(sshArgs, moshPortRange);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                WaitForEnter();

                return 100;
            }

            if (moshPortAndKey == null)
            {
                Console.Error.WriteLine("Remote server has not returned a valid MOSH CONNECT response.");
                WaitForEnter();

                return 100;
            }

            Console.Clear();

            string strHost = userHostMatch.Groups["host"].Value;

            if (!IPAddress.TryParse(strHost, out IPAddress host))
            {
                IPHostEntry hostInfo;

                try
                {
                    hostInfo = Dns.GetHostEntry(strHost);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to resolve host '{strHost}'. Exception: {ex.Message}");
                    WaitForEnter();

                    return 1;
                }

                host = hostInfo.AddressList.FirstOrDefault();

                if (host == null)
                {
                    Console.Error.WriteLine($"Failed to resolve host '{strHost}'.");
                    WaitForEnter();

                    return 1;
                }
            }

            int exitCode;

            try
            {
                exitCode = MoshClientWrapper.StartMoshSession(userHostMatch.Groups["user"].Value, host,
                    moshPortAndKey.Item1, moshPortAndKey.Item2);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                WaitForEnter();

                return 100;
            }

            if (exitCode != 0)
            {
                Console.Error.WriteLine($"mosh-client.exe exited with code {exitCode}.");
                WaitForEnter();

                return 100;
            }

            return 0;
        }

        private static string QuoteIfNeeded(string input)
        {
            if (!ShouldQuoteRx.IsMatch(input))
                return input;

            input = input.Replace("\"", "\"\"");

            return string.Concat("\"", input, "\"");
        }

        private static void WaitForEnter()
        {
            // Ensuring that user sees the error is done in a different way in release.
#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Press [Enter] to exit.");
            Console.ReadLine();
#endif
        }
    }
}
