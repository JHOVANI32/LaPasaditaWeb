using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarritoApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CarritoApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Obtener o Generar un Token de Invitado
        // POST: api/CarritoApi/token
        [HttpPost("token")]
        public async Task<IActionResult> GenerarToken()
        {
            var token = Guid.NewGuid().ToString();
            var sesion = new SesionInvitado
            {
                TokenSesion = token,
                FechaCreacion = DateTime.UtcNow,
                FechaUltimaActividad = DateTime.UtcNow
            };

            _context.SesionesInvitados.Add(sesion);
            await _context.SaveChangesAsync();

            return Ok(new { token });
        }

        // 2. Obtener el Contenido del Carrito (Invitado o Usuario Logueado)
        // GET: api/CarritoApi?token=xyz&usuarioId=123
        [HttpGet]
        public async Task<IActionResult> GetCarrito([FromQuery] string? token, [FromQuery] int? usuarioId)
        {
            if (string.IsNullOrEmpty(token) && !usuarioId.HasValue)
            {
                return BadRequest(new { mensaje = "Se requiere un token de invitado o un ID de usuario." });
            }

            var query = _context.CarritoTemporales.AsQueryable();

            if (usuarioId.HasValue && usuarioId > 0)
            {
                // Prioridad usuario logueado
                query = query.Where(c => c.UsuarioId == usuarioId);
            }
            else
            {
                // Carrito silencioso de invitado
                var sesion = await _context.SesionesInvitados.FirstOrDefaultAsync(s => s.TokenSesion == token);
                if (sesion == null)
                {
                    return Ok(new List<object>()); // Retorna vacío si la sesión no existe en BD
                }

                // Actualizar última actividad del invitado
                sesion.FechaUltimaActividad = DateTime.UtcNow;
                _context.SesionesInvitados.Update(sesion);
                await _context.SaveChangesAsync();

                query = query.Where(c => c.SesionInvitadoId == sesion.Id);
            }

            var items = await query
                .Include(c => c.Producto)
                .Select(c => new
                {
                    c.Id,
                    c.ProductoId,
                    NombreProducto = c.Producto != null ? c.Producto.Nombre : "",
                    ImagenUrl = c.Producto != null ? c.Producto.ImagenUrl : "",
                    PrecioUnitario = c.Producto != null ? c.Producto.Precio : 0,
                    StockDisponible = c.Producto != null ? c.Producto.Stock : 0,
                    c.Cantidad,
                    Subtotal = c.Producto != null ? c.Producto.Precio * c.Cantidad : 0
                })
                .ToListAsync();

            return Ok(items);
        }

        // 3. Agregar Producto al Carrito
        // POST: api/CarritoApi/agregar
        [HttpPost("agregar")]
        public async Task<IActionResult> AgregarAlCarrito([FromBody] CarritoRequest request)
        {
            if (request.ProductoId <= 0 || request.Cantidad <= 0)
            {
                return BadRequest(new { mensaje = "Datos de producto o cantidad inválidos." });
            }

            var producto = await _context.Productos.FindAsync(request.ProductoId);
            if (producto == null || !producto.Activo || producto.Stock < request.Cantidad)
            {
                return BadRequest(new { mensaje = "El producto no está disponible o no hay suficiente stock." });
            }

            CarritoTemporal? itemExistente = null;
            int? sesionInvitadoId = null;

            if (request.UsuarioId.HasValue && request.UsuarioId > 0)
            {
                // Usuario autenticado
                var usuario = await _context.Usuarios.FindAsync(request.UsuarioId.Value);
                if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });

                itemExistente = await _context.CarritoTemporales
                    .FirstOrDefaultAsync(c => c.UsuarioId == request.UsuarioId && c.ProductoId == request.ProductoId);
            }
            else if (!string.IsNullOrEmpty(request.Token))
            {
                // Invitado
                var sesion = await _context.SesionesInvitados.FirstOrDefaultAsync(s => s.TokenSesion == request.Token);
                if (sesion == null)
                {
                    // Crear sesión al vuelo si no se encuentra
                    sesion = new SesionInvitado
                    {
                        TokenSesion = request.Token,
                        FechaCreacion = DateTime.UtcNow,
                        FechaUltimaActividad = DateTime.UtcNow
                    };
                    _context.SesionesInvitados.Add(sesion);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    sesion.FechaUltimaActividad = DateTime.UtcNow;
                    _context.SesionesInvitados.Update(sesion);
                }

                sesionInvitadoId = sesion.Id;
                itemExistente = await _context.CarritoTemporales
                    .FirstOrDefaultAsync(c => c.SesionInvitadoId == sesion.Id && c.ProductoId == request.ProductoId);
            }
            else
            {
                return BadRequest(new { mensaje = "Se requiere identificación del carrito (Token o UsuarioId)." });
            }

            if (itemExistente != null)
            {
                // Verificar que la suma no exceda el stock
                if (itemExistente.Cantidad + request.Cantidad > producto.Stock)
                {
                    return BadRequest(new { mensaje = $"No puedes agregar más productos. El stock máximo disponible es {producto.Stock}." });
                }
                itemExistente.Cantidad += request.Cantidad;
                _context.CarritoTemporales.Update(itemExistente);
            }
            else
            {
                var nuevoItem = new CarritoTemporal
                {
                    ProductoId = request.ProductoId,
                    Cantidad = request.Cantidad,
                    UsuarioId = request.UsuarioId,
                    SesionInvitadoId = sesionInvitadoId,
                    FechaAgregado = DateTime.UtcNow
                };
                _context.CarritoTemporales.Add(nuevoItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Producto agregado correctamente al carrito." });
        }

        // 4. Actualizar Cantidad del Producto
        // POST: api/CarritoApi/actualizar
        [HttpPost("actualizar")]
        public async Task<IActionResult> ActualizarCantidad([FromBody] CarritoRequest request)
        {
            var query = _context.CarritoTemporales.AsQueryable();

            if (request.UsuarioId.HasValue && request.UsuarioId > 0)
            {
                query = query.Where(c => c.UsuarioId == request.UsuarioId && c.ProductoId == request.ProductoId);
            }
            else if (!string.IsNullOrEmpty(request.Token))
            {
                var sesion = await _context.SesionesInvitados.FirstOrDefaultAsync(s => s.TokenSesion == request.Token);
                if (sesion == null) return NotFound(new { mensaje = "Sesión de invitado no encontrada." });
                query = query.Where(c => c.SesionInvitadoId == sesion.Id && c.ProductoId == request.ProductoId);
            }
            else
            {
                return BadRequest(new { mensaje = "Identificación inválida." });
            }

            var item = await query.FirstOrDefaultAsync();
            if (item == null)
            {
                return NotFound(new { mensaje = "El producto no está en el carrito." });
            }

            var producto = await _context.Productos.FindAsync(request.ProductoId);
            if (producto == null || producto.Stock < request.Cantidad)
            {
                return BadRequest(new { mensaje = "Stock insuficiente." });
            }

            item.Cantidad = request.Cantidad;
            _context.CarritoTemporales.Update(item);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Cantidad actualizada correctamente." });
        }

        // 5. Eliminar un Producto del Carrito
        // POST: api/CarritoApi/eliminar
        [HttpPost("eliminar")]
        public async Task<IActionResult> EliminarDelCarrito([FromBody] CarritoRequest request)
        {
            var query = _context.CarritoTemporales.AsQueryable();

            if (request.UsuarioId.HasValue && request.UsuarioId > 0)
            {
                query = query.Where(c => c.UsuarioId == request.UsuarioId && c.ProductoId == request.ProductoId);
            }
            else if (!string.IsNullOrEmpty(request.Token))
            {
                var sesion = await _context.SesionesInvitados.FirstOrDefaultAsync(s => s.TokenSesion == request.Token);
                if (sesion == null) return NotFound(new { mensaje = "Sesión no encontrada." });
                query = query.Where(c => c.SesionInvitadoId == sesion.Id && c.ProductoId == request.ProductoId);
            }
            else
            {
                return BadRequest(new { mensaje = "Identificación inválida." });
            }

            var item = await query.FirstOrDefaultAsync();
            if (item == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado en el carrito." });
            }

            _context.CarritoTemporales.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Producto eliminado del carrito." });
        }

        // 6. Fusionar Carrito de Invitado con el de Usuario (al iniciar sesión)
        // POST: api/CarritoApi/fusionar
        [HttpPost("fusionar")]
        public async Task<IActionResult> FusionarCarritos([FromBody] CarritoFusionRequest request)
        {
            if (string.IsNullOrEmpty(request.Token) || request.UsuarioId <= 0)
            {
                return BadRequest(new { mensaje = "Datos de fusión inválidos." });
            }

            var sesion = await _context.SesionesInvitados.FirstOrDefaultAsync(s => s.TokenSesion == request.Token);
            if (sesion == null)
            {
                return Ok(new { mensaje = "No hay carrito de invitado para fusionar." });
            }

            var itemsInvitado = await _context.CarritoTemporales
                .Where(c => c.SesionInvitadoId == sesion.Id)
                .ToListAsync();

            if (itemsInvitado.Any())
            {
                foreach (var itemInv in itemsInvitado)
                {
                    // Buscar si el usuario ya tiene ese producto en su carrito
                    var itemUsuario = await _context.CarritoTemporales
                        .FirstOrDefaultAsync(c => c.UsuarioId == request.UsuarioId && c.ProductoId == itemInv.ProductoId);

                    if (itemUsuario != null)
                    {
                        // Sumar cantidades (respetando stock del producto)
                        var producto = await _context.Productos.FindAsync(itemInv.ProductoId);
                        if (producto != null)
                        {
                            itemUsuario.Cantidad = Math.Min(itemUsuario.Cantidad + itemInv.Cantidad, producto.Stock);
                            _context.CarritoTemporales.Update(itemUsuario);
                        }
                    }
                    else
                    {
                        // Migrar el ítem al usuario
                        itemInv.UsuarioId = request.UsuarioId;
                        itemInv.SesionInvitadoId = null; // Quitar el enlace de invitado
                        _context.CarritoTemporales.Update(itemInv);
                    }
                }

                // Eliminar la sesión de invitado
                _context.SesionesInvitados.Remove(sesion);
                await _context.SaveChangesAsync();
            }

            return Ok(new { mensaje = "Carritos fusionados correctamente." });
        }
    }

    // DTOs para recibir peticiones JSON
    public class CarritoRequest
    {
        public string? Token { get; set; }
        public int? UsuarioId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

    public class CarritoFusionRequest
    {
        public string Token { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
    }
}
