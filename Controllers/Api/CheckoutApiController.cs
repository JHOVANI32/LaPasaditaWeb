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

            // 4. Calcular Subtotal, aplicando promociones activas
            var ahora = DateTime.Now;
            var promocionesActivas = await _context.Promociones
                .Where(p => p.Activo && p.FechaInicio <= ahora && p.FechaFin >= ahora)
                .ToListAsync();

            decimal subtotal = 0;
            foreach(var item in itemsCarrito)
            {
                if (item.Producto != null)
                {
                    decimal precioBase = item.Producto.Precio;
                    decimal precioFinal = precioBase;
                    var promo = promocionesActivas.FirstOrDefault(p => p.ProductoId == item.ProductoId) 
                                ?? promocionesActivas.FirstOrDefault(p => p.ProductoId == null);
                    
                    if (promo != null)
                    {
                        precioFinal = precioBase - (precioBase * (promo.DescuentoPorcentaje / 100m));
                    }
                    subtotal += precioFinal * item.Cantidad;
                }
            }
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
                decimal precioBase = item.Producto!.Precio;
                decimal precioFinal = precioBase;
                var promo = promocionesActivas.FirstOrDefault(p => p.ProductoId == item.ProductoId) 
                            ?? promocionesActivas.FirstOrDefault(p => p.ProductoId == null);
                
                if (promo != null)
                {
                    precioFinal = precioBase - (precioBase * (promo.DescuentoPorcentaje / 100m));
                }

                var detalle = new DetallePedido
                {
                    PedidoId = nuevoPedido.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precioFinal
                };
                _context.DetallesPedidos.Add(detalle);

                // Reducir stock del inventario
                item.Producto.Stock -= item.Cantidad;
                _context.Productos.Update(item.Producto);
            }

            // 6.5 Evaluar Campañas de Cupones (Premios)
            var campanasActivas = await _context.CampanasCupones
                .Include(c => c.PedidosPremiados)
                .Where(c => c.Activo && c.MontoMinimo <= total)
                .ToListAsync();

            var campanaGanadora = campanasActivas
                .Where(c => (c.LimiteDiario == 0 || c.CuponesGeneradosHoy < c.LimiteDiario) &&
                            (c.LimiteEarlyBird == 0 || c.PedidosPremiados.Count < c.LimiteEarlyBird))
                .OrderByDescending(c => c.MontoMinimo)
                .FirstOrDefault();

            if (campanaGanadora != null)
            {
                decimal descuentoGenerado = 0;
                string tipoDescGenerado = "Fijo";

                if (campanaGanadora.TipoRecompensa == "Fijo")
                {
                    descuentoGenerado = campanaGanadora.ValorRecompensaFija ?? 0;
                }
                else // Sorpresa
                {
                    if (!string.IsNullOrEmpty(campanaGanadora.ValoresSorpresa))
                    {
                        var valores = campanaGanadora.ValoresSorpresa.Split(',')
                            .Select(v => decimal.TryParse(v.Trim(), out var val) ? val : 0)
                            .Where(v => v > 0)
                            .ToList();
                        
                        if (valores.Any())
                        {
                            var rnd = new Random();
                            descuentoGenerado = valores[rnd.Next(valores.Count)];
                            tipoDescGenerado = "Porcentaje"; // Asumimos que los sorpresa son porcentaje
                        }
                    }
                }

                if (descuentoGenerado > 0)
                {
                    // Generar código único
                    string codigoUnico = $"GANADOR-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";

                    var nuevoCupon = new Cupon
                    {
                        Codigo = codigoUnico,
                        TipoDescuento = tipoDescGenerado,
                        Valor = descuentoGenerado,
                        FechaExpiracion = DateTime.UtcNow.AddDays(30), // Válido por 30 días
                        LimiteUso = 1,
                        UsosActuales = 0,
                        Activo = true
                    };

                    _context.Cupones.Add(nuevoCupon);
                    await _context.SaveChangesAsync(); // Para obtener el ID del cupón

                    nuevoPedido.CuponGeneradoId = nuevoCupon.Id;
                    nuevoPedido.CampanaCuponId = campanaGanadora.Id;
                    
                    campanaGanadora.CuponesGeneradosHoy++;
                    _context.Pedidos.Update(nuevoPedido);
                    _context.CampanasCupones.Update(campanaGanadora);
                }
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
