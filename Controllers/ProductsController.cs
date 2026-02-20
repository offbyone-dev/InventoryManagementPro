using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementPro.Controllers;

public class ProductsController : Controller
{
    public IActionResult Index() => View();
}
