namespace GitSync
{
    public struct GitSyncRepoConfig
    {
        public string Organization { get; set; }
        public List<string> Repo { get; set; }
    }

    public struct GitSyncConfig
    {
        public List<GitSyncRepoConfig> Repo { get; set; }
        public string Path { get; set; }
    }
}