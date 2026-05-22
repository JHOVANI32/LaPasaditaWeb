// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminProductosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminProductosApiController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/AdminProductosApi
        [HttpGet]
        public async Task<IActionResult> GetProductos()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Descripcion,
                    p.Precio,
                    p.Stock,
                    p.ImagenUrl,
                    p.CategoriaId,
                    NombreCategoria = p.Categoria != null ? p.Categoria.Nombre : "Sin Categoría",
                    p.Activo
                })
                .ToListAsync();

            var categorias = await _context.Categorias
                .Where(c => c.Activo)
                .Select(c => new { c.Id, c.Nombre })
                .ToListAsync();

            return Ok(new { productos, categorias });
        }

        // GET: api/AdminProductosApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado." });
            }

            return Ok(new
            {
                producto.Id,
                producto.Nombre,
                producto.Descripcion,
                producto.Precio,
                producto.Stock,
                producto.ImagenUrl,
                producto.CategoriaId,
                producto.Activo
            });
        }

        // POST: api/AdminProductosApi
        [HttpPost]
        public async Task<IActionResult> CrearProducto([FromBody] ProductoSaveRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var producto = new Producto
            {
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                Precio = request.Precio,
                Stock = request.Stock,
                ImagenUrl = string.IsNullOrEmpty(request.ImagenUrl) ? "/images/productos/default-grocery.png" : request.ImagenUrl,
                CategoriaId = request.CategoriaId,
                Activo = request.Activo
            };

            _context.Productos.Add(producto);

            // Registrar historial de precios inicial
            var historial = new HistorialPrecio
            {
                Producto = producto,
                PrecioAnterior = 0,
                PrecioNuevo = request.Precio,
                FechaCambio = DateTime.UtcNow
            };
            _context.HistorialPrecios.Add(historial);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Producto creado con éxito.", productoId = producto.Id });
        }

        // PUT: api/AdminProductosApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarProducto(int id, [FromBody] ProductoSaveRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado." });
            }

            decimal precioAnterior = producto.Precio;

            // Actualizar campos
            producto.Nombre = request.Nombre;
            producto.Descripcion = request.Descripcion;
            producto.Stock = request.Stock;
            producto.CategoriaId = request.CategoriaId;
            producto.Activo = request.Activo;
            
            if (!string.IsNullOrEmpty(request.ImagenUrl))
            {
                producto.ImagenUrl = request.ImagenUrl;
            }

            // Registrar en historial si el precio cambió
            if (precioAnterior != request.Precio)
            {
                producto.Precio = request.Precio;
                var historial = new HistorialPrecio
                {
                    ProductoId = id,
                    PrecioAnterior = precioAnterior,
                    PrecioNuevo = request.Precio,
                    FechaCambio = DateTime.UtcNow
                };
                _context.HistorialPrecios.Add(historial);
            }

            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Producto actualizado con éxito." });
        }

        // DELETE: api/AdminProductosApi/5 (Borrado Lógico)
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado." });
            }

            // Realizamos un Borrado Lógico para no romper pedidos históricos
            producto.Activo = false;
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Producto desactivado correctamente del catálogo." });
        }

        // POST: api/AdminProductosApi/upload
        [HttpPost("upload")]
        public async Task<IActionResult> SubirImagen([FromForm] IFormFile imagenFile)
        {
            if (imagenFile == null || imagenFile.Length == 0)
            {
                return BadRequest(new { mensaje = "No se ha proporcionado un archivo válido." });
            }

            // Validar extensión
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = System.IO.Path.GetExtension(imagenFile.FileName).ToLower();
            if (!extensionesPermitidas.Contains(extension))
            {
                return BadRequest(new { mensaje = "Formato de imagen no permitido. Solo se aceptan JPG, PNG, GIF y WEBP." });
            }

            // Crear carpeta si no existe
            var carpetaDestino = Path.Combine(_env.WebRootPath, "images", "productos");
            if (!Directory.Exists(carpetaDestino))
            {
                Directory.CreateDirectory(carpetaDestino);
            }

            // Generar nombre de archivo único
            var nombreUnico = $"{Guid.NewGuid()}{extension}";
            var rutaFisica = Path.Combine(carpetaDestino, nombreUnico);

            // Guardar archivo
            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await imagenFile.CopyToAsync(stream);
            }

            var rutaRelativa = $"/images/productos/{nombreUnico}";
            return Ok(new { rutaImagen = rutaRelativa });
        }
    }

    // DTO para recibir peticiones JSON de creación y edición
    public class ProductoSaveRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string? ImagenUrl { get; set; }
        public int CategoriaId { get; set; }
        public bool Activo { get; set; } = true;
    }
}
