namespace TestTask
{
    public class Settings
    {
        public string SourceFolderPath { get; private set; }
        public string ReplicaFolderPath { get; private set; }
        public int SyncIntervalSeconds { get; private set; }
        public int SyncIntervalMiliseconds => SyncIntervalSeconds * 1000;

        public Settings(string sourceFolderPath, string replicaFolderPath, string syncIntervalSeconds) 
        {
            var sourceFolderExists = Directory.Exists(sourceFolderPath);
            if (!sourceFolderExists) { throw new ArgumentException("Source folder path does not exist."); }

            var seconds = ToInt(syncIntervalSeconds);

            SourceFolderPath = sourceFolderPath;
            ReplicaFolderPath = replicaFolderPath;
            SyncIntervalSeconds = seconds;
        }

        private static int ToInt(string s)
        {
            if (int.TryParse(s, out int val))
            {
                return val;
            }

            throw new ArgumentException("Synchronization interval time should be in seconds (int value), for example '600'");
        }
    }
}
