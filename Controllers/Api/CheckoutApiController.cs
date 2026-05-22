using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;
using Microsoft.Extensions.Configuration;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class CheckoutApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CheckoutApiController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/CheckoutApi/validar-cupon?codigo=BIENVENIDA10
        [HttpGet("validar-cupon")]
        public async Task<IActionResult> ValidarCupon([FromQuery] string codigo)
        {
            if (string.IsNullOrEmpty(codigo))
            {
                return BadRequest(new { mensaje = "El código de cupón es requerido." });
            }

            var cupon = await _context.Cupones
                .FirstOrDefaultAsync(c => c.Codigo.ToUpper() == codigo.ToUpper() && c.Activo);

            if (cupon == null)
            {
                return NotFound(new { mensaje = "El cupón ingresado no es válido o no existe." });
            }

            if (cupon.FechaExpiracion < DateTime.UtcNow)
            {
                return BadRequest(new { mensaje = "El cupón ha expirado." });
            }

            if (cupon.UsosActuales >= cupon.LimiteUso)
            {
                return BadRequest(new { mensaje = "El cupón ha alcanzado su límite de usos." });
            }

            return Ok(new
            {
                id = cupon.Id,
                codigo = cupon.Codigo,
                tipoDescuento = cupon.TipoDescuento,
                valor = cupon.Valor
            });
        }

        // POST: api/CheckoutApi
        [HttpPost]
        public async Task<IActionResult> ProcesarPedido([FromBody] CheckoutRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Obtener los ítems del carrito
            var query = _context.CarritoTemporales.AsQueryable();
            int? sesionInvitadoId = null;

            if (request.UsuarioId.HasValue && request.UsuarioId > 0)
            {
                query = query.Where(c => c.UsuarioId == request.UsuarioId);
            }
            else if (!string.IsNullOrEmpty(request.Token))
            {
                var sesion = await _context.SesionesInvitados.FirstOrDefaultAsync(s => s.TokenSesion == request.Token);
                if (sesion == null) return BadRequest(new { mensaje = "Sesión de invitado inválida o expirada." });
                sesionInvitadoId = sesion.Id;
                query = query.Where(c => c.SesionInvitadoId == sesion.Id);
            }
            else
            {
                return BadRequest(new { mensaje = "No se pudo identificar el carrito de origen." });
            }

            var itemsCarrito = await query
                .Include(c => c.Producto)
                .ToListAsync();

            if (!itemsCarrito.Any())
            {
                return BadRequest(new { mensaje = "El carrito de compras está vacío." });
            }

            // 2. Verificar disponibilidad de stock para cada artículo
            foreach (var item in itemsCarrito)
            {
                if (item.Producto == null || !item.Producto.Activo)
                {
                    return BadRequest(new { mensaje = $"El producto '{item.Producto?.Nombre ?? "Desconocido"}' ya no está disponible." });
                }
                if (item.Producto.Stock < item.Cantidad)
                {
                    return BadRequest(new { mensaje = $"Stock insuficiente para '{item.Producto.Nombre}'. Disponible: {item.Producto.Stock} unidades." });
                }
            }

            // 3. Obtener costo de envío base de la tienda
            var configTienda = await _context.ConfiguracionTienda.FirstOrDefaultAsync();
            decimal costoEnvio = configTienda != null ? configTienda.CostoEnvioBase : 15.00m;

            // 4. Calcular Subtotal y Descuentos
            decimal subtotal = itemsCarrito.Sum(item => (item.Producto?.Precio ?? 0) * item.Cantidad);
            decimal descuento = 0;
            Cupon? cuponAplicado = null;

            if (!string.IsNullOrEmpty(request.CodigoCupon))
            {
                var cupon = await _context.Cupones
                    .FirstOrDefaultAsync(c => c.Codigo.ToUpper() == request.CodigoCupon.ToUpper() && c.Activo && c.FechaExpiracion >= DateTime.UtcNow && c.UsosActuales < c.LimiteUso);
                
                if (cupon != null)
                {
                    cuponAplicado = cupon;
                    if (cupon.TipoDescuento == "Porcentaje")
                    {
                        descuento = subtotal * (cupon.Valor / 100m);
                    }
                    else // Fijo
                    {
                        descuento = cupon.Valor;
                    }
                    // Incrementar usos del cupón
                    cupon.UsosActuales++;
                    _context.Cupones.Update(cupon);
                }
            }

            decimal total = Math.Max(0, subtotal - descuento) + costoEnvio;

            // 5. Crear el Pedido
            var nuevoPedido = new Pedido
            {
                UsuarioId = request.UsuarioId,
                FechaPedido = DateTime.UtcNow,
                Total = total,
                Estado = "Pendiente",
                CuponId = cuponAplicado?.Id,
                MetodoPago = request.MetodoPago,
                CostoEnvio = costoEnvio,
                DireccionEnvio = request.DireccionEnvio,
                NombreCliente = request.NombreCliente,
                TelefonoCliente = request.TelefonoCliente,
                EmailCliente = request.EmailCliente
            };

            _context.Pedidos.Add(nuevoPedido);
            await _context.SaveChangesAsync(); // Generar ID del Pedido

            // 6. Crear Detalle del Pedido y reducir Stock físico
            foreach (var item in itemsCarrito)
            {
                var detalle = new DetallePedido
                {
                    PedidoId = nuevoPedido.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Producto!.Precio
                };
                _context.DetallesPedidos.Add(detalle);

                // Reducir stock del inventario
                item.Producto.Stock -= item.Cantidad;
                _context.Productos.Update(item.Producto);
            }

            // 7. Crear el historial de estados inicial
            var historial = new HistorialEstado
            {
                PedidoId = nuevoPedido.Id,
                EstadoAnterior = "Ninguno",
                EstadoNuevo = "Pendiente",
                FechaCambio = DateTime.UtcNow,
                Notas = "Pedido registrado mediante la aplicación web.",
                UsuarioCambioId = null // Cliente o Invitado
            };
            _context.HistorialEstados.Add(historial);

            // 8. Vaciar Carrito Temporal de la BD
            _context.CarritoTemporales.RemoveRange(itemsCarrito);

            // Si es invitado, limpiar sesión temporal
            if (sesionInvitadoId.HasValue)
            {
                var sesionObj = await _context.SesionesInvitados.FindAsync(sesionInvitadoId.Value);
                if (sesionObj != null)
                {
                    _context.SesionesInvitados.Remove(sesionObj);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "¡Pedido procesado con éxito!",
                pedidoId = nuevoPedido.Id,
                total = nuevoPedido.Total
            });
        }
    }

    // DTO para recibir la petición de Checkout
    public class CheckoutRequest
    {
        public string? Token { get; set; }
        public int? UsuarioId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string EmailCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string DireccionEnvio { get; set; } = string.Empty;
        public string MetodoPago { get; set; } = "Efectivo";
        public string? CodigoCupon { get; set; }
    }
}
