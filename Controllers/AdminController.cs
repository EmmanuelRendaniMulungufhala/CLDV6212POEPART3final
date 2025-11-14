using ABC_Retailer.Models;
using ABC_Retailer.Models.Services;
using ABC_Retailer.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IFunctionsApi _functions;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IFunctionsApi functions, ILogger<AdminController> logger)
        {
            _functions = functions;
            _logger = logger;
        }

        // =====================
        // DASHBOARD
        // =====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var customers = await _functions.GetCustomersAsync();
                var products = await _functions.GetProductsAsync();
                var orders = await _functions.GetOrdersAsync();

                var vm = new AdminDashboardViewModel
                {
                    TotalCustomers = customers?.Count ?? 0,
                    TotalProducts = products?.Count ?? 0,
                    TotalOrders = orders?.Count ?? 0,
                    PendingOrders = orders?.Count(o => o.Status == "Pending") ?? 0,
                    TotalRevenue = orders?.Sum(o => o.TotalPrice) ?? 0,
                    RecentOrders = orders?.Take(10).ToList() ?? new List<Order>(),
                    LowStockProducts = products?.Where(p => p.StockAvailable < 10).ToList() ?? new List<Product>()
                };

                _logger.LogInformation("Admin dashboard loaded by {Username}", User.Identity?.Name);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = $"Error loading dashboard: {ex.Message}";
                return View(new AdminDashboardViewModel());
            }
        }

        // =====================
        // ORDER MANAGEMENT
        // =====================
        public async Task<IActionResult> Orders()
        {
            try
            {
                var orders = await _functions.GetOrdersAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["ErrorMessage"] = $"Error loading orders: {ex.Message}";
                return View(new List<Order>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(string orderId, string status)
        {
            try
            {
                await _functions.UpdateOrderStatusAsync(orderId, status);
                TempData["SuccessMessage"] = $"Order status updated to {status} successfully!";
                _logger.LogInformation("Order {OrderId} status updated to {Status} by admin {Username}",
                    orderId, status, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", orderId);
                TempData["ErrorMessage"] = $"Error updating order status: {ex.Message}";
            }

            // Check if we came from dashboard or orders page
            var referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("/Admin/Orders"))
            {
                return RedirectToAction(nameof(Orders));
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(string id)
        {
            try
            {
                await _functions.DeleteOrderAsync(id);
                TempData["SuccessMessage"] = "Order deleted successfully!";
                _logger.LogInformation("Order {OrderId} deleted by admin {Username}", id, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                TempData["ErrorMessage"] = $"Error deleting order: {ex.Message}";
            }

            return RedirectToAction(nameof(Orders));
        }

        // =====================
        // CUSTOMER MANAGEMENT
        // =====================
        public async Task<IActionResult> Customers()
        {
            try
            {
                var customers = await _functions.GetCustomersAsync();
                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
                TempData["ErrorMessage"] = $"Error loading customers: {ex.Message}";
                return View(new List<Customer>());
            }
        }

        public IActionResult CreateCustomer()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _functions.CreateCustomerAsync(customer);
                    TempData["SuccessMessage"] = "Customer created successfully!";
                    _logger.LogInformation("Customer {CustomerName} created by admin {Username}",
                        customer.Name, User.Identity?.Name);
                    return RedirectToAction(nameof(Customers));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating customer");
                    ModelState.AddModelError("", $"Failed to create customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        public async Task<IActionResult> EditCustomer(string id)
        {
            try
            {
                var customer = await _functions.GetCustomerAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Customers));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer {CustomerId}", id);
                TempData["ErrorMessage"] = $"Error loading customer: {ex.Message}";
                return RedirectToAction(nameof(Customers));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(string id, Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _functions.UpdateCustomerAsync(id, customer);
                    TempData["SuccessMessage"] = "Customer updated successfully!";
                    _logger.LogInformation("Customer {CustomerId} updated by admin {Username}",
                        id, User.Identity?.Name);
                    return RedirectToAction(nameof(Customers));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating customer {CustomerId}", id);
                    ModelState.AddModelError("", $"Failed to update customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            try
            {
                await _functions.DeleteCustomerAsync(id);
                TempData["SuccessMessage"] = "Customer deleted successfully!";
                _logger.LogInformation("Customer {CustomerId} deleted by admin {Username}",
                    id, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
                TempData["ErrorMessage"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction(nameof(Customers));
        }

        // =====================
        // PRODUCT MANAGEMENT
        // =====================
        public async Task<IActionResult> Products()
        {
            try
            {
                var products = await _functions.GetProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["ErrorMessage"] = $"Error loading products: {ex.Message}";
                return View(new List<Product>());
            }
        }

        public IActionResult CreateProduct()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _functions.CreateProductAsync(product, imageFile);
                    TempData["SuccessMessage"] = "Product created successfully!";
                    _logger.LogInformation("Product {ProductName} created by admin {Username}",
                        product.ProductName, User.Identity?.Name);
                    return RedirectToAction(nameof(Products));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Failed to create product: {ex.Message}");
                }
            }
            return View(product);
        }

        public async Task<IActionResult> EditProduct(string id)
        {
            try
            {
                var product = await _functions.GetProductAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(Products));
                }
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product {ProductId}", id);
                TempData["ErrorMessage"] = $"Error loading product: {ex.Message}";
                return RedirectToAction(nameof(Products));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(string id, Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _functions.UpdateProductAsync(id, product, imageFile);
                    TempData["SuccessMessage"] = "Product updated successfully!";
                    _logger.LogInformation("Product {ProductId} updated by admin {Username}",
                        id, User.Identity?.Name);
                    return RedirectToAction(nameof(Products));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product {ProductId}", id);
                    ModelState.AddModelError("", $"Failed to update product: {ex.Message}");
                }
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            try
            {
                await _functions.DeleteProductAsync(id);
                TempData["SuccessMessage"] = "Product deleted successfully!";
                _logger.LogInformation("Product {ProductId} deleted by admin {Username}",
                    id, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                TempData["ErrorMessage"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Products));
        }
    }
}