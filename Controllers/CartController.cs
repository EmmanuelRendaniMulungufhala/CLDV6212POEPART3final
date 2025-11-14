using ABC_Retailer.Models;
using ABC_Retailer.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailer.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly IFunctionsApi _functions;
        private readonly ILogger<CartController> _logger;

        public CartController(IFunctionsApi functions, ILogger<CartController> logger)
        {
            _functions = functions;
            _logger = logger;
        }

        // View cart
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var username = User.Identity?.Name ?? "";
                var cartItems = await _functions.GetCartItemsAsync(username);

                _logger.LogInformation("Customer {Username} viewing cart with {Count} items",
                    username, cartItems?.Count ?? 0);

                return View(cartItems ?? new List<Cart>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart for {Username}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Error loading cart: " + ex.Message;
                return View(new List<Cart>());
            }
        }

        // Add item to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            try
            {
                var username = User.Identity?.Name ?? "";
                await _functions.AddToCartAsync(username, productId, quantity);

                _logger.LogInformation("Customer {Username} added product {ProductId} (qty: {Quantity}) to cart",
                    username, productId, quantity);

                TempData["SuccessMessage"] = "Product added to cart successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart", productId);
                TempData["ErrorMessage"] = "Error adding to cart: " + ex.Message;
                return RedirectToAction("Index", "Product");
            }
        }

        // Remove item from cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                await _functions.RemoveFromCartAsync(cartItemId);

                _logger.LogInformation("Customer {Username} removed item {CartItemId} from cart",
                    User.Identity?.Name, cartItemId);

                TempData["SuccessMessage"] = "Item removed from cart successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
                TempData["ErrorMessage"] = "Error removing item: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Clear entire cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var username = User.Identity?.Name ?? "";
                await _functions.ClearCartAsync(username);

                _logger.LogInformation("Customer {Username} cleared their cart", username);

                TempData["SuccessMessage"] = "Cart cleared successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for {Username}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Error clearing cart: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Checkout (placeholder)
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var username = User.Identity?.Name ?? "";
                var cartItems = await _functions.GetCartItemsAsync(username);

                if (cartItems == null || !cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty!";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Customer {Username} proceeding to checkout with {Count} items",
                    username, cartItems.Count);

                return View(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout for {Username}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Error loading checkout: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Process checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout()
        {
            try
            {
                var username = User.Identity?.Name ?? "";
                var cartItems = await _functions.GetCartItemsAsync(username);

                if (cartItems == null || !cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty!";
                    return RedirectToAction(nameof(Index));
                }

                // Create orders from cart items
                foreach (var item in cartItems)
                {
                    // Find customer by username
                    var customers = await _functions.GetCustomersAsync();
                    var customer = customers?.FirstOrDefault(c =>
                        c.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (customer != null)
                    {
                        await _functions.CreateOrderAsync(customer.RowKey, item.ProductId, item.Quantity);
                    }
                }

                // Clear cart after successful checkout
                await _functions.ClearCartAsync(username);

                _logger.LogInformation("Customer {Username} completed checkout", username);

                TempData["SuccessMessage"] = "Checkout successful! Your orders have been placed.";
                return RedirectToAction("MyOrders", "Order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing checkout for {Username}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Error processing checkout: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}