using Microsoft.Extensions.Logging;
using Moq;
using SonarQubeWorker.DataAccess;
using SonarQubeWorker.Interface;

namespace SonarQubeWorker.Tests
{
    public class AzureBlobDataAccessTests
    {
        private Mock<ILogger<AzureBlobDataAccess>> _mockLogger;
        private IAzureBlobDataAccess _azureBlobDataAccess;
        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<AzureBlobDataAccess>>();

            // Setup your mocks here
            // ...

            _azureBlobDataAccess = new AzureBlobDataAccess(_mockLogger.Object);
        }

        [Test]
        public async Task DownloadAsyncInstantDownload_FileExists_ReturnsScanReport()
        {
            // Arrange
            string blobFilename = "circustrein_1699625745679.zip";
            string userId = "josselard";
            string downloadedFilePath = Path.Combine(Directory.GetCurrentDirectory(), blobFilename); // Adjust the path as needed

            try
            {
                // Act
                var result = await _azureBlobDataAccess.DownloadAsyncInstantDownload(blobFilename, userId);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(blobFilename, result.Name);
                Assert.AreEqual("application/octet-stream", result.ContentType);
            }
            finally
            {
                // Cleanup: Delete the downloaded file
                if (File.Exists(downloadedFilePath))
                {
                    File.Delete(downloadedFilePath);
                }
            }
        }





        [Test]
        public async Task DownloadAsyncInstantDownload_FileDoesNotExist_ReturnsNull()
        {
            // Arrange
            string blobFilename = "nonexistent.zip";
            string userId = "";


            // Act
            var result = await _azureBlobDataAccess.DownloadAsyncInstantDownload(blobFilename, userId);

            // Assert
            Assert.IsNull(result);
        }
    }
}