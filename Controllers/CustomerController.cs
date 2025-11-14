using ABC_Retailer.Models;
using ABC_Retailer.Models.Services;
using ABC_Retailer.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABC_Retailer.Controllers
{
    [Authorize(Roles = "Customer,Admin")]
    public class CustomerController : Controller
    {
        private readonly IFunctionsApi _functions;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(IFunctionsApi functions, ILogger<CustomerController> logger)
        {
            _functions = functions;
            _logger = logger;
        }

        // Customer Dashboard - Shows their orders
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _functions.GetOrdersAsync();
                var customerOrders = orders?.Where(o =>
                    o.CustomerName.Contains(User.Identity?.Name ?? "")).ToList() ?? new List<Order>();

                _logger.LogInformation("Customer {Username} viewing their orders", User.Identity?.Name);
                return View(customerOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer orders for {Username}", User.Identity?.Name);
                TempData["ErrorMessage"] = $"Error loading orders: {ex.Message}";
                return View(new List<Order>());
            }
        }

        // Customer order creation
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> CreateOrder()
        {
            try
            {
                var vm = new OrderCreateViewModel
                {
                    Customers = await _functions.GetCustomersAsync() ?? new List<Customer>(),
                    Products = await _functions.GetProductsAsync() ?? new List<Product>()
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
                return View(new OrderCreateViewModel());
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(OrderCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    vm.Customers = await _functions.GetCustomersAsync() ?? new List<Customer>();
                    vm.Products = await _functions.GetProductsAsync() ?? new List<Product>();
                }
                catch
                {
                    // If reloading fails, continue with existing data
                }
                return View(vm);
            }

            try
            {
                await _functions.CreateOrderAsync(vm.CustomerId, vm.ProductId, vm.Quantity);
                TempData["SuccessMessage"] = "Order created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error creating order: " + ex.Message);
                try
                {
                    vm.Customers = await _functions.GetCustomersAsync() ?? new List<Customer>();
                    vm.Products = await _functions.GetProductsAsync() ?? new List<Product>();
                }
                catch
                {
                    // If reloading fails, continue with existing data
                }
                return View(vm);
            }
        }

        // Admin-only customer management (if needed)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageCustomers()
        {
            try
            {
                _logger.LogInformation("Loading customers list...");
                var customers = await _functions.GetCustomersAsync();
                _logger.LogInformation($"Successfully loaded {customers?.Count} customers");
                return View(customers ?? new List<Customer>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
                TempData["ErrorMessage"] = $"Error loading customers: {ex.Message}";
                return View(new List<Customer>());
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateCustomer()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Customer creation failed validation");
                return View(customer);
            }

            try
            {
                _logger.LogInformation("Creating new customer: {Name} {Surname}", customer.Name, customer.Surname);

                // Ensure keys are set
                if (string.IsNullOrEmpty(customer.RowKey))
                    customer.RowKey = Guid.NewGuid().ToString();

                if (string.IsNullOrEmpty(customer.PartitionKey))
                    customer.PartitionKey = "CUSTOMER";

                // Ensure active status is set
                customer.IsActive = true;

                var createdCustomer = await _functions.CreateCustomerAsync(customer);

                _logger.LogInformation("Customer created successfully with ID: {RowKey}", createdCustomer.RowKey);

                TempData["SuccessMessage"] = $"Customer '{customer.Name} {customer.Surname}' created successfully!";
                return RedirectToAction(nameof(ManageCustomers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                return View(customer);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditCustomer(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Customer ID is required.";
                return RedirectToAction(nameof(ManageCustomers));
            }

            try
            {
                _logger.LogInformation("Loading customer for edit: {CustomerId}", id);
                var customer = await _functions.GetCustomerAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(ManageCustomers));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer {CustomerId} for edit", id);
                TempData["ErrorMessage"] = $"Error loading customer: {ex.Message}";
                return RedirectToAction(nameof(ManageCustomers));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(string id, Customer customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            try
            {
                _logger.LogInformation("Updating customer: {CustomerId}", id);
                await _functions.UpdateCustomerAsync(id, customer);
                TempData["SuccessMessage"] = "Customer updated successfully!";
                return RedirectToAction(nameof(ManageCustomers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", id);
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(customer);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Customer ID is required.";
                return RedirectToAction(nameof(ManageCustomers));
            }

            try
            {
                _logger.LogInformation("Deleting customer: {CustomerId}", id);
                await _functions.DeleteCustomerAsync(id);
                TempData["SuccessMessage"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
                TempData["ErrorMessage"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(ManageCustomers));
        }
    }
}