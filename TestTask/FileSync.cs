namespace TestTask
{
    public class FileSync
    {
        public string Path { get; private set; }
        public string TrimPath { get; private set; }
        public long Size { get; private set; }
        public DateTime LastModificationTime { get; private set; }

        public FileSync(string path, long size, DateTime lastModificationTime, string rootPath) 
        {
            Path = path;
            Size = size;
            LastModificationTime = lastModificationTime;

            TrimPath = Path.Replace(rootPath, "");
        }


        public override string ToString()
        {
            return $"File Path: {TrimPath} | Last Modification: {LastModificationTime} | Size: {Size} bytes";
        }
    }
}
