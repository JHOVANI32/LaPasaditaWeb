// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
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

        // DELETE: api/AdminProductosApi/5 (Borrado Físico con fallback de Borrado Lógico)
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado." });
            }

            // Verificar si tiene pedidos históricos para no violar la FK restrictiva
            var tienePedidos = await _context.DetallesPedidos.AnyAsync(dp => dp.ProductoId == id);

            if (tienePedidos)
            {
                // Si ya se ha vendido, no podemos borrarlo físicamente. Realizamos una desactivación.
                producto.Activo = false;
                _context.Productos.Update(producto);
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    esBorradoFisico = false, 
                    mensaje = "El producto tiene ventas registradas en el historial. Se ha desactivado y ocultado del catálogo para proteger la contabilidad histórica de los pedidos." 
                });
            }
            else
            {
                // Si no tiene ventas, lo eliminamos físicamente por completo
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    esBorradoFisico = true, 
                    mensaje = "El producto se ha eliminado físicamente de la base de datos de manera definitiva." 
                });
            }
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

        // POST: api/AdminProductosApi/subir-imagenes-masivas
        [HttpPost("subir-imagenes-masivas")]
        public async Task<IActionResult> SubirImagenesMasivas([FromForm] List<IFormFile> imagenes)
        {
            if (imagenes == null || imagenes.Count == 0)
            {
                return BadRequest(new { mensaje = "No se recibieron imágenes para subir." });
            }

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var carpetaDestino = Path.Combine(_env.WebRootPath, "images", "productos");
            if (!Directory.Exists(carpetaDestino))
            {
                Directory.CreateDirectory(carpetaDestino);
            }

            int subidasExitosas = 0;
            var errores = new List<string>();

            foreach (var imagen in imagenes)
            {
                try
                {
                    var extension = Path.GetExtension(imagen.FileName).ToLower();
                    if (!extensionesPermitidas.Contains(extension))
                    {
                        errores.Add($"Archivo '{imagen.FileName}' no permitido: extensión inválida.");
                        continue;
                    }

                    // Limpiar y sanitizar el nombre original del archivo
                    var nombreOriginal = Path.GetFileName(imagen.FileName).ToLower();
                    var rutaFisica = Path.Combine(carpetaDestino, nombreOriginal);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await imagen.CopyToAsync(stream);
                    }
                    subidasExitosas++;
                }
                catch (Exception ex)
                {
                    errores.Add($"Error al subir '{imagen.FileName}': {ex.Message}");
                }
            }

            return Ok(new 
            { 
                mensaje = $"Se subieron {subidasExitosas} imágenes correctamente.",
                errores = errores
            });
        }

        // GET: api/AdminProductosApi/plantilla-excel
        [HttpGet("plantilla-excel")]
        public async Task<IActionResult> DescargarPlantilla()
        {
            var productos = await _context.Productos.Include(p => p.Categoria).OrderBy(p => p.Id).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Productos");
                var currentRow = 1;

                // Encabezados
                worksheet.Cell(currentRow, 1).Value = "Nombre";
                worksheet.Cell(currentRow, 2).Value = "Descripcion";
                worksheet.Cell(currentRow, 3).Value = "Precio";
                worksheet.Cell(currentRow, 4).Value = "Stock";
                worksheet.Cell(currentRow, 5).Value = "Categoria";
                worksheet.Cell(currentRow, 6).Value = "ImagenUrl";

                // Estilos para el encabezado
                var headerRange = worksheet.Range(1, 1, 1, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

                // Cargar datos reales
                foreach (var prod in productos)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = prod.Nombre;
                    worksheet.Cell(currentRow, 2).Value = prod.Descripcion;
                    worksheet.Cell(currentRow, 3).Value = prod.Precio;
                    worksheet.Cell(currentRow, 4).Value = prod.Stock;
                    worksheet.Cell(currentRow, 5).Value = prod.Categoria != null ? prod.Categoria.Nombre : "";
                    worksheet.Cell(currentRow, 6).Value = prod.ImagenUrl;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Inventario_Productos.xlsx");
                }
            }
        }

        // POST: api/AdminProductosApi/carga-masiva
        [HttpPost("carga-masiva")]
        public async Task<IActionResult> CargaMasiva([FromForm] IFormFile archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.Length == 0)
            {
                return BadRequest(new { mensaje = "Por favor, sube un archivo Excel válido." });
            }

            if (!Path.GetExtension(archivoExcel.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { mensaje = "El archivo debe ser un .xlsx" });
            }

            int productosAgregados = 0;
            int productosActualizados = 0;

            using (var stream = new MemoryStream())
            {
                await archivoExcel.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Saltar encabezados

                    foreach (var row in rows)
                    {
                        var nombre = row.Cell(1).GetString().Trim();
                        if (string.IsNullOrEmpty(nombre)) continue; // Saltar filas vacías

                        var descripcion = row.Cell(2).GetString().Trim();
                        var precioStr = row.Cell(3).GetString().Trim();
                        var stockStr = row.Cell(4).GetString().Trim();
                        var categoriaNombre = row.Cell(5).GetString().Trim();
                        var cellImagen = row.Cell(6);
                        var imagenUrl = "";
                        
                        // 1. Try to extract a physically embedded picture floating/placed on this specific cell (Column 6 / Column F)
                        var embeddedPic = worksheet.Pictures.FirstOrDefault(p => 
                            p.TopLeftCell != null && 
                            p.TopLeftCell.Address.RowNumber == row.RowNumber() && 
                            p.TopLeftCell.Address.ColumnNumber == 6);
                            
                        if (embeddedPic != null && embeddedPic.ImageStream != null)
                        {
                            try
                            {
                                var formatStr = embeddedPic.Format.ToString().ToLower();
                                var ext = ".png"; // default
                                if (formatStr.Contains("jpeg") || formatStr.Contains("jpg")) ext = ".jpg";
                                else if (formatStr.Contains("webp")) ext = ".webp";
                                else if (formatStr.Contains("gif")) ext = ".gif";

                                var nombreUnico = $"{Guid.NewGuid()}{ext}";
                                var carpetaDestinoImg = Path.Combine(_env.WebRootPath, "images", "productos");
                                if (!Directory.Exists(carpetaDestinoImg))
                                {
                                    Directory.CreateDirectory(carpetaDestinoImg);
                                }
                                
                                var rutaFisicaCompleta = Path.Combine(carpetaDestinoImg, nombreUnico);
                                
                                embeddedPic.ImageStream.Position = 0; // ensure at start
                                using (var fileStream = new FileStream(rutaFisicaCompleta, FileMode.Create))
                                {
                                    await embeddedPic.ImageStream.CopyToAsync(fileStream);
                                }
                                
                                imagenUrl = $"/images/productos/{nombreUnico}";
                            }
                            catch (Exception ex)
                            {
                                // Fail-silent and fallback to text parsing
                            }
                        }

                        // 2. If no physically embedded picture, fallback to robust text/hyperlink parsing
                        if (string.IsNullOrEmpty(imagenUrl))
                        {
                            var hyperlink = worksheet.Hyperlinks.FirstOrDefault(h => h.Cell.Address.Equals(cellImagen.Address));
                            if (hyperlink != null)
                            {
                                imagenUrl = hyperlink.ExternalAddress?.ToString()?.Trim() ?? "";
                            }
                            
                            if (string.IsNullOrEmpty(imagenUrl))
                            {
                                imagenUrl = cellImagen.GetString().Trim();
                            }

                            // Robust handling of simple filenames (e.g. "manzana.png") by auto-prepending the product image folder
                            if (!string.IsNullOrEmpty(imagenUrl) && 
                                !imagenUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                                !imagenUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && 
                                !imagenUrl.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                            {
                                imagenUrl = $"/images/productos/{imagenUrl}";
                            }
                        }

                        if (!decimal.TryParse(precioStr, out decimal precio)) precio = 0;
                        if (!int.TryParse(stockStr, out int stock)) stock = 0;

                        // Buscar o crear categoría
                        int categoriaId = 1; // Default fallback si todo falla
                        if (!string.IsNullOrEmpty(categoriaNombre))
                        {
                            var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == categoriaNombre.ToLower());
                            if (categoria == null)
                            {
                                categoria = new Categoria
                                {
                                    Nombre = categoriaNombre,
                                    Descripcion = "Categoría creada automáticamente",
                                    Activo = true
                                };
                                _context.Categorias.Add(categoria);
                                await _context.SaveChangesAsync();
                            }
                            categoriaId = categoria.Id;
                        }

                        // Buscar si el producto existe
                        var productoExistente = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre.ToLower() == nombre.ToLower());

                        if (productoExistente != null)
                        {
                            // Actualizar
                            decimal precioAnterior = productoExistente.Precio;

                            productoExistente.Descripcion = descripcion;
                            productoExistente.Precio = precio;
                            productoExistente.Stock = stock;
                            productoExistente.CategoriaId = categoriaId;
                            if (!string.IsNullOrEmpty(imagenUrl)) productoExistente.ImagenUrl = imagenUrl;
                            productoExistente.Activo = true; // Asegurar que está activo si se sube por excel

                            if (precioAnterior != precio)
                            {
                                _context.HistorialPrecios.Add(new HistorialPrecio
                                {
                                    Producto = productoExistente,
                                    PrecioAnterior = precioAnterior,
                                    PrecioNuevo = precio,
                                    FechaCambio = DateTime.UtcNow
                                });
                            }
                            _context.Productos.Update(productoExistente);
                            productosActualizados++;
                        }
                        else
                        {
                            // Crear nuevo
                            var nuevoProducto = new Producto
                            {
                                Nombre = nombre,
                                Descripcion = descripcion,
                                Precio = precio,
                                Stock = stock,
                                CategoriaId = categoriaId,
                                ImagenUrl = string.IsNullOrEmpty(imagenUrl) ? "/images/productos/default-grocery.png" : imagenUrl,
                                Activo = true
                            };
                            _context.Productos.Add(nuevoProducto);

                            _context.HistorialPrecios.Add(new HistorialPrecio
                            {
                                Producto = nuevoProducto,
                                PrecioAnterior = 0,
                                PrecioNuevo = precio,
                                FechaCambio = DateTime.UtcNow
                            });
                            productosAgregados++;
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { mensaje = $"Carga masiva completada. {productosAgregados} agregados, {productosActualizados} actualizados." });
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
