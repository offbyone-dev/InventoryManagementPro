using InventoryManagementPro.Data;
using InventoryManagementPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementPro.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _db;

        public ProductsController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? q)
        {
            IQueryable<Product> query = _db.Products
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.Id);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p =>
                    p.Name.Contains(q) ||
                    (p.Category != null && p.Category.Contains(q)));
            }

            ViewBag.Search = q;

            var products = await query.ToListAsync();
            ViewBag.Suppliers = await _db.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Suppliers = await _db.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product input)
        {
            ViewBag.Suppliers = await _db.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            input.CreatedAtUtc = DateTime.UtcNow;

            _db.Products.Add(input);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Product added successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            ViewBag.Suppliers = await _db.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product input)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = await _db.Suppliers
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                return View(input);
            }

            var product = await _db.Products.FindAsync(input.Id);
            if (product == null) return NotFound();

            product.Name = input.Name;
            product.Category = input.Category;
            product.Price = input.Price;
            product.Stock = input.Stock;
            product.ReorderLevel = input.ReorderLevel;
            product.IsActive = input.IsActive;
            product.SupplierId = input.SupplierId;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Product deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}