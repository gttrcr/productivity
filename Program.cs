using System.Runtime.InteropServices;
using System.Text.Json;
using static GttrcrGist.Process;
using static GitSync.Operations;
using GttrcrGist;
using System.Globalization;

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

            if (!CheckForInternetConnection())
                throw new Exception("No internet connection");
        }

        public static bool CheckForInternetConnection(int timeoutMs = 10000, string? url = null)
        {
            try
            {
                url ??= CultureInfo.InstalledUICulture switch
                {
                    { Name: var n } when n.StartsWith("fa") => // Iran
                        "http://www.aparat.com",
                    { Name: var n } when n.StartsWith("zh") => // China
                        "http://www.baidu.com",
                    _ =>
                        "http://www.gstatic.com/generate_204",
                };

                new HttpClient().GetAsync(url).Result.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                MutexConsole.Clear();
                InitialChecks(args, out string configFile, out string? action);
                Config? orgs = JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile));
                if (orgs == null)
                    throw new Exception("null organization file");

                for (int i = 0; i < orgs.Value.Organizations.Count; i++)
                {
                    string organization = orgs.Value.Organizations.ElementAt(i).Organization;
                    List<string> repos = orgs.Value.Organizations.ElementAt(i).Repos;
                    List<string> remoteReposList = new(Run([new() { OSPlatform = OSPlatform.Linux, Command = "gh repo list " + organization + " | awk '{ print $1 }'" }]).SelectMany(x => x));

                    if (remoteReposList.Count == 0)
                    {
                        MutexConsole.WriteLine("No remote repos for organization " + organization, null, ConsoleColor.Yellow);
                        continue;
                    }

                    remoteReposList = remoteReposList.Select(x => x.Split('/').Last()).ToList();
                    remoteReposList = remoteReposList.Where(x => Run([new() { OSPlatform = OSPlatform.Linux, Command = $"gh repo view {organization}/{x} --json isArchived --jq '.isArchived'" }]).SelectMany(x => x).ToList()[0] == "false").ToList();

                    if (repos.Contains("*"))
                        orgs.Value.Organizations[i] = new() { Organization = organization, Repos = new(remoteReposList) };
                    else
                    {
                        List<string> mismatchRepos = repos.Except(remoteReposList).ToList();
                        mismatchRepos.ForEach(x => MutexConsole.WriteLine("Remote repo " + x + " in organization " + organization + " does not exists", null, ConsoleColor.Yellow));
                        orgs.Value.Organizations[i] = new() { Organization = organization, Repos = new(repos.Except(mismatchRepos)) };
                    }

                    if (repos.Any(x => x.StartsWith('-')))
                    {
                        List<string> toRemove = repos.Where(x => x.StartsWith('-')).Select(x => x.Substring(1, x.Length - 1)).ToList();
                        orgs.Value.Organizations[i] = new() { Organization = organization, Repos = new(orgs.Value.Organizations[i].Repos.Where(x => !toRemove.Contains(x))) };
                    }
                }

                int line = 0;
                Parallel.ForEach(orgs.Value.Organizations, new ParallelOptions { MaxDegreeOfParallelism = 2 }, x => Parallel.ForEach(x.Repos, new ParallelOptions { MaxDegreeOfParallelism = 3 }, y => UpdateRepo(x.Organization, y, orgs.Value.Path, line++, false, 5, action)));
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
                orgs.Value.Organizations.ForEach(x =>
                {
                    List<string> dirs = [.. Directory.GetDirectories(Path.Combine(orgs.Value.Path, x.Organization))];
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