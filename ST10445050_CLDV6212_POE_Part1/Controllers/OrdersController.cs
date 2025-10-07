using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ST10445050_CLDV6212_POE_Part1.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TableService _tableService;
        private readonly QueueService _queueService;

        public OrdersController(TableService tableService, QueueService queueService)
        {
            _tableService = tableService;
            _queueService = queueService;
        }
        // =========================
        // LIST ORDERS + SEARCH
        // =========================
        public async Task<IActionResult> Index(string searchString)
        {
            var orders = await _tableService.GetAllOrdersAsync() ?? new List<Order>();
            var products = await _tableService.GetAllProductsAsync() ?? new List<Product>();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                // Build a lookup dictionary for products to avoid repeated FirstOrDefault
                var productLookup = products.ToDictionary(p => p.ProductID, p => p.Name, StringComparer.OrdinalIgnoreCase);

                orders = orders.Where(o =>
                    o.customerID.ToString().Contains(searchString, StringComparison.OrdinalIgnoreCase) || // search by customer ID
                    (productLookup.ContainsKey(o.productID) && productLookup[o.productID].Contains(searchString, StringComparison.OrdinalIgnoreCase)) || // search by product name
                    o.deliveryAddress.Contains(searchString, StringComparison.OrdinalIgnoreCase) // optional search by address
                ).ToList();
            }

            return View(orders);
        }

        // =========================
        // UPDATE ORDER STATUS
        // =========================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string partitionKey, string rowKey, string newStatus)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return BadRequest("Invalid order keys.");

            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            order.orderStatus = newStatus;
            await _tableService.UpdateOrderAsync(order); // Implement this in TableService

            TempData["StatusMessage"] = $"Order {order.orderID} status updated to {newStatus}.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ORDER DETAILS
        // =========================
        [HttpGet]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            return View(order);
        }

        // =========================
        // ADD ORDER (GET)
        // =========================
        public async Task<IActionResult> AddOrder()
        {
            var customers = await _tableService.GetAllCustomersAsync() ?? new List<Customer>();
            var products = await _tableService.GetAllProductsAsync() ?? new List<Product>();

            ViewData["Customers"] = customers;
            ViewData["Products"] = products;

            return View(new Order());
        }

        // =========================
        // ADD ORDER (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrder(Order order, int Quantity = 1)
        {
            var customers = await _tableService.GetAllCustomersAsync() ?? new List<Customer>();
            var products = await _tableService.GetAllProductsAsync() ?? new List<Product>();

            ViewData["Customers"] = customers;
            ViewData["Products"] = products;

            if (!ModelState.IsValid)
                return View(order);

            // Validate delivery date
            if (order.deliveryDate < DateTime.Parse("1900-01-01"))
                order.deliveryDate = DateTime.UtcNow.AddDays(1);
            order.deliveryDate = DateTime.SpecifyKind(order.deliveryDate, DateTimeKind.Utc);

            // Validate product
            var selectedProduct = products.FirstOrDefault(p => p.ProductID == order.productID);
            if (selectedProduct == null)
            {
                ModelState.AddModelError("productID", "Selected product not found.");
                return View(order);
            }

            if (Quantity < 1) Quantity = 1;
            order.orderTotal = selectedProduct.Price * Quantity;

            // Set Table Storage keys
            order.RowKey = Guid.NewGuid().ToString();
            order.PartitionKey = "OrdersPartition";

            // Incremental orderID
            order.orderID = (await _tableService.GetAllOrdersAsync()).Any()
                ? (await _tableService.GetAllOrdersAsync()).Max(o => o.orderID) + 1
                : 1;

            // Default order status
            order.orderStatus = "Pending";

            // Save order
            await _tableService.AddOrderAsync(order);

            // Queue notification
            var queueMessage = $"New Order Placed: ID={order.orderID}, Customer={order.customerID}, Product={order.productID}, Quantity={Quantity}, Total={order.orderTotal}";
            await _queueService.SendMessageAsync(queueMessage);

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE ORDER (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            return View(order);
        }

        // =========================
        // DELETE ORDER (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);
            if (order != null)
            {
                await _tableService.DeleteOrderAsync(partitionKey, rowKey);
                var queueMessage = $"Order Deleted: ID={order.orderID}, Customer={order.customerID}, Product={order.productID}";
                await _queueService.SendMessageAsync(queueMessage);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
