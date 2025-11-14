using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10445050_CLDV6212_POE_Part1.DbContext;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;
using System.Threading.Tasks;

public class LoginController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly CustomerService _customerService;

    public LoginController(ApplicationDbContext context, CustomerService customerService)
    {
        _context = context;                     // Database context for SQL user storage
        _passwordHasher = new PasswordHasher<User>();  // Security: hashes passwords before saving
        _customerService = customerService;      // Service used to store customers in Azure Table Storage
    }

    // =====================================================
    // GET: Login page
    // Returns the login view
    // =====================================================
    public IActionResult Index()
    {
        return View();
    }

    // =====================================================
    // GET: Registration page
    // Displays the registration form
    // =====================================================
    public IActionResult Register()
    {
        return View();
    }

    // =====================================================
    // POST: Register a new user
    // Handles saving the user to SQL DB and Azure Table Storage
    // =====================================================
    [HttpPost]
    public async Task<IActionResult> Register(User user)
    {
        if (ModelState.IsValid)
        {
            // Prevent anyone from registering as Admin manually
            if (user.Email == "admin1@gmail.com" && user.Password == "admin123")
            {
                user.Role = "Admin";   // Admin role is enforced, not user-chosen
                TempData["Error"] = "Admin users are hardcoded and cannot be registered.";
                return RedirectToAction("Index"); // Redirect back to login
            }

            // Hash the password before saving it to SQL for security
            user.Password = _passwordHasher.HashPassword(user, user.Password);

            // Save the user into SQL Database (this is for Customer accounts only)
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate unique CustomerID based on the SQL row ID
            var lastCustomer = await _context.Users.OrderByDescending(u => u.Id).FirstOrDefaultAsync();
            int newCustomerId = lastCustomer != null ? lastCustomer.Id + 1 : 1;

            // Prepare customer data for Azure Table Storage
            var customer = new Customer
            {
                CustomerID = newCustomerId.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone
            };

            // Add customer to Azure Table Storage
            var result = await _customerService.AddCustomerAsync(customer);

            // If Azure insertion fails, return error
            if (result.StartsWith("Error"))
            {
                ModelState.AddModelError("", result);
                return View(user);
            }

            // Display a friendly success message
            ViewData["SuccessMessage"] = "Registration successful! You will be redirected to login in 5 seconds.";

            // Clear form fields
            ModelState.Clear();

            return View();
        }

        // If inputs fail validation, return view with existing data for correction
        return View(user);
    }

    // =====================================================
    // POST: Login
    // Validates user credentials for Admin or Customer
    // =====================================================
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        // Input validation to ensure fields are filled
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            TempData["Error"] = "Please enter both email and password.";
            return RedirectToAction("Index");
        }

        email = email.Trim().ToLower(); // Normalise email for consistent checking

        // Hardcoded Admin login credentials (for POE requirement)
        if (email == "admin1@gmail.com" && password == "admin123")
        {
            // Store Admin session so system knows the user is Admin
            HttpContext.Session.SetString("Username", "Admin");
            return RedirectToAction("Index", "Home");
        }

        // Check if the email exists in SQL Database for Customer login
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user != null)
        {
            // Verify hashed password against user input
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Success)
            {
                // Store basic session data for logged-in Customers
                HttpContext.Session.SetString("Username", user.FirstName);
                HttpContext.Session.SetString("Email", user.Email);

                // Redirect Customers to their dashboard
                return RedirectToAction("Index", "CustomerRole");
            }
        }

        // If login fails
        TempData["Error"] = "Invalid email or password.";
        return RedirectToAction("Index");
    }

    // =====================================================
    // LOGOUT
    // Clears all session data and returns to login page
    // =====================================================
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();   // Remove all session variables
        return RedirectToAction("Index");
    }
}
