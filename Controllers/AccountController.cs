using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;

namespace LaPasaditaWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? guestToken = null, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "El correo y la contraseña son obligatorios.");
                return View();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password && u.Activo);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Correo electrónico o contraseña incorrectos.");
                return View();
            }

            // Crear los Claims (credenciales de identidad en la cookie)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Mantener la sesión iniciada
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            // Iniciar sesión (crea la cookie encriptada en el navegador)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // Fusión silenciosa de carritos si hay un token de invitado activo
            if (!string.IsNullOrEmpty(guestToken))
            {
                await FusionarCarritoInterno(guestToken, usuario.Id);
            }

            // Redirección inteligente según rol
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (usuario.Rol == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin"); // Supuesto administrador posterior
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string nombre, string email, string password, string? guestToken = null)
        {
            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Todos los campos son obligatorios.");
                return View();
            }

            // Verificar si el correo ya existe
            var existe = await _context.Usuarios.AnyAsync(u => u.Email == email);
            if (existe)
            {
                ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
                return View();
            }

            // Crear el nuevo usuario
            var usuario = new Usuario
            {
                Nombre = nombre,
                Email = email,
                PasswordHash = password, // Guardado directo simple para ambiente educativo
                Rol = "Cliente",
                Activo = true,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Autenticar automáticamente al registrarse
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // Fusión silenciosa del carrito
            if (!string.IsNullOrEmpty(guestToken))
            {
                await FusionarCarritoInterno(guestToken, usuario.Id);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Método auxiliar privado para fusionar carrito en el servidor durante el flujo de login
        private async Task FusionarCarritoInterno(string token, int usuarioId)
        {
            var sesion = await _context.SesionesInvitados.FirstOrDefaultAsync(s => s.TokenSesion == token);
            if (sesion == null) return;

            var itemsInvitado = await _context.CarritoTemporales
                .Where(c => c.SesionInvitadoId == sesion.Id)
                .ToListAsync();

            if (itemsInvitado.Any())
            {
                foreach (var itemInv in itemsInvitado)
                {
                    var itemUsuario = await _context.CarritoTemporales
                        .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.ProductoId == itemInv.ProductoId);

                    if (itemUsuario != null)
                    {
                        var producto = await _context.Productos.FindAsync(itemInv.ProductoId);
                        if (producto != null)
                        {
                            itemUsuario.Cantidad = Math.Min(itemUsuario.Cantidad + itemInv.Cantidad, producto.Stock);
                            _context.CarritoTemporales.Update(itemUsuario);
                        }
                        _context.CarritoTemporales.Remove(itemInv); // Eliminar el duplicado de invitado
                    }
                    else
                    {
                        itemInv.UsuarioId = usuarioId;
                        itemInv.SesionInvitadoId = null;
                        _context.CarritoTemporales.Update(itemInv);
                    }
                }

                _context.SesionesInvitados.Remove(sesion);
                await _context.SaveChangesAsync();
            }
        }
    }
}
