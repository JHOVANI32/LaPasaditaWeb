using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente")]
    public class ClientePedidosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClientePedidosApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ClientePedidosApi?usuarioId=2
        [HttpGet]
        public async Task<IActionResult> GetPedidos([FromQuery] int usuarioId)
        {
            // Validar que el usuario autenticado sólo pueda ver sus propios pedidos
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || userIdClaim != usuarioId.ToString())
            {
                return StatusCode(403, new { mensaje = "No estás autorizado para ver los pedidos de este usuario." });
            }

            var pedidos = await _context.Pedidos
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.FechaPedido)
                .Select(p => new
                {
                    p.Id,
                    p.FechaPedido,
                    p.Total,
                    p.Estado,
                    p.MetodoPago,
                    p.CostoEnvio,
                    p.DireccionEnvio,
                    p.NombreCliente,
                    Detalles = p.Detalles.Select(d => new
                    {
                        d.Id,
                        d.ProductoId,
                        NombreProducto = d.Producto != null ? d.Producto.Nombre : "Producto no disponible",
                        ImagenUrl = d.Producto != null ? d.Producto.ImagenUrl : "",
                        d.Cantidad,
                        d.PrecioUnitario,
                        Subtotal = d.Cantidad * d.PrecioUnitario
                    }),
                    HistorialEstados = p.HistorialEstados
                        .OrderBy(h => h.FechaCambio)
                        .Select(h => new
                        {
                            h.Id,
                            h.EstadoAnterior,
                            h.EstadoNuevo,
                            h.FechaCambio,
                            h.Notas
                        })
                })
                .ToListAsync();

            return Ok(pedidos);
        }
    }
}
