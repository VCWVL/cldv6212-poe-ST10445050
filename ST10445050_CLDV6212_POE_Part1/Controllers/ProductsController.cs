using Microsoft.AspNetCore.Mvc;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;
using System.Globalization;

namespace ST10445050_CLDV6212_POE_Part1.Controllers
{
    public class ProductsController : Controller
    {
        // =========================
        // Dependencies
        // =========================

        // Service to handle blob storage operations (images)
        private readonly BlobService _blobService;

        // Service to handle table storage operations (products)
        private readonly TableService _tableService;

        private readonly ProductService _productService;

        // Constructor: inject services
        public ProductsController(BlobService blobService, TableService tableService, ProductService productService)
        {
            _blobService = blobService;
            _tableService = tableService;
            _productService = productService;

        }




        // =========================
        // DETAILS
        // =========================
        // GET: /Products/Details
        // Displays details of a single product
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null)
                return NotFound(); 

            var product = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
                return NotFound(); 

            return View(product);
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return View(products);
            }
            catch
            {
                return View(new List<ProductUploadModel>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(ProductUploadModel model)
        {
            try
            {
                await _productService.UploadProductAsync(model);
                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }
    




// =========================
// EDIT PRODUCT
// =========================
[HttpGet]
        public async Task<IActionResult> EditProduct(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound(); // Return 404 if keys missing

            var product = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
                return NotFound(); 

            return View(product); // Display edit form
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(IFormCollection form, IFormFile? imageFile)
        {
            var partitionKey = form["PartitionKey"];
            var rowKey = form["RowKey"];

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return BadRequest("Missing keys for product update.");

            var existingProduct = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (existingProduct == null)
                return NotFound();

            // Update product fields from form
            existingProduct.Name = form["Name"];
            existingProduct.Description = form["Description"];

            // Parse Quantity safely
            existingProduct.Quantity = int.TryParse(form["Quantity"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty)
                ? qty
                : existingProduct.Quantity;

            // Parse Price safely
            existingProduct.Price = double.TryParse(form["Price"], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double price)
                ? price
                : existingProduct.Price;

            // Update image if a new file is provided
            if (imageFile != null && imageFile.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    await _blobService.DeleteBlobAsync(existingProduct.ImageUrl);

                // Upload new image
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                existingProduct.ImageUrl = await _blobService.UploadAsync(imageFile.OpenReadStream(), fileName);
            }

            // Save updated product to Table Storage
            await _tableService.UpdateProductAsync(existingProduct);

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE PRODUCT
        // =========================
        [HttpGet]
        public async Task<IActionResult> DeleteProduct(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null)
                return NotFound();

            var product = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
                return NotFound();

            return View(product); // Display delete confirmation page
        }

        [HttpPost, ActionName("DeleteProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var product = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (product != null)
            {
                // Delete blob image if exists
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var blobName = Path.GetFileName(new Uri(product.ImageUrl).AbsolutePath);
                    await _blobService.DeleteBlobAsync(product.ImageUrl);
                }

                // Delete product from Table Storage
                await _tableService.DeleteProductAsync(partitionKey, rowKey);
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HELPER: Generate Unique ProductID
        // =========================
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
    }
}
