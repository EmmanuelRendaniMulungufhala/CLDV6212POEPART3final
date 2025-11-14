using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;

namespace ABC_Retailer.Models.Services
{
    public class SqlFunctionsApi : IFunctionsApi
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SqlFunctionsApi> _logger;

        public SqlFunctionsApi(ApplicationDbContext context, ILogger<SqlFunctionsApi> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =====================
        // AUTHENTICATION METHODS
        // =====================
        public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            _logger.LogInformation("Authenticating user: {Username}", username);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => (u.Username == username || u.Email == username) && u.IsActive);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User authenticated successfully: {Username}", username);
                return user;
            }

            _logger.LogWarning("Authentication failed for user: {Username}", username);
            return null;
        }

        public async Task<User> RegisterUserAsync(User user)
        {
            _logger.LogInformation("Registering new user: {Username}", user.Username);

            // Check if username or email already exists
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                throw new ArgumentException("Username already exists");

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                throw new ArgumentException("Email already exists");

            user.PartitionKey = "USER";
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.Role = "Customer";
            user.IsActive = true;
            user.CreatedDate = DateTime.UtcNow;
            user.LastLogin = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Username}", user.Username);
            return user;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _context.Users.FindAsync(id);
        }

        // =====================
        // CUSTOMERS
        // =====================
        public async Task<List<Customer>> GetCustomersAsync()
        {
            return await _context.Customers.Where(c => c.IsActive).ToListAsync();
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task<Customer> CreateCustomerAsync(Customer c)
        {
            if (string.IsNullOrEmpty(c.RowKey))
                c.RowKey = Guid.NewGuid().ToString();

            c.PartitionKey = "CUSTOMER";
            c.IsActive = true;

            _context.Customers.Add(c);
            await _context.SaveChangesAsync();

            return c;
        }

        public async Task<Customer> UpdateCustomerAsync(string id, Customer c)
        {
            var existingCustomer = await _context.Customers.FindAsync(id);
            if (existingCustomer != null)
            {
                existingCustomer.Name = c.Name;
                existingCustomer.Surname = c.Surname;
                existingCustomer.Username = c.Username;
                existingCustomer.Email = c.Email;
                existingCustomer.ShippingAddress = c.ShippingAddress;
                existingCustomer.IsActive = c.IsActive;

                await _context.SaveChangesAsync();
            }
            return c;
        }

        public async Task DeleteCustomerAsync(string id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
        }

        // =====================
        // PRODUCTS
        // =====================
        public async Task<List<Product>> GetProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product p, IFormFile? imageFile)
        {
            if (string.IsNullOrEmpty(p.RowKey))
                p.RowKey = Guid.NewGuid().ToString();

            p.PartitionKey = "PRODUCT";

            _context.Products.Add(p);
            await _context.SaveChangesAsync();

            return p;
        }

        public async Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile)
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct != null)
            {
                existingProduct.ProductName = p.ProductName;
                existingProduct.Description = p.Description;
                existingProduct.Price = p.Price;
                existingProduct.StockAvailable = p.StockAvailable;

                await _context.SaveChangesAsync();
            }
            return p;
        }

        public async Task DeleteProductAsync(string id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        // =====================
        // ORDERS
        // =====================
        public async Task<List<Order>> GetOrdersAsync()
        {
            return await _context.Orders.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<Order?> GetOrderAsync(string id)
        {
            return await _context.Orders.FindAsync(id);
        }

        public async Task<Order> CreateOrderAsync(string customerId, string productId, int quantity)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            var product = await _context.Products.FindAsync(productId);

            if (customer == null || product == null)
            {
                throw new ArgumentException("Customer or product not found");
            }

            var order = new Order
            {
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = "ORDER",
                CustomerId = customerId,
                CustomerName = $"{customer.Name} {customer.Surname}",
                ProductId = productId,
                ProductName = product.ProductName,
                OrderDate = DateTime.Now,
                Quantity = quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * quantity,
                Status = "Pending"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task UpdateOrderStatusAsync(string id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = newStatus;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteOrderAsync(string id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        // =====================
        // CART METHODS
        // =====================
        public async Task<List<Cart>> GetCartItemsAsync(string customerUsername)
        {
            return await _context.Cart
                .Where(c => c.CustomerUsername == customerUsername)
                .OrderByDescending(c => c.AddedDate)
                .ToListAsync();
        }

        public async Task<Cart> AddToCartAsync(string customerUsername, string productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                throw new ArgumentException("Product not found");

            var cartItem = new Cart
            {
                CustomerUsername = customerUsername,
                ProductId = productId,
                ProductName = product.ProductName,
                Quantity = quantity,
                UnitPrice = product.Price
            };

            _context.Cart.Add(cartItem);
            await _context.SaveChangesAsync();

            return cartItem;
        }

        public async Task RemoveFromCartAsync(int cartItemId)
        {
            var cartItem = await _context.Cart.FindAsync(cartItemId);
            if (cartItem != null)
            {
                _context.Cart.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string customerUsername)
        {
            var cartItems = await _context.Cart
                .Where(c => c.CustomerUsername == customerUsername)
                .ToListAsync();

            _context.Cart.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
        }

        // =====================
        // UPLOADS
        // =====================
        public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName)
        {
            // For in-memory database, we'll just return a mock file name
            var fileName = $"uploaded_{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}";
            return await Task.FromResult(fileName);
        }
    }
}