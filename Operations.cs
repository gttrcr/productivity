using GttrcrGist;
using static GttrcrGist.Process;

namespace GitSync
{
    public static class Operations
    {
        public static List<Repo>? SomeDiff { get; private set; }
        public static List<Repo>? UnableToComplete { get; private set; }

        public static void UpdateRepo(string organization, string name, string path, int line, bool zeroLeft, int iterations, string? action)
        {
            name = name.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
            string repoPath = Path.Combine(path, organization, name);

            try
            {
                MutexConsole.WriteLine(repoPath + "...", line, ConsoleColor.White, zeroLeft);
                if (!Directory.Exists(repoPath))
                {
                    MutexConsole.WriteLine("Clone...", line);
                    Run(null, "git clone --recurse-submodules -j8 git@github.com:" + organization + "/" + name + ".git " + repoPath);
                    MutexConsole.WriteLine("done", line, ConsoleColor.Green);
                }
                else
                {
                    List<string> localBranches = Run(null, "git -C " + repoPath + " branch --format='%(refname:short)'");
                    List<string> remoteBranches = Run(null, "git -C " + repoPath + " branch -r --format='%(refname:short)'");
                    string currentBranch = Run(null, "git -C " + repoPath + " rev-parse --abbrev-ref HEAD")[0];
                    MutexConsole.WriteLine("Pull (" + currentBranch + ")...", line);
                    Run(null, "git -C " + repoPath + " pull");
                    Run(null, "git -C " + repoPath + " submodule update --recursive --init");
                    MutexConsole.WriteLine("Check...", line);
                    Run(null, "git -C " + repoPath + " add .");
                    if (Run(null, "git -C " + repoPath + " status --porcelain").Count > 0)
                    {
                        SomeDiff ??= [];
                        SomeDiff.Add(new() { Organization = organization, Name = name, Path = repoPath });
                        MutexConsole.WriteLine("some diff...", line, ConsoleColor.Yellow);
                        if (action != null && action.Equals("push"))
                        {
                            MutexConsole.WriteLine("push...", line, ConsoleColor.Yellow);
                            Run(null, "git -C " + repoPath + " add .");
                            Run(null, "git -C " + repoPath + " commit -m 'gitsync auto push'");
                            Run(null, "git -C " + repoPath + " push");
                        }
                    }
                    MutexConsole.WriteLine("done", line, ConsoleColor.Green);
                }
            }
            catch
            {
                if (iterations == 0)
                {
                    MutexConsole.WriteLine("unable to complete", line, ConsoleColor.Red);
                    UnableToComplete ??= [];
                    UnableToComplete.Add(new() { Organization = organization, Name = name, Path = repoPath });
                }
                else
                    UpdateRepo(organization, name, path, line, true, iterations - 1, action);
            }
        }
    }
}