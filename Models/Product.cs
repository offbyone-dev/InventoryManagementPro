using System.ComponentModel.DataAnnotations;
namespace InventoryManagementPro.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(60)]
    public string? Sku { get; set; }
    [StringLength(60)]
    public string? Category { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; } = 5;

    public bool IsActive { get; set; } = true;

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}