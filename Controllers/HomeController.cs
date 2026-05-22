using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;

namespace LaPasaditaWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var config = await _context.ConfiguracionTienda.FirstOrDefaultAsync();
        if (config != null)
        {
            ViewBag.NombreTienda = config.NombreTienda;
            ViewBag.CostoEnvioBase = config.CostoEnvioBase;
        }
        else
        {
            ViewBag.NombreTienda = "Abarrotes La Pasadita";
            ViewBag.CostoEnvioBase = 15.00m;
        }

        // Cargar promociones activas
        var ahora = DateTime.Now;
        var promocionesActivas = await _context.Promociones
            .Where(p => p.Activo && p.FechaInicio <= ahora && p.FechaFin >= ahora)
            .ToListAsync();
        ViewBag.PromocionesActivas = promocionesActivas;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Checkout()
    {
        var config = await _context.ConfiguracionTienda.FirstOrDefaultAsync();
        ViewBag.CostoEnvioBase = config != null ? config.CostoEnvioBase : 15.00m;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
