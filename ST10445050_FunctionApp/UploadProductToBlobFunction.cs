using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ABC_RETAILS_Function_App.Functions
{
    public class UploadProductToBlobFunction
    {
        private readonly ILogger _logger;

        public UploadProductToBlobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadProductToBlobFunction>();
        }

        [Function("UploadProductToBlobFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "products/uploadblob")] HttpRequestData req)
        {
            _logger.LogInformation("Processing UploadProductToBlobFunction request...");

            // ? FIXED: Correct variable name
            string blobConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = Environment.GetEnvironmentVariable("AzureBlobContainerName") ?? "product-images";

            if (string.IsNullOrWhiteSpace(blobConnectionString))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await badResponse.WriteStringAsync("Azure Storage connection string not found. Please check local.settings.json.");
                return badResponse;
            }

            // Ensure multipart form-data
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) ||
                !contentTypes.First().Contains("multipart/form-data"))
            {
                var badTypeResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badTypeResponse.WriteStringAsync("Invalid content type. Expected multipart/form-data.");
                return badTypeResponse;
            }

            var contentType = contentTypes.First();
            var boundary = contentType.Split("boundary=")[1];
            var reader = new Microsoft.AspNetCore.WebUtilities.MultipartReader(boundary, req.Body);

            Microsoft.AspNetCore.WebUtilities.MultipartSection section;
            string imageUrl = null;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var hasFileContentDisposition =
                    section.ContentDisposition != null &&
                    section.ContentDisposition.Contains("form-data") &&
                    section.ContentDisposition.Contains("filename=");

                if (hasFileContentDisposition)
                {
                    var fileName = GetFileName(section.ContentDisposition);
                    var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

                    // Validate file type
                    if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
                    {
                        var invalidFileResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                        await invalidFileResponse.WriteStringAsync("Invalid file type. Only .jpg and .png are allowed.");
                        return invalidFileResponse;
                    }

                    // ? Upload image to Azure Blob Storage
                    imageUrl = await UploadImageToBlob(section.Body, fileName, blobConnectionString, containerName);
                    break;
                }
            }

            if (string.IsNullOrEmpty(imageUrl))
            {
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync("No image uploaded.");
                return response;
            }

            // ? Return success with Blob URL
            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            await successResponse.WriteStringAsync(JsonConvert.SerializeObject(new { blobUrl = imageUrl }));
            return successResponse;
        }

        // Helper method to upload file stream to Blob
        private async Task<string> UploadImageToBlob(Stream fileStream, string fileName, string blobConnectionString, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient($"{Guid.NewGuid()}_{fileName}");
            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = "image/jpeg" });

            return blobClient.Uri.ToString();
        }

        private string GetFileName(string contentDisposition)
        {
            var elements = contentDisposition.Split(';');
            var filenameElement = elements.FirstOrDefault(e => e.Trim().StartsWith("filename="));
            if (filenameElement == null) return "uploaded_image.jpg";
            return filenameElement.Split('=')[1].Trim('"');
        }
    }
}
