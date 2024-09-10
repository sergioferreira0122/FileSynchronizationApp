using TestTask;

namespace TestTaskTests
{
    public class SettingsTests
    {
        [Fact]
        public void Constructor_ShouldThrowArgumentException_WhenSourceFolderPathDoesNotExist()
        {
            // Arrange
            var invalidSourceFolderPath = "C:\\NonExistentFolder";
            var replicaFolderPath = "C:\\VeeamTest\\Replica";
            var syncIntervalSeconds = "10";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new Settings(invalidSourceFolderPath, replicaFolderPath, syncIntervalSeconds));

            Assert.Equal("Source folder path does not exist.", exception.Message);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentException_WhenSyncIntervalIsNotInteger()
        {
            // Arrange
            var sourceFolderPath = "C:\\VeeamTest\\Source";
            var replicaFolderPath = "C:\\VeeamTest\\Replica";
            var invalidSyncIntervalSeconds = "not a number";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new Settings(sourceFolderPath, replicaFolderPath, invalidSyncIntervalSeconds));

            Assert.Equal("Synchronization interval time should be in seconds (int value), for example '600'", exception.Message);
        }
    }
}
