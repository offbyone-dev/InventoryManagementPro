using InventoryManagementPro.Data;
using InventoryManagementPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementPro.Controllers;

public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;
    public async Task<IActionResult> Index(string? q)
    {
        IQueryable<Product> query = _db.Products.OrderByDescending(p => p.Id);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p =>
                p.Name.Contains(q) ||
                (p.Category != null && p.Category.Contains(q)) ||
                (p.Sku != null && p.Sku.Contains(q)));
        }

        ViewBag.Search = q;
        var products = await query.ToListAsync();
        return View(products);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product input)
    {
        if (!ModelState.IsValid)
        {
            var products = await _db.Products
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View("Index", products);
        }

        input.CreatedAtUtc = DateTime.UtcNow;
        _db.Products.Add(input);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Product added successfully!";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Product deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        return View(product);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product input)
    {
        if (!ModelState.IsValid)
            return View(input);

        var product = await _db.Products.FindAsync(input.Id);
        if (product == null) return NotFound();

        product.Name = input.Name;
        product.Category = input.Category;
        product.Price = input.Price;
        product.Stock = input.Stock;
        product.ReorderLevel = input.ReorderLevel;
        product.IsActive = input.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Product updated successfully!";
        return RedirectToAction(nameof(Index));
    }
}