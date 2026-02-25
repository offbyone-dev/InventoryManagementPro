using InventoryManagementPro.Data;
using InventoryManagementPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementPro.Controllers
{
    public class SuppliersController : Controller
    {
        private readonly AppDbContext _db;
        public SuppliersController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var suppliers = await _db.Suppliers.ToListAsync();
            return View(suppliers);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _db.Suppliers.Add(supplier);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Supplier added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _db.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _db.Suppliers.Update(supplier);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Supplier updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _db.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            _db.Suppliers.Remove(supplier);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Supplier deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}