using System.Runtime.InteropServices;
using System.Text.Json;
using static GttrcrGist.Process;
using static GitSync.Operations;
using GttrcrGist;

namespace GitSync
{
    public class Program
    {
        private static void InitialChecks(string[] args, out string configFile, out string? action)
        {
            if (!Exists("git"))
                throw new Exception("git command must be installed");

            if (!Exists("gh"))
                throw new Exception("gh command must be installed");

            if (args.Length == 0)
                throw new Exception("No config file passed");

            if (args.Length > 0 && !File.Exists(args[0]))
                throw new Exception("Cannot find configuration file " + args[0]);

            action = null;
            if (args.Length == 2)
            {
                if (args[1].Equals("push"))
                    action = args[1];
                else
                    throw new NotImplementedException("Action " + args[1] + " is not implemented");
            }

            configFile = args[0];
        }

        public static void Main(string[] args)
        {
            try
            {
                InitialChecks(args, out string configFile, out string? action);
                MutexConsole.Clear();
                Config? orgs = JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile));
                if (orgs == null)
                    throw new Exception("null organization file");

                for (int i = 0; i < orgs.Value.Repos.Count; i++)
                {
                    string organization = orgs.Value.Repos.ElementAt(i).Organization;
                    List<string> repos = orgs.Value.Repos.ElementAt(i).Repos;
                    List<string> remoteReposList = new(Run([new() { OSPlatform = OSPlatform.Linux, Command = "gh repo list " + organization + " | awk '{ print $1 }'" }]).SelectMany(x => x));

                    if (remoteReposList.Count == 0)
                    {
                        MutexConsole.WriteLine("No remote repos for organization " + organization, null, ConsoleColor.Yellow);
                        continue;
                    }

                    remoteReposList = remoteReposList.Select(x => x.Split('/').Last()).ToList();

                    if (repos.Contains("*"))
                        orgs.Value.Repos[i] = new() { Organization = organization, Repos = new(remoteReposList) };
                    else
                    {
                        List<string> mismatchRepos = repos.Except(remoteReposList).ToList();
                        mismatchRepos.ForEach(x => MutexConsole.WriteLine("Remote repo " + x + " in organization " + organization + " does not exists", null, ConsoleColor.Yellow));
                        orgs.Value.Repos[i] = new() { Organization = organization, Repos = new(repos.Except(mismatchRepos)) };
                    }

                    if (repos.Any(x => x.StartsWith('-')))
                    {
                        List<string> toRemove = repos.Where(x => x.StartsWith('-')).Select(x => x.Substring(1, x.Length - 1)).ToList();
                        orgs.Value.Repos[i] = new() { Organization = organization, Repos = new(orgs.Value.Repos[i].Repos.Where(x => !toRemove.Contains(x))) };
                    }
                }

                int line = 0;
                Parallel.ForEach(orgs.Value.Repos, new ParallelOptions { MaxDegreeOfParallelism = 2 }, x => Parallel.ForEach(x.Repos, new ParallelOptions { MaxDegreeOfParallelism = 3 }, y => UpdateRepo(x.Organization, y, orgs.Value.Path, line++, false, 5, action)));
                MutexConsole.WriteLine("Done", line);
                MutexConsole.Clear();

                //Attention repositories
                if (SomeDiff == null || SomeDiff.Count == 0)
                    MutexConsole.WriteLine("Everything is up to date", null, ConsoleColor.Green);
                else
                    MutexConsole.WriteLine("The following repos need attention", null, ConsoleColor.Yellow);
                SomeDiff?.OrderBy(x => x.Organization).ToList().ForEach(x => MutexConsole.WriteLine(x.Organization + "\t" + x.Name + "\t" + x.Path));

                //Error repositories
                if (UnableToComplete != null && UnableToComplete.Count != 0)
                    MutexConsole.WriteLine("The following repos are in error", null, ConsoleColor.Red);
                UnableToComplete?.OrderBy(x => x.Organization).ToList().ForEach(x => MutexConsole.WriteLine(x.Organization + "\t" + x.Name + "\t" + x.Path));

                //Not found repositories on remote
                orgs.Value.Repos.ForEach(x =>
                {
                    List<string> dirs = Directory.GetDirectories(Path.Combine(orgs.Value.Path, x.Organization)).ToList();
                    List<string> expectedFolders = x.Repos.Select(y => Path.Combine(orgs.Value.Path, x.Organization, y)).ToList();
                    List<string> mismatchFolders = dirs.Except(expectedFolders).ToList();
                    if (mismatchFolders.Count > 0)
                    {
                        MutexConsole.WriteLine("Furthermore, these folders are not remote repositories of " + x.Organization, null, ConsoleColor.Yellow);
                        mismatchFolders?.ForEach(x => MutexConsole.WriteLine(x));
                    }
                });
            }
            catch (Exception ex)
            {
                MutexConsole.WriteLine(ex.Message, null, ConsoleColor.Red);
            }
        }
    }
}