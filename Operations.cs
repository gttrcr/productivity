using static GitSync.Program;
using static Process.Process;

namespace GitSync
{
    public static class Operations
    {
        public static void UpdateRepo(string organization, string repo, string path, int line, bool zeroLeft = false)
        {
            try
            {
                repo = repo.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
                string repoPath = Path.Combine(path, organization, repo);
                MutexConsoleWriteLine(repoPath + "...", line, ConsoleColor.White, zeroLeft);
                if (!Directory.Exists(repoPath))
                {
                    MutexConsoleWriteLine("Clone...", line);
                    Run(null, "git clone --recurse-submodules -j8 git@github.com:" + organization + "/" + repo + ".git " + repoPath);
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
                        MutexConsoleWriteLine("some diff...", line, ConsoleColor.Yellow);
                    MutexConsoleWriteLine("done", line, ConsoleColor.Green);
                }
            }
            catch
            {
                UpdateRepo(organization, repo, path, line, true);
            }
        }
    }
}