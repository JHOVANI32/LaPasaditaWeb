// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.Linq;
using LaPasaditaWeb.Models;

namespace LaPasaditaWeb.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // 1. Sembrar Configuración Básica de la Tienda (Requerido para no causar errores en la UI)
            if (!context.ConfiguracionTienda.Any())
            {
                context.ConfiguracionTienda.Add(new ConfiguracionTienda
                {
                    NombreTienda = "Abarrotes La Pasadita",
                    TelefonoContacto = "4891234567",
                    EmailContacto = "contacto@lapasadita.com",
                    DireccionFisica = "Calle Principal S/N, Centro, Axtla de Terrazas, S.L.P., C.P. 79930",
                    HorarioAtencion = "Lunes a Domingo 7:00 AM - 10:00 PM",
                    CostoEnvioBase = 15.00m
                });
                context.SaveChanges();
            }

            // 2. Sembrar Único Usuario Administrador Real
            if (!context.Usuarios.Any())
            {
                context.Usuarios.Add(new Usuario
                {
                    Nombre = "Administrador",
                    Email = "admin@lapasadita.com",
                    PasswordHash = "12345", // Contraseña real solicitada
                    Rol = "Admin",
                    Activo = true,
                    FechaRegistro = DateTime.UtcNow
                });
                context.SaveChanges();
            }
        }
    }
}
