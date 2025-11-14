using ABC_Retailer.Models;
using ABC_Retailer.Models.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext with In-Memory Database for development
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("ABCRetailAuth"));

// Register SQL Service - This is safe, it only uses DbContext
builder.Services.AddScoped<IFunctionsApi, SqlFunctionsApi>();

// Configure Authentication with Cookie as default scheme
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireCustomer", policy => policy.RequireRole("Customer"));
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add these middleware in the correct order
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Seed initial data
    await SeedData.Initialize(context);
}

app.Run();

// Seed data class
public static class SeedData
{
    public static async Task Initialize(ApplicationDbContext context)
    {
        // Check if users already exist
        if (!context.Users.Any())
        {
            var users = new[]
            {
                new User
                {
                    RowKey = "1",
                    Username = "admin",
                    Email = "admin@abcretail.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = "Admin"
                },
                new User
                {
                    RowKey = "2",
                    Username = "john.doe",
                    Email = "john.doe@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    FirstName = "John",
                    LastName = "Doe",
                    Role = "Customer"
                }
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        // Check if customers already exist
        if (!context.Customers.Any())
        {
            var customers = new[]
            {
                new Customer
                {
                    RowKey = "1",
                    Name = "John",
                    Surname = "Doe",
                    Username = "johndoe",
                    Email = "john@example.com",
                    ShippingAddress = "123 Main St, City, State"
                },
                new Customer
                {
                    RowKey = "2",
                    Name = "Jane",
                    Surname = "Smith",
                    Username = "janesmith",
                    Email = "jane@example.com",
                    ShippingAddress = "456 Oak Ave, City, State"
                }
            };

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();
        }

        // Check if products already exist
        if (!context.Products.Any())
        {
            var products = new[]
            {
                new Product
                {
                    RowKey = "1",
                    ProductName = "Laptop",
                    Description = "High-performance laptop with 16GB RAM",
                    Price = 999.99m,
                    StockAvailable = 10
                },
                new Product
                {
                    RowKey = "2",
                    ProductName = "Wireless Mouse",
                    Description = "Ergonomic wireless mouse",
                    Price = 29.99m,
                    StockAvailable = 25
                },
                new Product
                {
                    RowKey = "3",
                    ProductName = "Mechanical Keyboard",
                    Description = "RGB mechanical keyboard",
                    Price = 89.99m,
                    StockAvailable = 15
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        // Check if orders already exist
        if (!context.Orders.Any())
        {
            var orders = new[]
            {
                new Order
                {
                    RowKey = "1",
                    CustomerId = "1",
                    CustomerName = "John Doe",
                    ProductId = "1",
                    ProductName = "Laptop",
                    OrderDate = DateTime.Now.AddDays(-2),
                    Quantity = 1,
                    UnitPrice = 999.99m,
                    TotalPrice = 999.99m,
                    Status = "Completed"
                },
                new Order
                {
                    RowKey = "2",
                    CustomerId = "2",
                    CustomerName = "Jane Smith",
                    ProductId = "2",
                    ProductName = "Wireless Mouse",
                    OrderDate = DateTime.Now.AddDays(-1),
                    Quantity = 2,
                    UnitPrice = 29.99m,
                    TotalPrice = 59.98m,
                    Status = "Pending"
                }
            };

            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();
        }
    }
}