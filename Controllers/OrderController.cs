using ABC_Retailer.Models;
using ABC_Retailer.Models.Services;
using ABC_Retailer.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ABC_Retailer.Controllers
{
    // Orders require authentication
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IFunctionsApi _functions;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IFunctionsApi functions, ILogger<OrderController> logger)
        {
            _functions = functions;
            _logger = logger;
        }

        // View orders - Customer sees their own, Admin sees all
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var allOrders = await _functions.GetOrdersAsync();

                // If customer, filter to show only their orders
                if (User.IsInRole("Customer"))
                {
                    var username = User.Identity?.Name;
                    var orders = allOrders?.Where(o =>
                        o.CustomerName.Contains(username ?? "", StringComparison.OrdinalIgnoreCase))
                        .ToList() ?? new List<Order>();

                    _logger.LogInformation("Customer {Username} viewing {Count} orders", username, orders.Count);
                    return View(orders);
                }

                // Admin sees all orders
                _logger.LogInformation("Admin viewing all {Count} orders", allOrders?.Count ?? 0);
                return View(allOrders ?? new List<Order>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for {Username}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Error loading orders: " + ex.Message;
                return View(new List<Order>());
            }
        }

        // Customer-specific orders view
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyOrders()
        {
            try
            {
                var allOrders = await _functions.GetOrdersAsync();
                var username = User.Identity?.Name;

                var myOrders = allOrders?.Where(o =>
                    o.CustomerName.Contains(username ?? "", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(o => o.OrderDate)
                    .ToList() ?? new List<Order>();

                _logger.LogInformation("Customer {Username} has {Count} orders", username, myOrders.Count);
                return View(myOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for customer {Username}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Error loading your orders: " + ex.Message;
                return View(new List<Order>());
            }
        }

        // Create order - Both Customer and Admin can create orders
        [Authorize(Roles = "Customer,Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var vm = new OrderCreateViewModel
                {
                    Customers = await _functions.GetCustomersAsync() ?? new List<Customer>(),
                    Products = await _functions.GetProductsAsync() ?? new List<Product>()
                };

                _logger.LogInformation("Loading order creation form for {Username}", User.Identity?.Name);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order creation form");
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
                return View(new OrderCreateViewModel());
            }
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    vm.Customers = await _functions.GetCustomersAsync() ?? new List<Customer>();
                    vm.Products = await _functions.GetProductsAsync() ?? new List<Product>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reloading dropdown data");
                }
                return View(vm);
            }

            try
            {
                await _functions.CreateOrderAsync(vm.CustomerId, vm.ProductId, vm.Quantity);

                _logger.LogInformation("Order created by {Username} for Customer {CustomerId}, Product {ProductId}",
                    User.Identity?.Name, vm.CustomerId, vm.ProductId);

                TempData["SuccessMessage"] = "Order created successfully!";

                // Redirect based on role
                if (User.IsInRole("Customer"))
                {
                    return RedirectToAction(nameof(MyOrders));
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                ModelState.AddModelError("", "Error creating order: " + ex.Message);

                try
                {
                    vm.Customers = await _functions.GetCustomersAsync() ?? new List<Customer>();
                    vm.Products = await _functions.GetProductsAsync() ?? new List<Product>();
                }
                catch (Exception reloadEx)
                {
                    _logger.LogError(reloadEx, "Error reloading dropdown data");
                }
                return View(vm);
            }
        }

        // Delete order - Admin only
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _functions.DeleteOrderAsync(id);

                _logger.LogInformation("Order {OrderId} deleted by admin {Username}",
                    id, User.Identity?.Name);

                TempData["SuccessMessage"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                TempData["ErrorMessage"] = "Error deleting order: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // View order details - Customer can view their own, Admin can view all
        [Authorize(Roles = "Customer,Admin")]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var order = await _functions.GetOrderAsync(id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                // If customer, verify they own this order
                if (User.IsInRole("Customer"))
                {
                    var username = User.Identity?.Name;
                    if (!order.CustomerName.Contains(username ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Customer {Username} attempted to access order {OrderId} they don't own",
                            username, id);
                        return RedirectToAction("AccessDenied", "Account");
                    }
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for {OrderId}", id);
                TempData["ErrorMessage"] = "Error loading order details: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}