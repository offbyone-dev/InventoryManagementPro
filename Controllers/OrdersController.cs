using InventoryManagementPro.Data;
using InventoryManagementPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace InventoryManagementPro.Controllers
{
    public class OrdersController : Controller
    {
        private readonly AppDbContext _db;

        public OrdersController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Create()
        {
            ViewBag.Products = _db.Products.AsNoTracking().ToList();
            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order, List<int> productIds, List<int> quantities)
        {
            ViewBag.Products = await _db.Products.AsNoTracking().ToListAsync();

            if (productIds == null || quantities == null || productIds.Count == 0 || productIds.Count != quantities.Count)
            {
                ModelState.AddModelError("", "Please add at least one order item with quantity.");
                return View(order);
            }

            var pairs = productIds
                .Select((pid, idx) => new { pid, qty = quantities[idx] })
                .Where(x => x.pid > 0 && x.qty > 0)
                .ToList();

            if (pairs.Count == 0)
            {
                ModelState.AddModelError("", "Please select product(s) and valid quantity.");
                return View(order);
            }

            var grouped = pairs
                .GroupBy(x => x.pid)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.qty) })
                .ToList();

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var ids = grouped.Select(x => x.ProductId).ToList();
                var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

                if (products.Count != ids.Count)
                {
                    ModelState.AddModelError("", "One or more selected products are invalid.");
                    return View(order);
                }

                foreach (var g in grouped)
                {
                    var p = products.First(x => x.Id == g.ProductId);
                    if (p.Stock < g.Qty)
                    {
                        ModelState.AddModelError("", $"Not enough stock for {p.Name}. Available: {p.Stock}, Requested: {g.Qty}");
                        return View(order);
                    }
                }

                order.Items = new List<OrderItem>();

                foreach (var g in grouped)
                {
                    var p = products.First(x => x.Id == g.ProductId);

                    order.Items.Add(new OrderItem
                    {
                        ProductId = p.Id,
                        Product = p,
                        Quantity = g.Qty,
                        UnitPrice = p.Price
                    });

                    p.Stock -= g.Qty;
                }
                order.TotalAmount = order.Items.Sum(i => i.LineTotal);

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError("", "Failed to create order. Please try again.");
                return View(order);
            }
        }
        public IActionResult Index(string? q)
        {
            var query = _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(o =>
                    o.OrderNo.Contains(q) ||
                    (o.CustomerName != null && o.CustomerName.Contains(q)) ||
                    o.Status.Contains(q));
            }

            var orders = query
                .OrderByDescending(o => o.OrderDateUtc)
                .ToList();

            return View(orders);
        }
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (order.Status == "Cancelled")
                return RedirectToAction(nameof(Index));

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in order.Items)
                {
                    var p = await _db.Products.FindAsync(item.ProductId);
                    if (p != null)
                        p.Stock += item.Quantity;
                }

                order.Status = "Cancelled";
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await tx.RollbackAsync();
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
