using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class ReportesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Ventas")]
        public async Task<IActionResult> GetVentas([FromQuery] string periodo)
        {
            if (string.IsNullOrEmpty(periodo))
            {
                return BadRequest(new { mensaje = "El periodo es requerido (dia, mes, anio)" });
            }

            var ahora = DateTime.Now;
            DateTime fechaInicio;
            string tituloReporte;

            switch (periodo.ToLower())
            {
                case "dia":
                    fechaInicio = ahora.Date;
                    tituloReporte = $"Reporte de Ventas del Día ({ahora:dd/MM/yyyy})";
                    break;
                case "mes":
                    fechaInicio = new DateTime(ahora.Year, ahora.Month, 1);
                    tituloReporte = $"Reporte de Ventas del Mes ({ahora:MMMM yyyy})";
                    break;
                case "anio":
                    fechaInicio = new DateTime(ahora.Year, 1, 1);
                    tituloReporte = $"Reporte de Ventas del Año ({ahora.Year})";
                    break;
                default:
                    return BadRequest(new { mensaje = "Periodo inválido. Use dia, mes o anio." });
            }

            // Consultar pedidos completados o entregados dentro del periodo
            var pedidos = await _context.Pedidos
                .Where(p => p.FechaPedido >= fechaInicio && p.FechaPedido <= ahora)
                .OrderBy(p => p.FechaPedido)
                .Select(p => new
                {
                    p.Id,
                    p.FechaPedido,
                    p.NombreCliente,
                    p.Total,
                    p.MetodoPago,
                    p.Estado
                })
                .ToListAsync();

            var totalIngresos = pedidos.Sum(p => p.Total);
            var totalPedidos = pedidos.Count;

            return Ok(new
            {
                titulo = tituloReporte,
                fechaGeneracion = ahora,
                totalIngresos = totalIngresos,
                totalPedidos = totalPedidos,
                ventas = pedidos
            });
        }
    }
}
