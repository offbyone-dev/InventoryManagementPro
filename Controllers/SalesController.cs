using InventoryManagementPro.Data;
using InventoryManagementPro.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace InventoryManagementPro.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class SalesController : Controller
    {
        private readonly AppDbContext _db;
        public SalesController(AppDbContext db) { _db = db; }
        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            if (page < 1) page = 1;

            var nowUtc = DateTime.UtcNow;
            var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1);
            var start6 = monthStart.AddMonths(-5);
            var totalSales = await _db.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var thisMonthSales = await _db.Orders
                .Where(o => o.Status != "Cancelled" && o.OrderDateUtc >= monthStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var totalOrders = await _db.Orders.CountAsync();
            var monthly = await _db.Orders
                .Where(o => o.Status != "Cancelled" && o.OrderDateUtc >= start6)
                .GroupBy(o => new { o.OrderDateUtc.Year, o.OrderDateUtc.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<decimal>();

            for (int i = 0; i < 6; i++)
            {
                var dt = start6.AddMonths(i);
                labels.Add(dt.ToString("MMM"));
                var row = monthly.FirstOrDefault(x => x.Year == dt.Year && x.Month == dt.Month);
                data.Add(row?.Total ?? 0m);
            }
            var recentQuery = _db.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                recentQuery = recentQuery.Where(o =>
                    o.OrderNo.Contains(q) ||
                    (o.CustomerName != null && o.CustomerName.Contains(q)) ||
                    o.Status.Contains(q));
            }

            const int pageSize = 10;

            var totalCount = await recentQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var recentOrders = await recentQuery
                .OrderByDescending(o => o.OrderDateUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new SalesVM
            {
                TotalSales = totalSales,
                ThisMonthSales = thisMonthSales,
                TotalOrders = totalOrders,
                RevenueLabels = labels,
                RevenueData = data,

                RecentOrders = recentOrders,

                Query = q,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return View(vm);
        }
        [HttpGet]
        public async Task<IActionResult> ExportCsv(string? q)
        {
            var query = _db.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(o =>
                    o.OrderNo.Contains(q) ||
                    (o.CustomerName != null && o.CustomerName.Contains(q)) ||
                    o.Status.Contains(q));
            }

            var rows = await query
                .OrderByDescending(o => o.OrderDateUtc)
                .ToListAsync();

            static string CsvEscape(string? s)
            {
                s ??= "";
                if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
                    return "\"" + s.Replace("\"", "\"\"") + "\"";
                return s;
            }

            var sb = new StringBuilder();
            sb.AppendLine("OrderNo,Date,Customer,Amount,Status");

            foreach (var o in rows)
            {
                sb.Append(CsvEscape(o.OrderNo)).Append(',')
                  .Append(CsvEscape(o.OrderDateUtc.ToLocalTime().ToString("dd MMM yyyy"))).Append(',')
                  .Append(CsvEscape(o.CustomerName ?? "-")).Append(',')
                  .Append(o.TotalAmount.ToString("0.00")).Append(',')
                  .Append(CsvEscape(o.Status))
                  .AppendLine();
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"sales-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
        }
    }
}