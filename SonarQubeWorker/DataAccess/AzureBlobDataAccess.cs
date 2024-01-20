using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Sonarqube_API.Models;
using SonarQubeWorker.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarQubeWorker.DataAccess
{
    public class AzureBlobDataAccess : IAzureBlobDataAccess
    {
        private readonly string _storageConnectionString;
        private readonly string _storageContainerName;
        private readonly ILogger<AzureBlobDataAccess> _logger;

        public AzureBlobDataAccess(ILogger<AzureBlobDataAccess> logger)
        {

            _storageConnectionString = Environment.GetEnvironmentVariable("SQAzureBlobCS");
            _storageContainerName = Environment.GetEnvironmentVariable("SQAzureBlobContainerName");
            _logger = logger;

        }

        public async Task<RetrieveSourceCodeResponse> DownloadAsyncInstantDownload(string scanId, string userId)
        {
            BlobContainerClient client = new BlobContainerClient(_storageConnectionString, _storageContainerName);
            string destinationFilePath = scanId;
            try
            {
                // Get a reference to the blob uploaded earlier from the API in the container from configuration settings
                BlobClient file = client.GetBlobClient(userId + "\\" + scanId);

                // Check if the file exists in the container
                if (await file.ExistsAsync())
                {
                    await file.DownloadToAsync(destinationFilePath);

                    // Retrieve the file properties to populate the BlobDto
                    BlobProperties properties = await file.GetPropertiesAsync();
                    string name = scanId;
                    string contentType = properties.ContentType;


                    // Create a new BlobDto with the downloaded file details
                    return new RetrieveSourceCodeResponse { FilePath = destinationFilePath, Name = name, ContentType = contentType };
                }
            }
            catch (RequestFailedException ex)
                when (ex.ErrorCode == BlobErrorCode.BlobNotFound)

            {
                // Log error to console
                _logger.LogError($"File {scanId} was not found.");
            }

            // File does not exist
            return null;
        }

    }
}
