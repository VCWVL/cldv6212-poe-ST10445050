using Microsoft.AspNetCore.Mvc;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;

namespace ST10445050_CLDV6212_POE_Part1.Controllers
{
    public class CustomerRoleController : Controller
    {
        private readonly TableService _tableService;

        public CustomerRoleController(TableService tableService)
        {
            _tableService = tableService;
        }

        // =========================
        // Home Page
        // =========================
        public IActionResult Index()
        {
            return View();
        }

        // =========================
        // Display All Products
        // Loads all products from Azure Tables
        // =========================
        public async Task<IActionResult> Products()
        {
            var products = await _tableService.GetAllProductsAsync();
            return View(products);
        }

        // =========================
        // Product Details Page
        // Shows information for ONE product
        // =========================
        public async Task<IActionResult> Details(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return BadRequest();

            var products = await _tableService.GetAllProductsAsync();
            var product = products.FirstOrDefault(p => p.ProductID == productId);
            if (product == null) return NotFound();

            return View(product);
        }

        // =========================
        // Add Item To Cart
        // Saves cart information inside session storage
        // =========================
        [HttpPost]
        public IActionResult AddToCart(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return BadRequest();

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart")
                       ?? new List<CartItem>();

            var existingItem = cart.FirstOrDefault(c => c.ProductID == productId);

            if (existingItem != null)
                existingItem.Quantity++;
            else
                cart.Add(new CartItem { ProductID = productId, Quantity = 1 });

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            TempData["Message"] = "Product successfully added to cart.";

            return RedirectToAction(nameof(Products));
        }

        // =========================
        // View Cart
        // Builds display model with product names, prices, totals
        // =========================
        public async Task<IActionResult> Cart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart")
                       ?? new List<CartItem>();

            var products = await _tableService.GetAllProductsAsync();

            var cartDetails = cart.Select(c =>
            {
                var product = products.FirstOrDefault(p => p.ProductID == c.ProductID);

                return new CartViewModel
                {
                    ProductID = c.ProductID,
                    Name = product?.Name,
                    Price = product?.Price ?? 0,
                    Quantity = c.Quantity,
                    Total = (product?.Price ?? 0) * c.Quantity
                };
            }).ToList();

            ViewBag.Subtotal = cartDetails.Sum(c => c.Total);
            return View(cartDetails);
        }

        // =========================
        // Remove Item From Cart
        // =========================
        public IActionResult RemoveFromCart(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return BadRequest();

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart")
                       ?? new List<CartItem>();

            var item = cart.FirstOrDefault(c => c.ProductID == productId);
            if (item != null) cart.Remove(item);

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            TempData["Message"] = "Product successfully removed from cart.";

            return RedirectToAction(nameof(Cart));
        }

        // =========================
        // Process Order  
        // Creates ONE order for all items in the cart
        // =========================
        public async Task<IActionResult> ProcessOrder()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart")
                       ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Message"] = "Your cart is empty.";
                return RedirectToAction(nameof(Cart));
            }

            // Retrieve logged-in customer's email
            string email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Message"] = "You must be logged in to place an order.";
                return RedirectToAction("Index", "Login");
            }

            // Lookup the customer record in Azure Table Storage
            var customers = await _tableService.GetAllCustomersAsync();
            var thisCustomer = customers.FirstOrDefault(c => c.Email == email);

            if (thisCustomer == null)
            {
                TempData["Message"] = "Customer record not found.";
                return RedirectToAction(nameof(Cart));
            }

            var products = await _tableService.GetAllProductsAsync();
            var allOrders = await _tableService.GetAllOrdersAsync();

            double finalTotal = 0;
            List<string> productIds = new();

            // Calculate total order value and gather product IDs
            foreach (var item in cart)
            {
                var p = products.FirstOrDefault(x => x.ProductID == item.ProductID);
                if (p != null)
                {
                    finalTotal += p.Price * item.Quantity;
                    productIds.Add(p.ProductID);
                }
            }

            // Create ONE order that contains all product IDs
            var newOrder = new Order
            {
                PartitionKey = "OrdersPartition",
                RowKey = Guid.NewGuid().ToString(),

                customerID = int.Parse(thisCustomer.CustomerID),
                productID = string.Join(",", productIds),

                quantity = cart.Sum(x => x.Quantity),
                orderTotal = finalTotal,
                orderStatus = "Pending",
                orderDate = DateTime.UtcNow,

                orderID = allOrders.Any() ? allOrders.Max(o => o.orderID) + 1 : 1
            };

            await _tableService.AddOrderAsync(newOrder);

            // Save order to session so the Receipt page can display it
            HttpContext.Session.SetObjectAsJson("LastOrder", newOrder);

            // Clear the cart now that the order is placed
            HttpContext.Session.Remove("Cart");

            return RedirectToAction("Receipt");
        }

        // =========================
        // Receipt Page
        // Loads the LastOrder from session and displays it
        // =========================
        public IActionResult Receipt()
        {
            var order = HttpContext.Session.GetObjectFromJson<Order>("LastOrder");
            if (order == null)
                return RedirectToAction("Index");

            return View(order);
        }

        // =========================
        // Cart Session Models
        // Represents items stored within session
        // =========================
        public class CartItem
        {
            public string ProductID { get; set; }
            public int Quantity { get; set; }
        }

        public class OrderReceiptViewModel
        {
            public int OrderID { get; set; }
            public int CustomerID { get; set; }
            public string ProductID { get; set; }
            public int Quantity { get; set; }
            public double Total { get; set; }
            public DateTime OrderDate { get; set; }
            public string OrderStatus { get; set; }
        }
    }

    // =========================
    // Session Extensions
    // Converts objects to/from JSON for session storage
    // =========================
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value));
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null
                ? default
                : System.Text.Json.JsonSerializer.Deserialize<T>(value);
        }
    }
}
