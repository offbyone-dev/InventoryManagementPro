using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
namespace InventoryManagementPro.Models;

public class Order
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string OrderNo { get; set; } = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";

    public DateTime OrderDateUtc { get; set; } = DateTime.UtcNow;
    public string? CustomerName { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}