using System.Runtime.InteropServices;
using System.Text.Json;
using static GttrcrGist.Process;
using static GitSync.Operations;
using static GttrcrGist.MutexConsole;

namespace GitSync
{
    public class Program
    {
        private static void InitialChecks(string[] args, out string configFile)
        {
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

        public static void Main(string[] args)
        {
            try
            {
                InitialChecks(args, out string configFile);
                ResetScreen();
                Config? orgs = JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile));
                if (orgs == null)
                    throw new Exception("null organization file");

                for (int i = 0; i < orgs.Value.Repos.Count; i++)
                {
                    string organization = orgs.Value.Repos.ElementAt(i).Organization;
                    List<string> repos = orgs.Value.Repos.ElementAt(i).Repos;
                    if (repos.Count == 1 && repos[0].Equals("*"))
                        orgs.Value.Repos[i] = new()
                        {
                            Organization = organization,
                            Repos = new List<string>(Run(new() { new()
                            {
                                OSPlatform = OSPlatform.Linux, Command = "gh repo list " + organization + " | awk '{ print $1 }'"  // command for linux
                            } }))
                        };
                }

                int line = 0;
                Parallel.ForEach(orgs.Value.Repos, new ParallelOptions { MaxDegreeOfParallelism = 2 }, x => Parallel.ForEach(x.Repos, new ParallelOptions { MaxDegreeOfParallelism = 3 }, y => UpdateRepo(x.Organization, y, orgs.Value.Path, line++, false, 5, args.Length == 2 && args[1].Equals("push"))));
                MutexConsoleWriteLine("Done" + Environment.NewLine, line);
                ResetScreen();

                //Last messages
                if (SomeDiff == null || SomeDiff.Count == 0)
                    MutexConsoleWriteLine("Everything is up to date", null, ConsoleColor.Green);
                else
                    MutexConsoleWriteLine("The following repos need attention", null, ConsoleColor.Yellow);
                SomeDiff?.ForEach(x => MutexConsoleWriteLine(x.Organization + "\t" + x.Name + "\t" + x.Path));

                if (UnableToComplete != null && UnableToComplete.Count != 0)
                    MutexConsoleWriteLine("The following repos are in error", null, ConsoleColor.Red);
                UnableToComplete?.ForEach(x => MutexConsoleWriteLine(x.Organization + "\t" + x.Name + "\t" + x.Path));

                orgs.Value.Repos.ForEach(x =>
                {
                    List<string> dirs = Directory.GetDirectories(Path.Combine(orgs.Value.Path, x.Organization)).ToList();
                    List<string> expectedFolders = x.Repos.Select(x => Path.Combine(orgs.Value.Path, x)).ToList();
                    List<string> diffFolders = dirs.Except(expectedFolders).ToList();
                    if (diffFolders.Count > 0)
                    {
                        MutexConsoleWriteLine("Furthermore, these folders are not repositories of " + x.Organization, null, ConsoleColor.Yellow);
                        diffFolders?.ForEach(x => MutexConsoleWriteLine(x));
                    }
                });
            }
            catch (Exception ex)
            {
                MutexConsoleWriteLine(ex.Message);
            }
        }
    }
}