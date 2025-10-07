using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using ST10445050_CLDV6212_POE_Part1.Models;
using System.IO;

namespace ST10445050_CLDV6212_POE_Part1.Services
{
    public class FileStorageService
    {
        private readonly string _connectionString;
        private readonly string _fileShareName;

        public string ConnectionString => _connectionString;
        public string FileShareName => _fileShareName;

        public FileStorageService(string connectionString, string fileShareName)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _fileShareName = fileShareName ?? throw new ArgumentNullException(nameof(fileShareName));
        }

        // ================= Upload File =================
        public async Task UploadFileAsync(string directoryName, string fileName, Stream fileStream)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File stream is empty", nameof(fileStream));

            try
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);

                // Ensure the share exists
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                // Ensure the directory exists
                await directoryClient.CreateIfNotExistsAsync();

                var fileClient = directoryClient.GetFileClient(fileName);
                await fileClient.CreateAsync(fileStream.Length);

                fileStream.Position = 0;
                await fileClient.UploadRangeAsync(new HttpRange(0, fileStream.Length), fileStream);
            }
            catch (RequestFailedException ex)
            {
                // You can log ex.Message for debugging
                throw new InvalidOperationException($"Error uploading file '{fileName}': {ex.Message}", ex);
            }
        }

        // ================= Download File =================
        public async Task<Stream?> DownloadFileAsync(string directoryName, string fileName)
        {
            try
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                var fileClient = directoryClient.GetFileClient(fileName);

                var response = await fileClient.DownloadAsync();
                var ms = new MemoryStream();
                await response.Value.Content.CopyToAsync(ms);
                ms.Position = 0;
                return ms;
            }
            catch (RequestFailedException)
            {
                // Return null if file or directory does not exist
                return null;
            }
        }

        // ================= List Files =================
        public async Task<List<FileModel>> ListFilesAsync(string directoryName)
        {
            var result = new List<FileModel>();

            try
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);

                // Ensure the share exists
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                // Ensure the directory exists
                await directoryClient.CreateIfNotExistsAsync();

                await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        var fileClient = directoryClient.GetFileClient(item.Name);
                        var props = await fileClient.GetPropertiesAsync();

                        result.Add(new FileModel
                        {
                            fileName = item.Name,
                            fileSize = props.Value.ContentLength,
                            LastModified = props.Value.LastModified
                        });
                    }
                }
            }
            catch (RequestFailedException)
            {
                // Return empty list if share or directory not found
                return new List<FileModel>();
            }

            return result;
        }

        // ================= Delete File =================
        public async Task<bool> DeleteFileAsync(string directoryName, string fileName)
        {
            try
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                var fileClient = directoryClient.GetFileClient(fileName);

                var result = await fileClient.DeleteIfExistsAsync();
                return result.Value; // true if deleted, false if file not found
            }
            catch (RequestFailedException)
            {
                return false; // Return false if directory or file does not exist
            }
        }
    }
}
