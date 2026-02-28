using ClosedXML.Excel;
using InventoryManagementPro.Data;
using InventoryManagementPro.Models;
using InventoryManagementPro.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace InventoryManagementPro.Controllers
{
    public class ReportsController : Controller
    {
        private readonly AppDbContext _db;
        public ReportsController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(int range = 30, string type = "Sales", string format = "PDF", string? q = null)
        {
            range = NormalizeRange(range);
            type = NormalizeType(type);
            format = NormalizeFormat(format);

            var nowUtc = DateTime.UtcNow;
            var fromUtc = nowUtc.AddDays(-range);

            var revenue = await _db.Orders.AsNoTracking()
                .Where(o => o.Status != "Cancelled" && o.OrderDateUtc >= fromUtc)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var ordersCount = await _db.Orders.AsNoTracking()
                .Where(o => o.Status != "Cancelled" && o.OrderDateUtc >= fromUtc)
                .CountAsync();

            var productsAdded = await _db.Products.AsNoTracking()
                .CountAsync(p => p.CreatedAtUtc >= fromUtc);
            var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1);
            var start6 = monthStart.AddMonths(-5);

            var monthly = await _db.Orders.AsNoTracking()
                .Where(o => o.Status != "Cancelled" && o.OrderDateUtc >= start6)
                .GroupBy(o => new { o.OrderDateUtc.Year, o.OrderDateUtc.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.TotalAmount) })
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

            var rr = _db.ReportRecords.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                rr = rr.Where(r =>
                    r.Title.Contains(q) ||
                    r.Type.Contains(q) ||
                    r.Format.Contains(q) ||
                    r.Status.Contains(q) ||
                    ("#R-" + r.Id).Contains(q));
            }

            var recentReports = await rr
                .OrderByDescending(r => r.CreatedUtc)
                .Take(15)
                .ToListAsync();

            var vm = new ReportsVM
            {
                RangeDays = range,
                ReportType = type,
                Format = format,
                Q = q,

                RevenueLastXDays = revenue,
                OrdersLastXDays = ordersCount,
                ProductsAddedLastXDays = productsAdded,

                Labels = labels,
                RevenueData = data,

                RecentReports = recentReports
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int range, string type, string format, string? submitAction)
        {
            range = NormalizeRange(range);
            type = NormalizeType(type);
            format = NormalizeFormat(format);

            var nowUtc = DateTime.UtcNow;
            var fromUtc = nowUtc.AddDays(-range);

            var revenue = await _db.Orders.AsNoTracking()
                .Where(o => o.Status != "Cancelled" && o.OrderDateUtc >= fromUtc)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var orders = await _db.Orders.AsNoTracking()
                .Where(o => o.Status != "Cancelled" && o.OrderDateUtc >= fromUtc)
                .CountAsync();

            var productsAdded = await _db.Products.AsNoTracking()
                .CountAsync(p => p.CreatedAtUtc >= fromUtc);

            byte[] bytes;
            string contentType;
            string fileName;

            if (format == "Excel")
                (bytes, contentType, fileName) = BuildExcel(type, range, revenue, orders, productsAdded);
            else
                (bytes, contentType, fileName) = BuildPdf(type, range, revenue, orders, productsAdded);
            if (string.Equals(submitAction, "export", StringComparison.OrdinalIgnoreCase))
                return File(bytes, contentType, fileName);
       
            var existing = await _db.ReportRecords
                .OrderByDescending(r => r.CreatedUtc)
                .FirstOrDefaultAsync(r =>
                    r.Type == type &&
                    r.Format == format &&
                    r.RangeDays == range
                );

            if (existing != null)
            {
                existing.Title = $"{type} Summary ({range} days)";
                existing.CreatedUtc = DateTime.UtcNow;
                existing.FileName = fileName;
                existing.ContentType = contentType;
                existing.FileContent = bytes;
            }
            else
            {
                var record = new ReportRecord
                {
                    Title = $"{type} Summary ({range} days)",
                    Type = type,
                    Format = format,
                    Status = "Ready",
                    RangeDays = range,
                    CreatedUtc = DateTime.UtcNow,
                    FileName = fileName,
                    ContentType = contentType,
                    FileContent = bytes
                };

                _db.ReportRecords.Add(record);
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { range, type, format });
        }

        public async Task<IActionResult> Details(int id)
        {
            var report = await _db.ReportRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();
            return View(report);
        }

        public async Task<IActionResult> Download(int id)
        {
            var report = await _db.ReportRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();
            return File(report.FileContent, report.ContentType, report.FileName);
        }

        private static int NormalizeRange(int range) => (range == 7 || range == 30 || range == 90 || range == 365) ? range : 30;

        private static string NormalizeType(string type)
        {
            type = (type ?? "").Trim();
            return type.Equals("Inventory", StringComparison.OrdinalIgnoreCase) ? "Inventory"
                 : type.Equals("LowStock", StringComparison.OrdinalIgnoreCase) ? "LowStock"
                 : "Sales";
        }

        private static string NormalizeFormat(string format)
        {
            format = (format ?? "").Trim();
            return format.Equals("Excel", StringComparison.OrdinalIgnoreCase) ? "Excel" : "PDF";
        }

        private static (byte[] bytes, string contentType, string fileName) BuildExcel(string type, int range, decimal revenue, int orders, int productsAdded)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Report");

            ws.Cell(1, 1).Value = "Report";
            ws.Cell(1, 2).Value = $"{type} Summary ({range} days)";

            ws.Cell(3, 1).Value = "Revenue";
            ws.Cell(3, 2).Value = revenue;

            ws.Cell(4, 1).Value = "Orders";
            ws.Cell(4, 2).Value = orders;

            ws.Cell(5, 1).Value = "Products Added";
            ws.Cell(5, 2).Value = productsAdded;

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            var fileName = $"Report_{type}_{range}days_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return (ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private static (byte[] bytes, string contentType, string fileName) BuildPdf(string type, int range, decimal revenue, int orders, int productsAdded)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var localNow = DateTime.Now; 

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text($"InventoryManagementPro - {type} Report ({range} days)")
                        .SemiBold().FontSize(16);

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Generated (Local): {localNow:yyyy-MM-dd HH:mm}");
                        col.Item().LineHorizontal(1);

                        col.Item().Text($"Revenue: ${revenue:0.00}");
                        col.Item().Text($"Orders: {orders}");
                        col.Item().Text($"Products Added: {productsAdded}");
                    });

                    page.Footer().AlignCenter().Text("Generated by InventoryManagementPro");
                });
            });

            var bytes = doc.GeneratePdf();
            var fileName = $"Report_{type}_{range}days_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            return (bytes, "application/pdf", fileName);
        }
    }
}