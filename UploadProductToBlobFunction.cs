using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "products/uploadblob")] HttpRequestData req)
        {
            _logger.LogInformation("Processing UploadProductToBlobFunction request...");

            string tableConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string blobConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorageBlob");
            string containerName = Environment.GetEnvironmentVariable("AzureBlobContainerName") ?? "product-images";

            var tableClient = new TableClient(tableConnectionString, "Products");
            await tableClient.CreateIfNotExistsAsync();

            // GET: List all products
            if (req.Method == "GET")
            {
                try
                {
                    var products = new List<Product>();
                    await foreach (var entity in tableClient.QueryAsync<Product>())
                    {
                        products.Add(entity);
                    }

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    await response.WriteStringAsync(JsonConvert.SerializeObject(products));
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error fetching products: {ex.Message}");
                    var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    await response.WriteStringAsync(JsonConvert.SerializeObject(new { Error = "Error fetching products." }));
                    return response;
                }
            }

            // POST: Add new product
            if (req.Method == "POST")
            {
                var response = req.CreateResponse();
                try
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    var productInput = JsonConvert.DeserializeObject<ProductInputModel>(requestBody);

                    if (productInput == null || string.IsNullOrWhiteSpace(productInput.Name))
                    {
                        response.StatusCode = HttpStatusCode.BadRequest;
                        await response.WriteStringAsync(JsonConvert.SerializeObject(new { Error = "Product name is required." }));
                        return response;
                    }

                    // Upload image if provided
                    string? imageUrl = null;
                    if (!string.IsNullOrWhiteSpace(productInput.ImageBase64) && !string.IsNullOrEmpty(blobConnectionString))
                    {
                        try
                        {
                            byte[] imageBytes = Convert.FromBase64String(productInput.ImageBase64);
                            var blobServiceClient = new BlobServiceClient(blobConnectionString);
                            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                            string fileName = $"{Guid.NewGuid()}.jpg";
                            var blobClient = containerClient.GetBlobClient(fileName);

                            using var ms = new MemoryStream(imageBytes);
                            await blobClient.UploadAsync(ms, new BlobHttpHeaders { ContentType = "image/jpeg" });

                            imageUrl = blobClient.Uri.ToString();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to upload image: {ex.Message}");
                        }
                    }

                    // Generate incremental ProductID
                    int maxId = 0;
                    await foreach (var p in tableClient.QueryAsync<Product>())
                    {
                        if (int.TryParse(p.ProductID, out int id) && id > maxId)
                            maxId = id;
                    }

                    var product = new Product
                    {
                        PartitionKey = "ProductsPartition",
                        RowKey = Guid.NewGuid().ToString(),
                        ProductID = (maxId + 1).ToString(),
                        Name = productInput.Name,
                        Description = productInput.Description,
                        Price = productInput.Price,
                        Quantity = productInput.Quantity,
                        ImageUrl = imageUrl
                    };

                    await tableClient.AddEntityAsync(product);

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync(JsonConvert.SerializeObject(new
                    {
                        message = $"Product {product.Name} uploaded successfully.",
                        blobUrl = imageUrl,
                        ProductRowKey = product.RowKey
                    }));

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error uploading product.");
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    await response.WriteStringAsync(JsonConvert.SerializeObject(new { Error = ex.Message }));
                    return response;
                }
            }

            // Method not allowed
            var methodNotAllowed = req.CreateResponse(HttpStatusCode.MethodNotAllowed);
            await methodNotAllowed.WriteStringAsync("Only GET and POST methods are supported.");
            return methodNotAllowed;
        }

        private class ProductInputModel
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public double Price { get; set; }
            public int Quantity { get; set; }
            public string? ImageBase64 { get; set; }
        }

        public class Product : ITableEntity
        {
            public string PartitionKey { get; set; } = "ProductsPartition";
            public string RowKey { get; set; } = Guid.NewGuid().ToString();
            public string ProductID { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public double Price { get; set; }
            public int Quantity { get; set; }
            public string? ImageUrl { get; set; }
            public DateTimeOffset? Timestamp { get; set; } = null;
            public ETag ETag { get; set; } = ETag.All;
        }
    }
}
