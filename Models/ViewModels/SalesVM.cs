using InventoryManagementPro.Models;

namespace InventoryManagementPro.Models.ViewModels
{
    public class SalesVM
    {
        public decimal TotalSales { get; set; }
        public decimal ThisMonthSales { get; set; }
        public int TotalOrders { get; set; }

        public List<string> RevenueLabels { get; set; } = new();
        public List<decimal> RevenueData { get; set; } = new();

        public List<Order> RecentOrders { get; set; } = new();
        public string? Query { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
    }
}