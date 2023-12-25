namespace GitSync
{
    public struct Repo
    {
        public string Organization { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public struct RepoConfig
    {
        public string Organization { get; set; }
        public List<string> Repos { get; set; }
    }

    public struct Config
    {
        public List<RepoConfig> Repos { get; set; }
        public string Path { get; set; }
    }
}