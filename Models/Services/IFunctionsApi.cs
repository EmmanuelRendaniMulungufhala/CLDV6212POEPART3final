using ABC_Retailer.Models;
using Microsoft.AspNetCore.Http;

namespace ABC_Retailer.Models.Services
{
    public interface IFunctionsApi
    {
        // Authentication
        Task<User?> AuthenticateUserAsync(string username, string password);
        Task<User> RegisterUserAsync(User user);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByIdAsync(string id);

        // Customers
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<Customer> CreateCustomerAsync(Customer c);
        Task<Customer> UpdateCustomerAsync(string id, Customer c);
        Task DeleteCustomerAsync(string id);

        // Products
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string id);
        Task<Product> CreateProductAsync(Product p, IFormFile? imageFile);
        Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile);
        Task DeleteProductAsync(string id);

        // Orders
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string id);
        Task<Order> CreateOrderAsync(string customerId, string productId, int quantity);
        Task UpdateOrderStatusAsync(string id, string newStatus);
        Task DeleteOrderAsync(string id);

        // Cart
        Task<List<Cart>> GetCartItemsAsync(string customerUsername);
        Task<Cart> AddToCartAsync(string customerUsername, string productId, int quantity);
        Task RemoveFromCartAsync(int cartItemId);
        Task ClearCartAsync(string customerUsername);

        // Uploads
        Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName);
    }
}