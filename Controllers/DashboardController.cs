using InventoryManagementPro.Data;
using InventoryManagementPro.Models;
using InventoryManagementPro.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementPro.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;
        public DashboardController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var nowUtc = DateTime.UtcNow;
            var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1);
            var start6 = monthStart.AddMonths(-5);

            var totalProducts = await _db.Products.CountAsync();
            var lowStock = await _db.Products.CountAsync(p => p.Stock > 0 && p.Stock <= p.ReorderLevel);
            var outOfStock = await _db.Products.CountAsync(p => p.Stock <= 0);
            var inStock = totalProducts - lowStock - outOfStock;

            var totalOrders = await _db.Orders.CountAsync();

            var revenueThisMonth = await _db.Orders
                .Where(o => o.Status != "Cancelled" && o.OrderDateUtc >= monthStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var monthly = await _db.Orders
                .Where(o => o.Status != "Cancelled" && o.OrderDateUtc >= start6)
                .GroupBy(o => new { o.OrderDateUtc.Year, o.OrderDateUtc.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(x => x.TotalAmount),
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var labels = new List<string>();
            var revenueData = new List<decimal>();
            var salesCounts = new List<int>();

            for (int i = 0; i < 6; i++)
            {
                var dt = start6.AddMonths(i);
                labels.Add(dt.ToString("MMM"));

                var row = monthly.FirstOrDefault(x => x.Year == dt.Year && x.Month == dt.Month);

                revenueData.Add(row?.Total ?? 0m);
                salesCounts.Add(row?.Count ?? 0);
            }

            var recentProducts = await _db.Products
                .AsNoTracking()
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            var vm = new DashboardVM
            {
                TotalProducts = totalProducts,
                LowStockCount = lowStock,
                OutOfStockCount = outOfStock,
                InStockCount = inStock,
                TotalOrders = totalOrders,
                RevenueThisMonth = revenueThisMonth,

                RevenueLabels = labels,
                RevenueData = revenueData,
                SalesCounts = salesCounts,

                RecentProducts = recentProducts
            };

            return View(vm);
        }
    }
}