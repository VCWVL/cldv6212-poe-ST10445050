using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

//ST10445050 - KEONA MACKAN

namespace FileUploadFunctionApp.Functions
{
    public class UploadFileFunction
    {
        private readonly ILogger _logger;
        private const string ShareName = "uploads";   // MUST match FileShareName in your storage account
        private const string DirectoryName = "uploads"; // Folder inside the share

        public UploadFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadFileFunction>();
        }

        [Function("UploadFileFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get", Route = "files")] HttpRequestData req)
        {
            _logger.LogInformation("Processing UploadFileFunction request...");

            var response = req.CreateResponse();
            string connectionString = Environment.GetEnvironmentVariable("FileShareConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("? Missing FileShareConnectionString in settings.");
                return response;
            }

            try
            {
                var serviceClient = new ShareServiceClient(connectionString);
                var shareClient = serviceClient.GetShareClient(ShareName);
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetDirectoryClient(DirectoryName);
                await directoryClient.CreateIfNotExistsAsync();

                // POST ? Upload file to Azure File Share
                if (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    var fileRequest = JsonConvert.DeserializeObject<FileUploadRequest>(requestBody);

                    if (fileRequest == null || string.IsNullOrEmpty(fileRequest.FileName) || string.IsNullOrEmpty(fileRequest.FileContent))
                    {
                        response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                        await response.WriteStringAsync("Invalid request. Must include FileName and Base64 FileContent.");
                        return response;
                    }

                    byte[] fileBytes = Convert.FromBase64String(fileRequest.FileContent);
                    var fileClient = directoryClient.GetFileClient(fileRequest.FileName);

                    using var stream = new MemoryStream(fileBytes);
                    await fileClient.CreateAsync(stream.Length);
                    stream.Position = 0;
                    await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream);

                    _logger.LogInformation($"? File '{fileRequest.FileName}' uploaded to '{DirectoryName}' inside '{ShareName}'.");

                    response.StatusCode = System.Net.HttpStatusCode.OK;
                    await response.WriteStringAsync($"? File '{fileRequest.FileName}' uploaded successfully.");
                    return response;
                }

                // GET ? List files from the directory
                if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    var files = new List<FileModel>();
                    await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
                    {
                        if (!item.IsDirectory)
                        {
                            var fileClient = directoryClient.GetFileClient(item.Name);
                            var props = await fileClient.GetPropertiesAsync();
                            files.Add(new FileModel
                            {
                                fileName = item.Name,
                                fileSize = props.Value.ContentLength,
                                LastModified = props.Value.LastModified
                            });
                        }
                    }

                    var responseData = new
                    {
                        Share = ShareName,
                        Directory = DirectoryName,
                        FileCount = files.Count,
                        Files = files
                    };

                    string jsonResponse = JsonConvert.SerializeObject(responseData, Formatting.Indented);
                    response.StatusCode = System.Net.HttpStatusCode.OK;
                    await response.WriteStringAsync(jsonResponse, Encoding.UTF8);
                    return response;
                }

                response.StatusCode = System.Net.HttpStatusCode.MethodNotAllowed;
                await response.WriteStringAsync("Only GET and POST methods are supported.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Error processing file upload: {ex.Message}");
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }

    public class FileUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string FileContent { get; set; } = string.Empty; // Base64 string
    }

    public class FileModel
    {
        public string fileName { get; set; } = string.Empty;
        public long fileSize { get; set; }
        public DateTimeOffset? LastModified { get; set; }

        public string DisplaySize =>
            fileSize >= 1024 * 1024 ? $"{fileSize / 1024 / 1024} MB"
            : fileSize >= 1024 ? $"{fileSize / 1024} KB"
            : $"{fileSize} Bytes";
    }
}
