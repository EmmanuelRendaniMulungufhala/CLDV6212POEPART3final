using ABC_Retailer.Models;
using ABC_Retailer.Models.Services;
using ABC_Retailer.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ABC_Retailer.Controllers
{
    public class AccountController : Controller
    {
        private readonly IFunctionsApi _functions;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IFunctionsApi functions, ILogger<AccountController> logger)
        {
            _functions = functions;
            _logger = logger;
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Store return URL for redirect after login
            ViewData["ReturnUrl"] = returnUrl;

            // Redirect if already logged in
            if (User.Identity?.IsAuthenticated ?? false)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _logger.LogInformation("Login attempt for user: {Username}", model.Username);

                var user = await _functions.AuthenticateUserAsync(model.Username, model.Password);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    _logger.LogWarning("Failed login attempt for user: {Username} - Authentication returned null", model.Username);
                    TempData["ErrorMessage"] = "Invalid username or password. Please try again.";
                    return View(model);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");
                    _logger.LogWarning("Login attempt for inactive user: {Username}", model.Username);
                    TempData["ErrorMessage"] = "Your account has been deactivated.";
                    return View(model);
                }

                _logger.LogInformation("User authenticated successfully: {Username}, Role: {Role}", user.Username, user.Role);

                // Create claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.RowKey),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.GivenName, user.FirstName),
                    new Claim(ClaimTypes.Surname, user.LastName),
                    new Claim(ClaimTypes.Role, user.Role) // This is critical for role-based authorization
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(1),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User {Username} signed in successfully with role {Role}", user.Username, user.Role);
                TempData["SuccessMessage"] = $"Welcome back, {user.FirstName}!";

                // Redirect based on role
                if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Redirecting admin user {Username} to Admin dashboard", user.Username);
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    _logger.LogInformation("Redirecting customer user {Username} to Home", user.Username);

                    // Check if there's a return URL
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                TempData["ErrorMessage"] = $"Login error: {ex.Message}";
                return View(model);
            }
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            // Redirect if already logged in
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _logger.LogInformation("Registration attempt for username: {Username}", model.Username);

                // Create new user - will be assigned Customer role by default
                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PasswordHash = model.Password, // Will be hashed in the RegisterUserAsync method
                    Role = "Customer", // Default role
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var createdUser = await _functions.RegisterUserAsync(newUser);

                _logger.LogInformation("New customer registered successfully: {Username}", createdUser.Username);
                TempData["SuccessMessage"] = "Registration successful! Please login with your credentials.";

                return RedirectToAction(nameof(Login));
            }
            catch (ArgumentException ex)
            {
                // Handle username/email already exists
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning("Registration failed for {Username}: {Message}", model.Username, ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
                TempData["ErrorMessage"] = $"Registration error: {ex.Message}";
                return View(model);
            }
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "Unknown";

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("User {Username} logged out successfully", username);
            TempData["SuccessMessage"] = "You have been logged out successfully.";

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning("Access denied for user: {Username}", User.Identity?.Name ?? "Anonymous");
            return View();
        }

        // GET: Account/Profile (for logged-in users to view their profile)
        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            var userInfo = new
            {
                Username = User.Identity?.Name,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                FirstName = User.FindFirst(ClaimTypes.GivenName)?.Value,
                LastName = User.FindFirst(ClaimTypes.Surname)?.Value,
                Role = User.FindFirst(ClaimTypes.Role)?.Value,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false
            };

            return View(userInfo);
        }
    }
}