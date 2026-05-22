// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
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
    [Authorize(Roles = "Admin")]
    public class AdminCategoriasApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminCategoriasApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AdminCategoriasApi
        [HttpGet]
        public async Task<IActionResult> GetCategorias()
        {
            var categorias = await _context.Categorias
                .OrderBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Descripcion,
                    c.Activo,
                    CantProductos = c.Productos.Count(p => p.Activo)
                })
                .ToListAsync();

            return Ok(categorias);
        }

        // GET: api/AdminCategoriasApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound(new { mensaje = "Categoría no encontrada." });
            }

            return Ok(new
            {
                categoria.Id,
                categoria.Nombre,
                categoria.Descripcion,
                categoria.Activo
            });
        }

        // POST: api/AdminCategoriasApi
        [HttpPost]
        public async Task<IActionResult> CrearCategoria([FromBody] CategoriaSaveRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(request.Nombre))
            {
                return BadRequest(new { mensaje = "El nombre de la categoría es obligatorio." });
            }

            // Verificar si ya existe una categoría con el mismo nombre (fácil comparación)
            var existe = await _context.Categorias.AnyAsync(c => c.Nombre.ToLower() == request.Nombre.ToLower());
            if (existe)
            {
                return BadRequest(new { mensaje = "Ya existe una categoría con este nombre." });
            }

            var categoria = new Categoria
            {
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                Activo = request.Activo
            };

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Categoría creada con éxito.", categoriaId = categoria.Id });
        }

        // PUT: api/AdminCategoriasApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarCategoria(int id, [FromBody] CategoriaSaveRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound(new { mensaje = "Categoría no encontrada." });
            }

            if (string.IsNullOrEmpty(request.Nombre))
            {
                return BadRequest(new { mensaje = "El nombre de la categoría es obligatorio." });
            }

            // Verificar si el nombre nuevo ya está en uso por otra categoría
            var existe = await _context.Categorias.AnyAsync(c => c.Id != id && c.Nombre.ToLower() == request.Nombre.ToLower());
            if (existe)
            {
                return BadRequest(new { mensaje = "Ya existe otra categoría con este nombre." });
            }

            categoria.Nombre = request.Nombre;
            categoria.Descripcion = request.Descripcion;
            categoria.Activo = request.Activo;

            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Categoría actualizada con éxito." });
        }

        // DELETE: api/AdminCategoriasApi/5 (Desactivación lógica)
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound(new { mensaje = "Categoría no encontrada." });
            }

            // Borrado lógico para conservar integridad referencial de los productos ya comprados
            categoria.Activo = false;
            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Categoría desactivada correctamente." });
        }
    }

    public class CategoriaSaveRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
    }
}
