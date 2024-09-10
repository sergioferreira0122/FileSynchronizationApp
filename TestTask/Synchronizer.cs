using System.Security.Cryptography;

namespace TestTask
{
    public class Synchronizer
    {
        private readonly Logger _logger;
        private readonly Settings _settings;
        public Dictionary<string, FolderSync> SourceFolder { get; } = [];
        public Dictionary<string, FolderSync> ReplicaFolder { get; } = [];
        public Queue<SyncOperation> SyncOperations { get; } = [];

        public Synchronizer(Logger logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public void Start()
        {
            while (true)
            {
                Synchronize();
                Thread.Sleep(_settings.SyncIntervalMiliseconds);
            }
        }

        public void Synchronize()
        {
            _logger.Write("Synchronization started.");

            // Ensure the replica folder exists
            Directory.CreateDirectory(_settings.ReplicaFolderPath);

            // Read all files and folders from the source and replica directories
            ReadFoldersAndFiles(_settings.SourceFolderPath, SourceFolder, _settings.SourceFolderPath);
            ReadFoldersAndFiles(_settings.ReplicaFolderPath, ReplicaFolder, _settings.ReplicaFolderPath);

            // Clean up files and folders in the replica that are not present in the source
            CleanReplicaFolder();

            // Check for files and folders in the source that need to be copied or created in the replica
            CheckCopyAndCreationOperations();

            // Execute all queued synchronization operations
            TakeOperations();

            _logger.Write("Synchronization completed.");

            // Clear the dictionaries and queue to prepare for the next sync cycle
            SourceFolder.Clear();
            ReplicaFolder.Clear();
            SyncOperations.Clear();
        }

        public void ReadFoldersAndFiles(string currentFolderPath, Dictionary<string, FolderSync> folderSyncDict, string rootFolderPath)
        {
            var folderSync = new FolderSync(currentFolderPath, Directory.GetLastWriteTime(currentFolderPath), rootFolderPath);
            var fileSyncList = new List<FileSync>();

            // Read all files in the current folder
            foreach (var file in Directory.GetFiles(currentFolderPath))
            {
                var fileInfo = new FileInfo(file);
                var fileSync = new FileSync(file, fileInfo.Length, fileInfo.LastWriteTime, rootFolderPath);
                fileSyncList.Add(fileSync);
            }

            folderSync.Files = fileSyncList;
            folderSyncDict[folderSync.TrimPath] = folderSync;

            // Recursively read all subfolders
            foreach (var subfolder in Directory.GetDirectories(currentFolderPath))
            {
                ReadFoldersAndFiles(subfolder, folderSyncDict, rootFolderPath);
            }
        }

        public void CleanReplicaFolder()
        {
            foreach (var replicaFolder in ReplicaFolder.Values)
            {
                // If the folder exists in the source
                if (SourceFolder.TryGetValue(replicaFolder.TrimPath, out var sourceFolder))
                {
                    // Check each file in the replica folder
                    foreach (var replicaFile in replicaFolder.Files)
                    {
                        // If the file is not present in the source, queue it for removal
                        if (!sourceFolder.Files.Any(file => file.TrimPath == replicaFile.TrimPath))
                        {
                            SyncOperations.Enqueue(new SyncOperation(replicaFile.Path, TargetType.File, SyncOperator.Remove, _settings.ReplicaFolderPath));
                        }
                    }
                }
                else
                {
                    // If the folder itself is not in the source, queue the whole folder for removal
                    SyncOperations.Enqueue(new SyncOperation(replicaFolder.Path, TargetType.Folder, SyncOperator.Remove, _settings.ReplicaFolderPath));
                }
            }
        }

        public void CheckCopyAndCreationOperations()
        {
            foreach (var sourceFolder in SourceFolder.Values)
            {
                // If the folder exists in the replica
                if (ReplicaFolder.TryGetValue(sourceFolder.TrimPath, out var replicaFolder))
                {
                    // Check each file in the source folder
                    foreach (var fileSource in sourceFolder.Files)
                    {
                        // Try to find the corresponding file in the replica
                        var fileReplica = replicaFolder.Files.FirstOrDefault(file => file.TrimPath == fileSource.TrimPath);

                        if (fileReplica == null)
                        {
                            // If the file does not exist in the replica, queue it for creation
                            SyncOperations.Enqueue(new SyncOperation(_settings.ReplicaFolderPath + fileSource.TrimPath, TargetType.File, SyncOperator.Create, _settings.ReplicaFolderPath));
                        }
                        else
                        {
                            // Compare the files byte by byte to ensure they are identical
                            if (!FilesAreEqual(fileReplica.Path, fileSource.Path))
                            {
                                // If the files differ, queue the file in the replica for replacement (copy)
                                SyncOperations.Enqueue(new SyncOperation(_settings.ReplicaFolderPath + fileSource.TrimPath, TargetType.File, SyncOperator.Copy, _settings.ReplicaFolderPath));
                            }
                            else
                            {
                                // If the files are identical, update the last modification time in the replica
                                File.SetLastWriteTime(fileReplica.Path, fileSource.LastModificationTime);
                            }
                        }
                    }
                }
                else
                {
                    // If the folder does not exist in the replica, queue it for creation
                    SyncOperations.Enqueue(new SyncOperation(_settings.ReplicaFolderPath + sourceFolder.TrimPath, TargetType.Folder, SyncOperator.Create, _settings.ReplicaFolderPath));

                    // Also, queue all files within this folder for creation
                    foreach (var fileSource in sourceFolder.Files)
                    {
                        SyncOperations.Enqueue(new SyncOperation(_settings.ReplicaFolderPath + fileSource.TrimPath, TargetType.File, SyncOperator.Create, _settings.ReplicaFolderPath));
                    }
                }
            }
        }

        public void TakeOperations()
        {
            while (SyncOperations.Count != 0)
            {
                var operation = SyncOperations.Dequeue();

                // Handle the creation of files or folders
                if (operation.SyncOperator.Equals(SyncOperator.Create))
                {
                    if (operation.TargetType.Equals(TargetType.File))
                    {
                        // If the file exists in the source, copy it to the replica
                        if (File.Exists(_settings.SourceFolderPath + operation.TrimPath))
                        {
                            File.Copy(_settings.SourceFolderPath + operation.TrimPath, operation.Path, true);
                            _logger.Write($"Created file: {_settings.SourceFolderPath + operation.TrimPath} to {operation.Path}");
                        }
                        else
                        {
                            // Log an error if the file was deleted before it could be copied
                            File.Delete(operation.Path);
                            _logger.Write($"Error creating the file, source file was deleted during synchronization: {_settings.SourceFolderPath + operation.TrimPath}");
                        }
                    }
                    else
                    {
                        // Create the directory in the replica
                        Directory.CreateDirectory(operation.Path);
                        _logger.Write($"Created directory: {operation.Path}");
                    }
                }
                // Handle the copying of files
                else if (operation.SyncOperator.Equals(SyncOperator.Copy))
                {
                    // If the file exists in the source, copy it to the replica
                    if (File.Exists(_settings.SourceFolderPath + operation.TrimPath))
                    {
                        File.Copy(_settings.SourceFolderPath + operation.TrimPath, operation.Path, true);
                        _logger.Write($"Copied file: {_settings.SourceFolderPath + operation.TrimPath} to {operation.Path}");
                    }
                    else
                    {
                        // Log an error if the file was deleted before it could be copied
                        File.Delete(operation.Path);
                        _logger.Write($"Error copying the file, source file was deleted during synchronization: {_settings.SourceFolderPath + operation.TrimPath}");
                    }
                }
                // Handle the removal of files or folders
                else
                {
                    if (operation.TargetType.Equals(TargetType.File))
                    {
                        // If the file exists in the replica, delete it
                        if (File.Exists(operation.Path))
                        {
                            File.Delete(operation.Path);
                            _logger.Write($"Deleted file: {operation.Path}");
                        }

                    }
                    else
                    {
                        // If the directory exists in the replica, delete it
                        if (Directory.Exists(operation.Path))
                        {
                            Directory.Delete(operation.Path, true);
                            _logger.Write($"Deleted directory: {operation.Path}");
                        }
                    }
                }
            }
        }

        private static bool FilesAreEqual(string filePath1, string filePath2)
        {
            var fileInfo1 = new FileInfo(filePath1);
            var fileInfo2 = new FileInfo(filePath2);

            // If the file sizes are different, the files are not equal
            if (fileInfo1.Length != fileInfo2.Length)
            {
                return false; // Files are different
            }

            // Compare the file contents using MD5 hash
            using var md5 = MD5.Create();

            using var stream1 = File.OpenRead(filePath1);
            using var stream2 = File.OpenRead(filePath2);
            var hash1 = md5.ComputeHash(stream1);
            var hash2 = md5.ComputeHash(stream2);

            // Check if the hashes are identical
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    return false; // Files are different
                }
            }

            return true; // Files are the same
        }
    }
}