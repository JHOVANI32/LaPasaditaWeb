using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;
using System.Security.Claims;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/Perfil")]
    [Authorize]
    public class PerfilApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PerfilApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Perfil
        [HttpGet]
        public async Task<IActionResult> GetPerfil()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { mensaje = "No estás autenticado." });
            }

            var usuario = await _context.Usuarios.FindAsync(int.Parse(userIdClaim));
            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            return Ok(new PerfilResponse
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                Rol = usuario.Rol
            });
        }

        // PUT: api/Perfil
        [HttpPut]
        public async Task<IActionResult> UpdatePerfil([FromBody] PerfilUpdateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { mensaje = "No estás autenticado." });
            }

            var usuario = await _context.Usuarios.FindAsync(int.Parse(userIdClaim));
            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            if (string.IsNullOrEmpty(request.Nombre) || string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { mensaje = "El nombre y el correo son obligatorios." });
            }

            var emailDuplicado = await _context.Usuarios
                .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != usuario.Id);
            if (emailDuplicado)
            {
                return BadRequest(new { mensaje = "Este correo electrónico ya está en uso por otra cuenta." });
            }

            if (!string.IsNullOrEmpty(request.PasswordNuevo))
            {
                if (string.IsNullOrEmpty(request.PasswordActual))
                {
                    return BadRequest(new { mensaje = "Debes ingresar tu contraseña actual para establecer una nueva." });
                }

                if (usuario.PasswordHash != request.PasswordActual)
                {
                    return BadRequest(new { mensaje = "La contraseña actual es incorrecta." });
                }

                usuario.PasswordHash = request.PasswordNuevo;
            }

            usuario.Nombre = request.Nombre;
            usuario.Email = request.Email;
            usuario.Telefono = request.Telefono;

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            // Regenerar la cookie de autenticación con los nuevos claims en caliente
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return Ok(new { mensaje = "Perfil actualizado con éxito." });
        }
    }

    public class PerfilResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Rol { get; set; } = string.Empty;
    }

    public class PerfilUpdateRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? PasswordActual { get; set; }
        public string? PasswordNuevo { get; set; }
    }
}
