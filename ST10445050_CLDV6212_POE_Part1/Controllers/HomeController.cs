using Microsoft.AspNetCore.Mvc;
using ST10445050_CLDV6212_POE_Part1.Models;
using System.Diagnostics;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    // Constructor receives a logger for recording system messages and errors
    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // =====================================================
    // HOME / INDEX
    // This action decides where the user must go after login
    // Redirects based on whether the user is Admin or Customer
    // =====================================================
    public IActionResult Index()
    {
        // Check if a user session exists (meaning the user is logged in)
        var username = HttpContext.Session.GetString("Username");

        if (string.IsNullOrEmpty(username))
        {
            // If the session has no username, the user is not logged in
            // Redirect them to the Login page
            return RedirectToAction("Index", "Login");
        }

        // Check if the logged-in user is the Admin
        if (username == "Admin")
        {
            // If Admin logs in, they stay on the main Home page
            // Add a welcome message for display on the view
            ViewData["WelcomeMessage"] = $"Welcome, Admin {username}!";
            return View();
        }

        // Check if the user is a Customer (normal user)
        var role = HttpContext.Session.GetString("Role");

        if (role == "Customer")
        {
            // Customers should not see the Admin home page
            // Redirect them to the CustomerRole area dashboard
            return RedirectToAction("Index", "CustomerRole");
        }

        // If no valid role is found, redirect to Login page
        return RedirectToAction("Index", "Login");
    }

    // =====================================================
    // PRIVACY PAGE
    // Displays a static privacy policy view
    // =====================================================
    public IActionResult Privacy()
    {
        return View();
    }

    // =====================================================
    // ERROR PAGE
    // Displays error details when something goes wrong
    // Includes a RequestId that helps with debugging
    // =====================================================
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
