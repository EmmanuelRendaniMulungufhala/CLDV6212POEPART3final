namespace ABC_Retailer.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public List<Product> LowStockProducts { get; set; } = new List<Product>();
    }
}