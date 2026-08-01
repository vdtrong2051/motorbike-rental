using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.ViewModels.Vehicle;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

public class HomeController : Controller
{
    private readonly IVehicleService _vehicleService;

    public HomeController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    public async Task<IActionResult> Index()
    {
        var xeNoiBat = await _vehicleService.SearchAsync(new VehicleSearchViewModel
        {
            ChiXeSanSang = true,
            Page = 1,
            PageSize = 6
        });
        return View(xeNoiBat.Items);
    }

    public IActionResult Error() => View();
}