using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ABC_Retailer.Models;
using ABC_Retailer.Models.Services;

namespace ABC_Retailer.Controllers
{
    public class ProductController : Controller
    {
        private readonly IFunctionsApi _functions;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IFunctionsApi functions, ILogger<ProductController> logger)
        {
            _functions = functions;
            _logger = logger;
        }

        // Public - anyone can view products
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _functions.GetProductsAsync();

                _logger.LogInformation("Loading {Count} products for user: {Username}",
                    products?.Count ?? 0, User.Identity?.Name ?? "Anonymous");

                return View(products ?? new List<Product>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["ErrorMessage"] = "Error loading products: " + ex.Message;
                return View(new List<Product>());
            }
        }

        // Public - anyone can view product details
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var product = await _functions.GetProductAsync(id);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Viewing product {ProductId} - {ProductName}",
                    id, product.ProductName);

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product {ProductId}", id);
                TempData["ErrorMessage"] = "Error loading product: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Admin only - create product
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product p, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(p);
            }

            try
            {
                await _functions.CreateProductAsync(p, imageFile);

                _logger.LogInformation("Product {ProductName} created by admin {Username}",
                    p.ProductName, User.Identity?.Name);

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", "Error creating product: " + ex.Message);
                return View(p);
            }
        }

        // Admin only - edit product
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var product = await _functions.GetProductAsync(id);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product {ProductId} for edit", id);
                TempData["ErrorMessage"] = "Error loading product: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Product p, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(p);
            }

            try
            {
                await _functions.UpdateProductAsync(id, p, imageFile);

                _logger.LogInformation("Product {ProductId} updated by admin {Username}",
                    id, User.Identity?.Name);

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                ModelState.AddModelError("", "Error updating product: " + ex.Message);
                return View(p);
            }
        }

        // Admin only - delete product
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _functions.DeleteProductAsync(id);

                _logger.LogInformation("Product {ProductId} deleted by admin {Username}",
                    id, User.Identity?.Name);

                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                TempData["ErrorMessage"] = "Error deleting product: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Public - search products
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            try
            {
                var allProducts = await _functions.GetProductsAsync();

                if (string.IsNullOrWhiteSpace(query))
                {
                    return View("Index", allProducts ?? new List<Product>());
                }

                var filteredProducts = allProducts?
                    .Where(p => p.ProductName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                p.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? new List<Product>();

                ViewBag.SearchQuery = query;
                ViewBag.ResultCount = filteredProducts.Count;

                _logger.LogInformation("Search for '{Query}' returned {Count} results",
                    query, filteredProducts.Count);

                return View("Index", filteredProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                TempData["ErrorMessage"] = "Error searching products: " + ex.Message;
                return View("Index", new List<Product>());
            }
        }
    }
}