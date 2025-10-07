using Microsoft.AspNetCore.Mvc;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;

namespace ST10445050_CLDV6212_POE_Part1.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableService _tableService;
        private readonly CustomerService _customerService;

        public CustomersController(TableService tableService, CustomerService customerService )
        {
            _tableService = tableService;
            _customerService = customerService;
        }

        // ======================
        // LIST ALL CUSTOMERS
        // ======================
        public async Task<IActionResult> Index()
        {
            var customers = await _tableService.GetAllCustomersAsync();
            return View(customers);
        }

        [HttpGet]
        public IActionResult AddCustomer()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            var result = await _customerService.AddCustomerAsync(customer);

            if (result.StartsWith("Error") || result.StartsWith("Exception"))
            {
                ModelState.AddModelError("", result);
                return View(customer);
            }

            TempData["Message"] = result;
            return RedirectToAction("Index");
        }

       
    

// ======================
// EDIT (GET)
// ======================
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

        // ======================
        // EDIT (POST)
        // ======================
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

        // DELETE (GET)
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

        // DELETE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _tableService.DeleteCustomerAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }


        // ======================
        // DETAILS
        // ======================
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
