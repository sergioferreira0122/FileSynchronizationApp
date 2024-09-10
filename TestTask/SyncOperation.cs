namespace TestTask
{
    public class SyncOperation
    {
        public string Path { get; private set; }
        public string TrimPath { get; private set; }
        public TargetType TargetType { get; private set; }
        public SyncOperator SyncOperator { get; private set; }

        public SyncOperation(string path, TargetType targetType, SyncOperator syncOperator, string rootPath) 
        {
            Path = path;
            TargetType = targetType;
            SyncOperator = syncOperator;

            TrimPath = Path.Replace(rootPath, "");
        }

        public override string ToString()
        {
            return $"Path: {Path} | Type: {TargetType} | Operation: {SyncOperator}";
        }
    }
}
