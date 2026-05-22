using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace LaPasaditaWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var hoy = DateTime.UtcNow.Date;

            // Pedidos creados hoy
            var pedidosHoy = await _context.Pedidos
                .Where(p => p.FechaPedido.Date == hoy)
                .CountAsync();

            // Suma del total de pedidos de hoy (excluyendo cancelados)
            var ventasDia = await _context.Pedidos
                .Where(p => p.FechaPedido.Date == hoy && p.Estado != "Cancelado")
                .SumAsync(p => (decimal?)p.Total) ?? 0m;

            // Productos activos en catálogo
            var catalogoActivo = await _context.Productos
                .Where(p => p.Activo)
                .CountAsync();

            // Usuarios registrados (clientes y admins)
            var usuariosActivos = await _context.Usuarios
                .Where(u => u.Activo)
                .CountAsync();

            // Últimos 3 pedidos para actividad reciente
            var pedidosRecientes = await _context.Pedidos
                .Include(p => p.Usuario)
                .OrderByDescending(p => p.FechaPedido)
                .Take(3)
                .ToListAsync();

            ViewBag.PedidosHoy = pedidosHoy;
            ViewBag.VentasDia = ventasDia;
            ViewBag.CatalogoActivo = catalogoActivo;
            ViewBag.UsuariosActivos = usuariosActivos;
            ViewBag.PedidosRecientes = pedidosRecientes;

            return View();
        }
    }
}
