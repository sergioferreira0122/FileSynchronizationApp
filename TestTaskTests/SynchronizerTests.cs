using Moq;
using TestTask;
using FluentAssertions;

namespace TestTaskTests
{
    public class SynchronizerTests
    {
        private Mock<Logger> _mockLogger;
        private Mock<Settings> _mockSettings;
        private Synchronizer _synchronizer;

        public SynchronizerTests()
        {
            Directory.CreateDirectory("C:\\VeeamTest\\Logs");
            Directory.CreateDirectory("C:\\VeeamTest\\Source");
            Directory.CreateDirectory("C:\\VeeamTest\\Replica");

            _mockSettings = new Mock<Settings>("C:\\VeeamTest\\Source", "C:\\VeeamTest\\Replica", "10");
            _mockLogger = new Mock<Logger>("C:\\VeeamTest\\Logs\\logs.txt");

            _mockLogger.Setup(logger => logger.Write(It.IsAny<string>()));

            _synchronizer = new Synchronizer(_mockLogger.Object, _mockSettings.Object);
        }

        private void CleanFolders()
        {
            // Clean Source Folder
            if (Directory.Exists(_mockSettings.Object.SourceFolderPath))
            {
                Directory.Delete(_mockSettings.Object.SourceFolderPath, true); // Deletes all contents of the source folder
                Directory.CreateDirectory(_mockSettings.Object.SourceFolderPath); // Recreate the source directory
            }

            // Clean Replica Folder
            if (Directory.Exists(_mockSettings.Object.ReplicaFolderPath))
            {
                Directory.Delete(_mockSettings.Object.ReplicaFolderPath, true); // Deletes all contents of the replica folder
                Directory.CreateDirectory(_mockSettings.Object.ReplicaFolderPath); // Recreate the replica directory
            }
        }

        [Fact]
        public void Synchronize_ShouldCreateReplicaFolder_IfNotExist()
        {
            // Arrange
            if (Directory.Exists(_mockSettings.Object.ReplicaFolderPath))
            {
                Directory.Delete(_mockSettings.Object.ReplicaFolderPath, true); // Ensure the replica folder doesn't exist
            }

            // Act
            _synchronizer.Synchronize();

            // Assert
            Assert.True(Directory.Exists(_mockSettings.Object.ReplicaFolderPath));

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }



        [Fact]
        public void CheckCopyAndCreationOperations_ShouldQueueCopyOperation_WhenFilesAreDifferent()
        {
            // Arrange
            var sourceFile = Path.Combine(_mockSettings.Object.SourceFolderPath, "file.txt");
            var replicaFile = Path.Combine(_mockSettings.Object.ReplicaFolderPath, "file.txt");

            File.WriteAllText(sourceFile, "Source file");
            File.WriteAllText(replicaFile, "Replica file");

            // Act
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.SourceFolderPath, _synchronizer.SourceFolder, _mockSettings.Object.SourceFolderPath);
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.ReplicaFolderPath, _synchronizer.ReplicaFolder, _mockSettings.Object.ReplicaFolderPath);
            _synchronizer.CheckCopyAndCreationOperations();

            // Assert
            _synchronizer.SyncOperations.Count.Should().Be(1);
            var operation = _synchronizer.SyncOperations.Dequeue();
            operation.SyncOperator.Should().Be(SyncOperator.Copy);
            operation.Path.Should().Be(Path.Combine(_mockSettings.Object.ReplicaFolderPath, "file.txt"));

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }
        
        [Fact]
        public void TakeOperations_ShouldCreateFile_WhenOperationIsCreate()
        {
            // Arrange
            var sourceFile = _mockSettings.Object.SourceFolderPath + "\\file1.txt";

            File.WriteAllText(sourceFile, "Source file");

            // Act
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.SourceFolderPath, _synchronizer.SourceFolder, _mockSettings.Object.SourceFolderPath);
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.ReplicaFolderPath, _synchronizer.ReplicaFolder, _mockSettings.Object.ReplicaFolderPath);

            _synchronizer.CheckCopyAndCreationOperations();

            _synchronizer.TakeOperations();

            // Assert
            Assert.True(File.Exists(_mockSettings.Object.ReplicaFolderPath + "\\file1.txt"));

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }

        [Fact]
        public void CheckCopyAndCreationOperations_ShouldUpdateLastModificationDate_WhenFilesAreHaveDifferentModificationDate()
        {
            // Arrange
            var sourceFile = Path.Combine(_mockSettings.Object.SourceFolderPath, "file.txt");
            var replicaFile = Path.Combine(_mockSettings.Object.ReplicaFolderPath, "file.txt");
            var newDateTime = DateTime.Now.AddMinutes(2);

            File.WriteAllText(sourceFile, "Source file");

            File.SetLastWriteTime(sourceFile, newDateTime);

            File.WriteAllText(replicaFile, "Source file");

            // Act
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.SourceFolderPath, _synchronizer.SourceFolder, _mockSettings.Object.SourceFolderPath);
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.ReplicaFolderPath, _synchronizer.ReplicaFolder, _mockSettings.Object.ReplicaFolderPath);
            _synchronizer.CheckCopyAndCreationOperations();

            // Assert
            File.GetLastWriteTime(replicaFile).Should().Be(newDateTime);

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }

        [Fact]
        public void TakeOperations_ShouldCreateFoldersAndFiles_WhenOperationIsCreate()
        {
            // Arrange
            Directory.CreateDirectory(_mockSettings.Object.SourceFolderPath + "\\Folder1\\Folder11");
            var sourceFile = _mockSettings.Object.SourceFolderPath + "\\Folder1\\file1.txt";

            File.WriteAllText(sourceFile, "Source file");

            // Act
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.SourceFolderPath, _synchronizer.SourceFolder, _mockSettings.Object.SourceFolderPath);
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.ReplicaFolderPath, _synchronizer.ReplicaFolder, _mockSettings.Object.ReplicaFolderPath);

            _synchronizer.CheckCopyAndCreationOperations();

            _synchronizer.TakeOperations();

            // Assert
            Assert.True(File.Exists(_mockSettings.Object.ReplicaFolderPath + "\\Folder1\\file1.txt"));

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }

        [Fact]
        public void FilesAreEqual_ShouldCheckByteByByte()
        {
            // Arrange
            var sourceFile = Path.Combine(_mockSettings.Object.SourceFolderPath, "file.txt");
            var replicaFile = Path.Combine(_mockSettings.Object.ReplicaFolderPath, "file.txt");

            File.WriteAllText(sourceFile, "0e306561559aa787d00bc6f70bbdfe3404cf03659e704f8534c00ffb659c4c8740cc942feb2da115a3f4155cbb8607497386656d7d1f34a42059d78f5a8dd1ef");
            File.WriteAllText(replicaFile, "0e306561559aa787d00bc6f70bbdfe3404cf03659e744f8534c00ffb659c4c8740cc942feb2da115a3f415dcbb8607497386656d7d1f34a42059d78f5a8dd1ef");

            // Act
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.SourceFolderPath, _synchronizer.SourceFolder, _mockSettings.Object.SourceFolderPath);
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.ReplicaFolderPath, _synchronizer.ReplicaFolder, _mockSettings.Object.ReplicaFolderPath);
            _synchronizer.CheckCopyAndCreationOperations();

            // Assert
            _synchronizer.SyncOperations.Count.Should().Be(1);
            var operation = _synchronizer.SyncOperations.Dequeue();
            operation.SyncOperator.Should().Be(SyncOperator.Copy);
            operation.Path.Should().Be(Path.Combine(_mockSettings.Object.ReplicaFolderPath, "file.txt"));

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }

        [Fact]
        public void TakeOperations_ShouldShowError_WhenOperationIsCreate_AndTheFileWasDeletedDuringSync()
        {
            // Arrange
            var sourceFile = _mockSettings.Object.SourceFolderPath + "\\file1.txt";

            File.WriteAllText(sourceFile, "Source file");

            // Act
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.SourceFolderPath, _synchronizer.SourceFolder, _mockSettings.Object.SourceFolderPath);
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.ReplicaFolderPath, _synchronizer.ReplicaFolder, _mockSettings.Object.ReplicaFolderPath);

            _synchronizer.CheckCopyAndCreationOperations();

            File.Delete(sourceFile);

            _synchronizer.TakeOperations();

            // Assert
            Assert.False(File.Exists(_mockSettings.Object.ReplicaFolderPath + "\\file1.txt"));

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }

        [Fact]
        public void TakeOperations_ShouldCopyFile_WhenOperationIsCopy()
        {
            // Arrange
            var sourceFile = _mockSettings.Object.SourceFolderPath + "\\file1.txt";
            File.WriteAllText(sourceFile, "Source file");

            var replicaFile = _mockSettings.Object.ReplicaFolderPath + "\\file1.txt";
            File.WriteAllText(replicaFile, "Replica file");

            // Act
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.SourceFolderPath, _synchronizer.SourceFolder, _mockSettings.Object.SourceFolderPath);
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.ReplicaFolderPath, _synchronizer.ReplicaFolder, _mockSettings.Object.ReplicaFolderPath);

            _synchronizer.CheckCopyAndCreationOperations();

            _synchronizer.TakeOperations();

            // Assert
            var replicaFileText = File.ReadAllText(_mockSettings.Object.ReplicaFolderPath + "\\file1.txt");
            var sourceFileText = File.ReadAllText(_mockSettings.Object.SourceFolderPath + "\\file1.txt");

            sourceFileText.Should().Be(replicaFileText);

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }
        [Fact]
        public void TakeOperations_ShouldShowError_WhenOperationIsCopy_AndTheFileWasDeletedDuringSync()
        {
            // Arrange
            var sourceFile = _mockSettings.Object.SourceFolderPath + "\\file1.txt";
            File.WriteAllText(sourceFile, "Source file");

            var replicaFile = _mockSettings.Object.ReplicaFolderPath + "\\file1.txt";
            File.WriteAllText(replicaFile, "Replica file");

            // Act
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.SourceFolderPath, _synchronizer.SourceFolder, _mockSettings.Object.SourceFolderPath);
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.ReplicaFolderPath, _synchronizer.ReplicaFolder, _mockSettings.Object.ReplicaFolderPath);

            _synchronizer.CheckCopyAndCreationOperations();

            File.Delete(sourceFile);

            _synchronizer.TakeOperations();

            // Assert
            Assert.False(File.Exists(_mockSettings.Object.ReplicaFolderPath + "\\file1.txt"));

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }

        [Fact]
        public void TakeOperations_ShouldDeleteFile_WhenFileInReplicaNotInSource()
        {
            // Arrange
            Directory.CreateDirectory(_mockSettings.Object.ReplicaFolderPath + "\\Folder1\\Folder11");
            var replicaFile1 = _mockSettings.Object.ReplicaFolderPath + "\\Folder1\\Folder11\\file.txt";
            File.WriteAllText(replicaFile1, "Replica file 1");

            var replicaFile2 = _mockSettings.Object.ReplicaFolderPath + "\\file.txt";
            File.WriteAllText(replicaFile2, "Replica file 2");

            // Act
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.SourceFolderPath, _synchronizer.SourceFolder, _mockSettings.Object.SourceFolderPath);
            _synchronizer.ReadFoldersAndFiles(_mockSettings.Object.ReplicaFolderPath, _synchronizer.ReplicaFolder, _mockSettings.Object.ReplicaFolderPath);

            _synchronizer.CleanReplicaFolder();

            _synchronizer.TakeOperations();

            // Assert
            Assert.False(File.Exists(_mockSettings.Object.ReplicaFolderPath + "\\Folder1\\Folder11\\file.txt"));

            _mockLogger.Object.Close();  // Ensure the logger is closed of after each test
            CleanFolders(); // Ensure both folders are clean for upcoming tests
        }
    }
}