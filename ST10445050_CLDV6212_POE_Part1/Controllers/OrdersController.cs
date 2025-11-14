using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ST10445050_CLDV6212_POE_Part1.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TableService _tableService;
        private readonly QueueService _queueService;
        private readonly HttpClient _httpClient;

        public OrdersController(TableService tableService, QueueService queueService)
        {
            _tableService = tableService;   // Interacts with Azure Table Storage (Orders, Customers, Products)
            _queueService = queueService;   // Sends queue messages for monitoring or processing
            _httpClient = new HttpClient(); // Used to trigger Azure Functions via POST
        }

        // =====================================================
        // LIST ALL ORDERS + SEARCH BAR
        // Supports searching by:
        // OrderID OR Product Name
        // =====================================================
        public async Task<IActionResult> Index(string? searchString)
        {
            // Retrieve all orders and products at once
            var orders = await _tableService.GetAllOrdersAsync() ?? new List<Order>();
            var products = await _tableService.GetAllProductsAsync() ?? new List<Product>();

            // Build dictionary for quick product name lookup using productID
            var productLookup = products
                .Where(p => !string.IsNullOrEmpty(p.ProductID) && !string.IsNullOrEmpty(p.Name))
                .ToDictionary(p => p.ProductID, p => p.Name, StringComparer.OrdinalIgnoreCase);

            ViewBag.Message = null;

            // If the user typed a search term, apply filtering logic
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                // Filtering logic supports:
                // OrderID, CustomerID and each product name inside multi-product orders
                orders = orders.Where(o =>
                {
                    // Match order ID
                    if (o.orderID.ToString().Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        return true;

                    // Match customer ID
                    if (o.customerID.ToString().Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        return true;

                    // Handle orders with multiple products (split by commas)
                    if (!string.IsNullOrEmpty(o.productID))
                    {
                        var productIds = o.productID
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        foreach (var pid in productIds)
                        {
                            if (productLookup.TryGetValue(pid, out var productName) &&
                                productName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }).ToList();

                // Display a friendly "no results" message
                if (!orders.Any())
                {
                    ViewBag.Message = $" Sorry, no orders found matching \"{searchString}\".";
                }
            }

            // Always display newest orders first
            orders = orders.OrderByDescending(o => o.orderID).ToList();

            return View(orders);
        }

        // =====================================================
        // UPDATE ORDER STATUS (Pending → Processed → Shipped etc.)
        // Called from dropdown on Index page
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string partitionKey, string rowKey, string newStatus)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return BadRequest("Invalid order keys.");

            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            // Update the status column in Table Storage
            order.orderStatus = newStatus;
            await _tableService.UpdateOrderAsync(order);

            TempData["StatusMessage"] = $"Order {order.orderID} status updated to {newStatus}.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // VIEW ORDER DETAILS
        // Shows all fields of a single order
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            return View(order);
        }

        // =====================================================
        // ADD ORDER (GET)
        // Loads customers and products for dropdowns
        // =====================================================
        public async Task<IActionResult> AddOrder()
        {
            var customers = await _tableService.GetAllCustomersAsync() ?? new List<Customer>();
            var products = await _tableService.GetAllProductsAsync() ?? new List<Product>();

            // These lists feed the dropdown menus
            ViewData["Customers"] = customers;
            ViewData["Products"] = products;

            return View(new Order());
        }

        // =====================================================
        // ADD ORDER (POST)
        // Validates product, calculates totals, saves to Table Storage
        // Also triggers Azure Functions + queue messaging
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrder(Order order, int quantity = 1)
        {
            var customers = await _tableService.GetAllCustomersAsync() ?? new List<Customer>();
            var products = await _tableService.GetAllProductsAsync() ?? new List<Product>();

            ViewData["Customers"] = customers;
            ViewData["Products"] = products;

            // Validation check
            if (!ModelState.IsValid)
                return View(order);

            // Check product exists
            var selectedProduct = products.FirstOrDefault(p => p.ProductID == order.productID);
            if (selectedProduct == null)
            {
                ModelState.AddModelError("productID", "Selected product not found.");
                return View(order);
            }

            // Minimum quantity rule
            if (quantity < 1) quantity = 1;
            order.quantity = quantity;

            // Calculate price
            order.orderTotal = selectedProduct.Price * quantity;

            // Set Table Storage keys
            order.RowKey = Guid.NewGuid().ToString();
            order.PartitionKey = "OrdersPartition";

            // Increment orderID manually
            var allOrders = await _tableService.GetAllOrdersAsync();
            order.orderID = allOrders.Any() ? allOrders.Max(o => o.orderID) + 1 : 1;

            // Default status
            if (string.IsNullOrEmpty(order.orderStatus))
                order.orderStatus = "Pending";

            // Timestamp
            order.orderDate = DateTime.UtcNow;

            // Save to Table Storage
            await _tableService.AddOrderAsync(order);

            // =====================================================
            // CALL AZURE FUNCTION (HTTP TRIGGER)
            // Sends entire order as JSON
            // =====================================================
            try
            {
                string functionUrl = "https://st10445050-abcretails.azurewebsites.net";

                var orderJson = JsonSerializer.Serialize(order);
                var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(functionUrl, content);

                // Basic logging (developer feedback)
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($" Order {order.orderID} sent to Azure Function.");
                }
                else
                {
                    Console.WriteLine($" Azure Function rejected order {order.orderID}. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error calling Azure Function: {ex.Message}");
            }

            // =====================================================
            // SEND LOCAL QUEUE MESSAGE (Azure Queue Storage)
            // Useful for tracking activities or simulating workflows
            // =====================================================
            var queueMessage =
                $"New Order Created: ID={order.orderID}, Customer={order.customerID}, Product={order.productID}, Quantity={quantity}, Total={order.orderTotal}, Status={order.orderStatus}";

            await _queueService.SendMessageAsync(queueMessage);

            TempData["StatusMessage"] = $"Order {order.orderID} created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DELETE ORDER (GET)
        // Shows confirmation screen
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            return View(order);
        }

        // =====================================================
        // DELETE ORDER (POST)
        // Deletes from Table Storage and logs a queue message
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);
            if (order != null)
            {
                await _tableService.DeleteOrderAsync(partitionKey, rowKey);

                // Log deletion to Azure Queue
                var queueMessage =
                    $"Order Deleted: ID={order.orderID}, Customer={order.customerID}, Product={order.productID}";

                await _queueService.SendMessageAsync(queueMessage);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
