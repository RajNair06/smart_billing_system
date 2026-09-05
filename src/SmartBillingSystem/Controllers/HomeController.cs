using Microsoft.AspNetCore.Mvc;
using SmartBillingSystem.Models.ViewModels;

namespace SmartBillingSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
