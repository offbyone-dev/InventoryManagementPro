using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementPro.Controllers;

public class OrdersController : Controller
{
    public IActionResult Index() => View();
}
