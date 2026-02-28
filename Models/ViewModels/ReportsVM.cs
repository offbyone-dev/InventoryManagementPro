using InventoryManagementPro.Models;

namespace InventoryManagementPro.Models.ViewModels
{
    public class ReportsVM
    {
        public int RangeDays { get; set; } = 30;         
        public string ReportType { get; set; } = "Sales";   
        public string Format { get; set; } = "PDF";        
        public string? Q { get; set; }                      
        public decimal RevenueLastXDays { get; set; }
        public int OrdersLastXDays { get; set; }
        public int ProductsAddedLastXDays { get; set; }
        public List<string> Labels { get; set; } = new();
        public List<decimal> RevenueData { get; set; } = new();
        public List<ReportRecord> RecentReports { get; set; } = new();
    }
}