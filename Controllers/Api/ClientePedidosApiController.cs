using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;

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
                    p.TelefonoCliente,
                    p.EmailCliente,
                    CuponGenerado = (p.CuponGenerado != null && p.Estado == "Entregado") ? new {
                        p.CuponGenerado.Codigo,
                        p.CuponGenerado.Valor,
                        p.CuponGenerado.TipoDescuento,
                        p.CuponGenerado.FechaExpiracion
                    } : null,
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

        // POST: api/ClientePedidosApi/{id}/cancelar
        [HttpPost("{id}/cancelar")]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { mensaje = "No estás autenticado." });
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound(new { mensaje = "El pedido no existe." });
            }

            if (pedido.UsuarioId.ToString() != userIdClaim)
            {
                return StatusCode(403, new { mensaje = "No estás autorizado para cancelar este pedido." });
            }

            if (pedido.Estado != "Pendiente")
            {
                return BadRequest(new { mensaje = $"No se puede cancelar el pedido porque ya se encuentra en estado: {pedido.Estado}." });
            }

            string estadoAnterior = pedido.Estado;
            pedido.Estado = "Cancelado";

            // Devolver stock físico
            foreach (var detalle in pedido.Detalles)
            {
                if (detalle.Producto != null)
                {
                    detalle.Producto.Stock += detalle.Cantidad;
                    _context.Productos.Update(detalle.Producto);
                }
            }

            var historial = new HistorialEstado
            {
                PedidoId = pedido.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = "Cancelado",
                FechaCambio = DateTime.UtcNow,
                Notas = "Pedido cancelado por el cliente desde su panel.",
                UsuarioCambioId = pedido.UsuarioId
            };
            _context.HistorialEstados.Add(historial);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Tu pedido ha sido cancelado con éxito y se ha restablecido el stock." });
        }

        // POST: api/ClientePedidosApi/{id}/editar
        [HttpPost("{id}/editar")]
        public async Task<IActionResult> EditarPedido(int id, [FromBody] EditarPedidoRequest request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { mensaje = "No estás autenticado." });
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound(new { mensaje = "El pedido no existe." });
            }

            if (pedido.UsuarioId.ToString() != userIdClaim)
            {
                return StatusCode(403, new { mensaje = "No estás autorizado para modificar este pedido." });
            }

            if (pedido.Estado != "Pendiente")
            {
                return BadRequest(new { mensaje = $"No se puede modificar el pedido porque ya se encuentra en estado: {pedido.Estado}." });
            }

            // Modificar datos de envío y contacto
            if (!string.IsNullOrEmpty(request.NombreCliente))
            {
                pedido.NombreCliente = request.NombreCliente;
            }
            if (!string.IsNullOrEmpty(request.TelefonoCliente))
            {
                pedido.TelefonoCliente = request.TelefonoCliente;
            }
            if (!string.IsNullOrEmpty(request.DireccionEnvio))
            {
                pedido.DireccionEnvio = request.DireccionEnvio;
            }

            // Modificar productos y cantidades
            if (request.Detalles != null && request.Detalles.Any())
            {
                // Devolver todo el stock anterior temporalmente
                foreach (var oldDetalle in pedido.Detalles)
                {
                    if (oldDetalle.Producto != null)
                    {
                        oldDetalle.Producto.Stock += oldDetalle.Cantidad;
                    }
                }

                // Validar stock disponible
                foreach (var item in request.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto == null)
                    {
                        return BadRequest(new { mensaje = $"El producto con ID {item.ProductoId} no existe." });
                    }
                    if (producto.Stock < item.Cantidad)
                    {
                        return BadRequest(new { mensaje = $"No hay suficiente stock disponible para el producto '{producto.Nombre}'. Stock disponible: {producto.Stock}." });
                    }
                }

                // Eliminar detalles antiguos
                _context.DetallesPedidos.RemoveRange(pedido.Detalles);

                // Crear los nuevos detalles y aplicar reducción de stock
                decimal nuevoSubtotal = 0;
                var nuevosDetalles = new List<DetallePedido>();

                var fechaHoy = DateTime.UtcNow;
                var promocionesActivas = await _context.Promociones
                    .Where(p => p.Activo && p.FechaInicio <= fechaHoy && p.FechaFin >= fechaHoy)
                    .ToListAsync();

                foreach (var item in request.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto != null)
                    {
                        // Reducir stock definitivo
                        producto.Stock -= item.Cantidad;
                        _context.Productos.Update(producto);

                        // Calcular precio con promoción
                        decimal precioBase = producto.Precio;
                        decimal precioFinal = precioBase;
                        var promo = promocionesActivas.FirstOrDefault(p => p.ProductoId == producto.Id) 
                                    ?? promocionesActivas.FirstOrDefault(p => p.ProductoId == null);
                        
                        if (promo != null)
                        {
                            precioFinal = precioBase - (precioBase * (promo.DescuentoPorcentaje / 100m));
                        }

                        var nuevoDetalle = new DetallePedido
                        {
                            PedidoId = pedido.Id,
                            ProductoId = producto.Id,
                            Cantidad = item.Cantidad,
                            PrecioUnitario = precioFinal
                        };
                        nuevosDetalles.Add(nuevoDetalle);
                        nuevoSubtotal += item.Cantidad * precioFinal;
                    }
                }

                _context.DetallesPedidos.AddRange(nuevosDetalles);

                // Calcular descuento si hay cupón asociado
                decimal descuento = 0;
                if (pedido.CuponId.HasValue)
                {
                    var cupon = await _context.Cupones.FindAsync(pedido.CuponId.Value);
                    if (cupon != null)
                    {
                        if (cupon.TipoDescuento == "Porcentaje")
                        {
                            descuento = nuevoSubtotal * (cupon.Valor / 100m);
                        }
                        else
                        {
                            descuento = cupon.Valor;
                        }
                    }
                }

                // Actualizar total
                pedido.Total = Math.Max(0, nuevoSubtotal - descuento) + pedido.CostoEnvio;
            }

            var historial = new HistorialEstado
            {
                PedidoId = pedido.Id,
                EstadoAnterior = pedido.Estado,
                EstadoNuevo = pedido.Estado, 
                FechaCambio = DateTime.UtcNow,
                Notas = "Pedido modificado por el cliente (datos de envío y/o productos).",
                UsuarioCambioId = pedido.UsuarioId
            };
            _context.HistorialEstados.Add(historial);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "El pedido ha sido modificado correctamente." });
        }
    }

    public class EditarPedidoRequest
    {
        public string NombreCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string DireccionEnvio { get; set; } = string.Empty;
        public List<DetalleEdicionRequest> Detalles { get; set; } = new List<DetalleEdicionRequest>();
    }

    public class DetalleEdicionRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}
