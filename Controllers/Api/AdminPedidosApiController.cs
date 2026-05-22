using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;
using Microsoft.Extensions.Configuration;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminPedidosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminPedidosApiController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/AdminPedidosApi
        [HttpGet]
        public async Task<IActionResult> GetPedidos()
        {
            var pedidos = await _context.Pedidos
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
                    p.TelefonoCliente,
                    p.EmailCliente,
                    CantArticulos = p.Detalles.Count
                })
                .ToListAsync();

            return Ok(pedidos);
        }

        // GET: api/AdminPedidosApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPedido(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound(new { mensaje = "Pedido no encontrado." });
            }

            var historial = await _context.HistorialEstados
                .Where(h => h.PedidoId == id)
                .OrderByDescending(h => h.FechaCambio)
                .Select(h => new
                {
                    h.Id,
                    h.EstadoAnterior,
                    h.EstadoNuevo,
                    h.FechaCambio,
                    h.Notas
                })
                .ToListAsync();

            return Ok(new
            {
                pedido.Id,
                pedido.FechaPedido,
                pedido.Total,
                pedido.Estado,
                pedido.MetodoPago,
                pedido.CostoEnvio,
                pedido.DireccionEnvio,
                pedido.NombreCliente,
                pedido.TelefonoCliente,
                pedido.EmailCliente,
                Detalles = pedido.Detalles.Select(d => new
                {
                    d.Id,
                    NombreProducto = d.Producto != null ? d.Producto.Nombre : "Producto no disponible",
                    ImagenUrl = d.Producto != null ? d.Producto.ImagenUrl : "",
                    d.Cantidad,
                    d.PrecioUnitario,
                    Subtotal = d.Cantidad * d.PrecioUnitario
                }),
                Historial = historial
            });
        }

        // PUT: api/AdminPedidosApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarEstado(int id, [FromBody] EstadoUpdateRequest request)
        {
            if (string.IsNullOrEmpty(request.EstadoNuevo))
            {
                return BadRequest(new { mensaje = "El nuevo estado es obligatorio." });
            }

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound(new { mensaje = "Pedido no encontrado." });
            }

            string estadoAnterior = pedido.Estado;
            if (estadoAnterior == request.EstadoNuevo)
            {
                return BadRequest(new { mensaje = "El pedido ya se encuentra en ese estado." });
            }

            // Actualizar estado del pedido
            pedido.Estado = request.EstadoNuevo;
            _context.Pedidos.Update(pedido);

            // Registrar en historial
            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? adminId = !string.IsNullOrEmpty(adminIdClaim) ? int.Parse(adminIdClaim) : null;

            var historial = new HistorialEstado
            {
                PedidoId = id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = request.EstadoNuevo,
                FechaCambio = DateTime.UtcNow,
                Notas = string.IsNullOrEmpty(request.Notas) ? $"Estado cambiado de {estadoAnterior} a {request.EstadoNuevo} por el Administrador." : request.Notas,
                UsuarioCambioId = adminId
            };
            _context.HistorialEstados.Add(historial);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Estado del pedido actualizado correctamente.", nuevoEstado = request.EstadoNuevo });
        }
    }

    public class EstadoUpdateRequest
    {
        public string EstadoNuevo { get; set; } = string.Empty;
        public string? Notas { get; set; }
    }
}
