using Microsoft.AspNetCore.Mvc;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;

namespace ST10445050_CLDV6212_POE_Part1.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableService _tableService;
        private readonly CustomerService _customerService;

        // Constructor receives services for interacting with Azure Table Storage
        public CustomersController(TableService tableService, CustomerService customerService)
        {
            _tableService = tableService;
            _customerService = customerService;
        }

        // ============================================
        // DISPLAY ALL CUSTOMERS
        // Loads customer list from Azure Table Storage
        // ============================================
        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return View(customers);
        }

        // ============================================
        // ADD CUSTOMER (GET)
        // Shows the form where admin can create a new customer
        // ============================================
        [HttpGet]
        public IActionResult AddCustomer()
        {
            return View();
        }

        // ============================================
        // ADD CUSTOMER (POST)
        // Saves the new customer to Azure Table Storage
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            var result = await _customerService.AddCustomerAsync(customer);

            // Handles service error messages
            if (result.StartsWith("Error") || result.StartsWith("Exception"))
            {
                ModelState.AddModelError("", result);
                return View(customer);
            }

            TempData["Message"] = result;
            return RedirectToAction("Index");
        }

        // ============================================
        // EDIT CUSTOMER (GET)
        // Retrieves the customer to display on the edit form
        // ============================================
        [HttpGet]
        public async Task<IActionResult> EditCustomer(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return BadRequest();

            var customer = await _tableService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // ============================================
        // EDIT CUSTOMER (POST)
        // Saves updated customer details
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(Customer customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            try
            {
                await _tableService.UpdateCustomerAsync(customer);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Could not update customer: {ex.Message}");
                return View(customer);
            }
        }

        // ============================================
        // DELETE CUSTOMER (GET)
        // Shows confirmation page before deleting
        // ============================================
        [HttpGet]
        public async Task<IActionResult> DeleteCustomer(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return BadRequest();

            var customer = await _tableService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // ============================================
        // DELETE CUSTOMER (POST)
        // Deletes the customer from Azure Table Storage
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _tableService.DeleteCustomerAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }

        // ============================================
        // CUSTOMER DETAILS
        // Displays full information of a single customer
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return BadRequest();

            var customer = await _tableService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
                return NotFound();

            return View(customer);
        }
    }
}
