using InventoryManagementPro.Models;

namespace InventoryManagementPro.Models.ViewModels
{
    public class DashboardVM
    {
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int InStockCount { get; set; }

        public int TotalOrders { get; set; }
        public decimal RevenueThisMonth { get; set; }

        public List<string> RevenueLabels { get; set; } = new();
        public List<decimal> RevenueData { get; set; } = new();
        public List<int> SalesCounts { get; set; } = new();
        public List<Product> RecentProducts { get; set; } = new();
}
}