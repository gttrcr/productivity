namespace GitSync
{
    public struct Repo
    {
        public string Organization { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public struct OrganizationConfig
    {
        public string Organization { get; set; }
        public List<string> Repos { get; set; }
    }

    public struct Config
    {
        public List<OrganizationConfig> Organizations { get; set; }
        public string Path { get; set; }
    }
}