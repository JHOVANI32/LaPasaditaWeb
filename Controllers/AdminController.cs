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

            // Productos agotados (Stock = 0)
            var productosAgotados = await _context.Productos
                .Where(p => p.Activo && p.Stock == 0)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            // Productos con stock bajo (1 a 10)
            var productosStockBajo = await _context.Productos
                .Where(p => p.Activo && p.Stock > 0 && p.Stock <= 10)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            // Pedidos pendientes
            var pedidosPendientes = await _context.Pedidos
                .Where(p => p.Estado == "Pendiente" || p.Estado == "Preparando")
                .OrderBy(p => p.FechaPedido)
                .ToListAsync();

            // Clientes registrados hoy
            var clientesNuevosHoy = await _context.Usuarios
                .Where(u => u.Rol == "Cliente" && u.FechaRegistro.Date == hoy)
                .ToListAsync();

            // Meta de ventas (ejemplo: 1000 pesos)
            decimal metaVentasDia = 1000m;
            bool metaAlcanzada = ventasDia >= metaVentasDia;

            // Datos para Gráfica de Ventas (Últimos 7 días)
            var hace7Dias = hoy.AddDays(-6);
            var ventasUltimos7Dias = await _context.Pedidos
                .Where(p => p.Estado != "Cancelado" && p.FechaPedido.Date >= hace7Dias)
                .GroupBy(p => p.FechaPedido.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Total) })
                .ToListAsync();

            var labelsDias = new string[7];
            var totalesVentas = new decimal[7];
            for(int i = 0; i < 7; i++) {
                var dia = hace7Dias.AddDays(i);
                labelsDias[i] = dia.ToString("dd MMM");
                var ventaDia = ventasUltimos7Dias.FirstOrDefault(v => v.Fecha.Date == dia.Date);
                totalesVentas[i] = ventaDia != null ? (decimal)ventaDia.Total : 0m;
            }

            ViewBag.PedidosHoy = pedidosHoy;
            ViewBag.VentasDia = ventasDia;
            ViewBag.CatalogoActivo = catalogoActivo;
            ViewBag.UsuariosActivos = usuariosActivos;
            ViewBag.PedidosRecientes = pedidosRecientes;
            ViewBag.ProductosStockBajo = productosStockBajo;
            ViewBag.ProductosAgotados = productosAgotados;
            ViewBag.PedidosPendientes = pedidosPendientes;
            ViewBag.ClientesNuevosHoy = clientesNuevosHoy;
            ViewBag.MetaVentasDia = metaVentasDia;
            ViewBag.MetaAlcanzada = metaAlcanzada;
            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labelsDias);
            ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(totalesVentas);

            return View();
        }
    }
}
