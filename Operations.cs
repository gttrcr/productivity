using static GitSync.Program;
using static Process.Process;

namespace GitSync
{
    public static class Operations
    {
        public static List<Repo>? SomeDiff { get; private set; }
        public static List<Repo>? UnableToComplete { get; private set; }

        public static void UpdateRepo(string organization, string name, string path, int line, bool zeroLeft, int iterations, bool push)
        {
            name = name.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
            string repoPath = Path.Combine(path, organization, name);

            try
            {
                MutexConsoleWriteLine(repoPath + "...", line, ConsoleColor.White, zeroLeft);
                if (!Directory.Exists(repoPath))
                {
                    MutexConsoleWriteLine("Clone...", line);
                    Run(null, "git clone --recurse-submodules -j8 git@github.com:" + organization + "/" + name + ".git " + repoPath);
                    MutexConsoleWriteLine("done", line, ConsoleColor.Green);
                }
                else
                {
                    List<string> localBranches = Run(null, "git -C " + repoPath + " branch --format='%(refname:short)'");
                    List<string> remoteBranches = Run(null, "git -C " + repoPath + " branch -r --format='%(refname:short)'");
                    string currentBranch = Run(null, "git -C " + repoPath + " rev-parse --abbrev-ref HEAD")[0];
                    MutexConsoleWriteLine("Pull (" + currentBranch + ")...", line);
                    Run(null, "git -C " + repoPath + " pull");
                    Run(null, "git -C " + repoPath + " submodule update --recursive --remote --init");
                    MutexConsoleWriteLine("Check...", line);
                    if (Run(null, "git -C " + repoPath + " diff --stat").Count > 0)
                    {
                        SomeDiff ??= new List<Repo>();
                        SomeDiff.Add(new() { Organization = organization, Name = name, Path = repoPath });
                        MutexConsoleWriteLine("some diff...", line, ConsoleColor.Yellow);
                        if (push)
                        {
                            MutexConsoleWriteLine("push...", line, ConsoleColor.Yellow);
                            Run(null, "git -C " + repoPath + " add .");
                            Run(null, "git -C " + repoPath + " commit -m 'gitsync auto push'");
                            Run(null, "git -C " + repoPath + " push");
                        }
                    }
                    MutexConsoleWriteLine("done", line, ConsoleColor.Green);
                }
            }
            catch
            {
                if (iterations == 0)
                {
                    MutexConsoleWriteLine("unable to complete", line, ConsoleColor.Red);
                    UnableToComplete ??= new List<Repo>();
                    UnableToComplete.Add(new() { Organization = organization, Name = name, Path = repoPath });
                }
                else
                    UpdateRepo(organization, name, path, line, true, iterations - 1, push);
            }
        }
    }
}