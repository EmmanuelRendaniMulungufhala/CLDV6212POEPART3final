using ABC_Retailer.Models;
using ABC_Retailer.Models.Services;
using ABC_Retailer.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailer.Controllers
{
    // HomeController is public - accessible to everyone (authenticated or not)
    public class HomeController : Controller
    {
        private readonly IFunctionsApi _functions;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IFunctionsApi functions, ILogger<HomeController> logger)
        {
            _functions = functions;
            _logger = logger;
        }

        // Public homepage - anyone can view
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading home page for user: {Username}",
                    User.Identity?.Name ?? "Anonymous");

                var customers = await _functions.GetCustomersAsync();
                var products = await _functions.GetProductsAsync();
                var orders = await _functions.GetOrdersAsync();

                _logger.LogInformation("Successfully loaded: {CustomerCount} customers, {ProductCount} products, {OrderCount} orders",
                    customers?.Count ?? 0, products?.Count ?? 0, orders?.Count ?? 0);

                var vm = new HomeViewModel
                {
                    CustomerCount = customers?.Count ?? 0,
                    ProductCount = products?.Count ?? 0,
                    OrderCount = orders?.Count ?? 0,
                    TotalRevenue = orders?.Sum(o => o.TotalPrice) ?? 0,
                    FeaturedProducts = products?.Take(6).ToList() ?? new List<Product>()
                };

                // Add user info to ViewBag for personalized greeting
                if (User.Identity?.IsAuthenticated ?? false)
                {
                    ViewBag.IsAuthenticated = true;
                    ViewBag.UserName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
                    ViewBag.UserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");

                var vm = new HomeViewModel
                {
                    CustomerCount = 0,
                    ProductCount = 0,
                    OrderCount = 0,
                    TotalRevenue = 0,
                    FeaturedProducts = new List<Product>()
                };

                TempData["ErrorMessage"] = $"Failed to load data: {ex.Message}";
                return View(vm);
            }
        }

        // Public action for testing API connection
        public async Task<IActionResult> TestApi()
        {
            try
            {
                var customers = await _functions.GetCustomersAsync();
                return Content($"API Test Successful! Loaded {customers?.Count} customers.");
            }
            catch (Exception ex)
            {
                return Content($"API Test Failed: {ex.Message}");
            }
        }

        // About page (optional)
        public IActionResult About()
        {
            return View();
        }

        // Contact page (optional)
        public IActionResult Contact()
        {
            return View();
        }

        // Privacy page (optional)
        public IActionResult Privacy()
        {
            return View();
        }
    }
}