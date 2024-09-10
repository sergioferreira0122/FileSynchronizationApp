namespace TestTask
{
    public class FolderSync
    {
        public string Path { get; private set; }
        public string TrimPath { get; private set; }
        public DateTime LastModificationTime { get; private set; }
        public List<FileSync> Files { get; set; } = [];

        public FolderSync(string path, DateTime lastModificationTime, string rootPath) 
        {
            Path = path;
            LastModificationTime = lastModificationTime;

            TrimPath = Path.Replace(rootPath, "");
        }


        public override string ToString()
        {
            string filesInfo = string.Join("\n", Files.Select(f => f.ToString()));
            return $"Folder Path: {TrimPath} | Last Modification: {LastModificationTime} | Files:\n{filesInfo}";
        }
    }
}
