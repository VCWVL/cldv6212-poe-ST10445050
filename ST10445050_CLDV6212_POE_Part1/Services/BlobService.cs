using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ST10445050_CLDV6212_POE_Part1.Services
{
    public class BlobService
    {
        private readonly BlobServiceClient _blobClient;
        private readonly string _containerName;

        public BlobService(IConfiguration configuration)
        {
            // Get connection string from appsettings.json
            string connectionString = configuration.GetConnectionString("AzureStorage");
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Azure Blob Storage connection string is missing.");

            _containerName = "product-images"; // your container name
            _blobClient = new BlobServiceClient(connectionString);

            // Ensure the container exists
            var containerClient = _blobClient.GetBlobContainerClient(_containerName);
            containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        // Upload a file stream to Azure Blob Storage
        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType = null)
        {
            var containerClient = _blobClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var headers = new BlobHttpHeaders();
            if (!string.IsNullOrEmpty(contentType))
                headers.ContentType = contentType;

            await blobClient.UploadAsync(fileStream, headers);
            return blobClient.Uri.ToString();
        }

        // Delete a blob from Azure Blob Storage using its URI
        public async Task DeleteBlobAsync(string blobUri)
        {
            Uri uri = new Uri(blobUri);
            string blobName = uri.Segments[^1];

            var containerClient = _blobClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
    }
}
