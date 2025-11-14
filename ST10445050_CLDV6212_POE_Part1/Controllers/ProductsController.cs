using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10445050_CLDV6212_POE_Part1.DbContext;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ST10445050_CLDV6212_POE_Part1.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BlobService _blobService;          // Service for blob uploads if needed
        private readonly TableService _tableService;        // Service for interacting with Azure Table Storage
        private readonly HttpClient _httpClient;            // Used to call Azure Functions
        private readonly ApplicationDbContext _context;     // SQL DB used for product details (Entity Framework)

        public ProductsController(
            BlobService blobService,
            TableService tableService,
            HttpClient httpClient,
            ApplicationDbContext context)
        {
            _blobService = blobService;
            _tableService = tableService;
            _httpClient = httpClient;
            _context = context;
        }

        // =====================================================
        // LIST PRODUCTS PAGE
        // Displays all products stored in Azure Table Storage
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var products = await _tableService.GetAllProductsAsync();
            return View(products);
        }

        // =====================================================
        // ADD PRODUCT (GET)
        // Opens the form for adding a new product
        // =====================================================
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        // =====================================================
        // ADD PRODUCT (POST)
        // Handles creating a new product and uploading an image
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product product, IFormFile? imageFile)
        {
            try
            {
                // Set default values if none provided
                if (product.Price == 0) product.Price = 0;
                if (product.Quantity == 0) product.Quantity = 0;

                // Generate unique ProductID and Table Storage keys
                product.ProductID = (await GenerateUniqueID()).ToString();
                product.PartitionKey = "ProductsPartition";
                product.RowKey = Guid.NewGuid().ToString();

                // -----------------------------------------------------
                // IMAGE UPLOAD VIA AZURE FUNCTION
                // Sends the file to Function App which uploads to Blob Storage
                // -----------------------------------------------------
                if (imageFile != null && imageFile.Length > 0)
                {
                    var content = new MultipartFormDataContent();
                    var fileContent = new StreamContent(imageFile.OpenReadStream());
                    fileContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);

                    content.Add(fileContent, "imageFile", imageFile.FileName);

                    var response = await _httpClient.PostAsync(
                        "https://st10445050-abcretails.azurewebsites.net", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        dynamic responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject(responseContent);

                        // URL returned from your Azure Function is saved here
                        product.ImageUrl = responseObject.blobUrl;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to upload image to Azure Function.");
                        return View(product);
                    }
                }

                // These are not used, so they are cleared
                product.ImageFile = null;
                product.ImageBase64 = null;

                // Save final product record into Azure Table Storage
                await _tableService.AddProductAsync(product);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving product: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while saving the product.");
                return View(product);
            }
        }

        // =====================================================
        // HELPER: Generate Unique Incremental Product ID
        // Used to maintain consistent numbering
        // =====================================================
        private async Task<int> GenerateUniqueID()
        {
            var products = await _tableService.GetAllProductsAsync();
            if (products.Any())
            {
                int maxID = products.Max(p => int.TryParse(p.ProductID, out var n) ? n : 0);
                return maxID + 1;
            }
            return 1;
        }

        // =====================================================
        // DELETE PRODUCT (GET)
        // Loads product details into confirmation page
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> DeleteProduct(string partitionKey, string rowKey)
        {
            var product = await _tableService.GetProductAsync(partitionKey, rowKey);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index");
            }

            return View(product);
        }

        // =====================================================
        // DELETE PRODUCT (POST)
        // Executes the delete operation in Table Storage
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(Product product)
        {
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index");
            }

            try
            {
                await _tableService.DeleteProductAsync(product.PartitionKey, product.RowKey);

                TempData["Success"] = "Product deleted successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // =====================================================
        // VIEW PRODUCT DETAILS
        // Fetches product from SQL DB instead of Table Storage
        // Because Entity Framework is used on the details page
        // =====================================================
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var product = await _context.Product
                .FirstOrDefaultAsync(p => p.PartitionKey == partitionKey && p.RowKey == rowKey);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // =====================================================
        // EDIT PRODUCT (GET)
        // Loads existing product into edit form
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> EditProduct(string partitionKey, string rowKey)
        {
            var product = await _tableService.GetProductAsync(partitionKey, rowKey);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index");
            }

            return View(product);
        }

        // =====================================================
        // EDIT PRODUCT (POST)
        // Saves updated product information back to Table Storage
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product product)
        {
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index");
            }

            try
            {
                await _tableService.UpdateProductAsync(product);

                TempData["Success"] = "Product updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
