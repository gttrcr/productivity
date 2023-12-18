using System.Runtime.InteropServices;
using System.Text.Json;
using static Process.Process;
using static GitSync.Operations;

namespace GitSync
{
    public class Program
    {
        private static void InitialChecks(string[] args, out string configFile)
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            if (!Exists("git"))
                throw new Exception("git command must be installed");

            if (!Exists("gh"))
                throw new Exception("gh command must be installed");

            if (args.Length == 0)
                throw new Exception("No config file passed");

            if (args.Length > 0 && !File.Exists(args[0]))
                throw new Exception("Cannot find configuration file " + args[0]);

            if (args.Length > 1)
                MutexConsoleWriteLine("Skipped parameters after the first");

            configFile = args[0];
        }

        private static readonly Mutex ConsoleWriteMutex = new();
        private static readonly List<int> Xs = new();
        private static int LineOffset = 0;
        public static void MutexConsoleWriteLine(string text, int? y = null, ConsoleColor color = ConsoleColor.White, bool zeroLeft = false)
        {
            ConsoleWriteMutex.WaitOne();
            int left = 0;
            if (y != null)
            {
                if (y >= Xs.Count)
                    Xs.AddRange(Enumerable.Repeat(0, y.Value - Xs.Count + 1));

                if (zeroLeft)
                {
                    left = 0;
                    Xs[y.Value] = text.Length;
                }
                else
                {
                    left = Xs[y.Value];
                    Xs[y.Value] += text.Length;
                }
            }

            Console.SetCursorPosition(left, (y ?? 0) + (y == null ? LineOffset++ : LineOffset));
            ConsoleColor tmp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = tmp;
            ConsoleWriteMutex.ReleaseMutex();
        }

        public static void Main(string[] args)
        {
            try
            {
                InitialChecks(args, out string configFile);
                GitSyncConfig? orgs = JsonSerializer.Deserialize<GitSyncConfig>(File.ReadAllText(configFile));
                if (orgs == null)
                    throw new Exception("null organization file");

                for (int i = 0; i < orgs.Value.Repo.Count; i++)
                {
                    string organization = orgs.Value.Repo.ElementAt(i).Organization;
                    List<string> repos = orgs.Value.Repo.ElementAt(i).Repo;
                    if (repos.Count == 1 && repos[0].Equals("*"))
                        orgs.Value.Repo[i] = new()
                        {
                            Organization = organization,
                            Repo = new List<string>(Run(new() { new()
                            {
                                OSPlatform = OSPlatform.Linux, Command = "gh repo list " + organization + " | awk '{ print $1 }'"  // command for linux
                            } }))
                        };
                }

                int line = 0;
                Parallel.ForEach(orgs.Value.Repo, x => Parallel.ForEach(x.Repo, y => UpdateRepo(x.Organization, y, orgs.Value.Path, line++)));
                MutexConsoleWriteLine("Done" + Environment.NewLine, line);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}