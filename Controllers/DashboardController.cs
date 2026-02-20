using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementPro.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index() => View();
}
