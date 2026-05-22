// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Models;

namespace LaPasaditaWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ═══════════════════════════════════════════════════════════
        // Las 14 tablas del sistema (PostgreSQL / Supabase)
        // ═══════════════════════════════════════════════════════════
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Categoria> Categorias { get; set; } = null!;
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<SesionInvitado> SesionesInvitados { get; set; } = null!;
        public DbSet<CarritoTemporal> CarritoTemporales { get; set; } = null!;
        public DbSet<Direccion> Direcciones { get; set; } = null!;
        public DbSet<Cupon> Cupones { get; set; } = null!;
        public DbSet<Pedido> Pedidos { get; set; } = null!;
        public DbSet<DetallePedido> DetallesPedidos { get; set; } = null!;
        public DbSet<HistorialEstado> HistorialEstados { get; set; } = null!;
        public DbSet<HistorialPrecio> HistorialPrecios { get; set; } = null!;
        public DbSet<Promocion> Promociones { get; set; } = null!;
        public DbSet<CalificacionProducto> CalificacionesProductos { get; set; } = null!;
        public DbSet<ConfiguracionTienda> ConfiguracionTienda { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ═══════════════════════════════════════════════════════════
            // FLUENT API — Solo comportamientos OnDelete personalizados
            // Las FK ya están definidas con [ForeignKey] en los modelos,
            // aquí solo configuramos el comportamiento de eliminación.
            // ═══════════════════════════════════════════════════════════

            // Pedidos -> Usuarios (SetNull: conservar pedido si se borra usuario)
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.Pedidos)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            // Pedidos -> Cupones (SetNull)
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cupon)
                .WithMany(c => c.Pedidos)
                .HasForeignKey(p => p.CuponId)
                .OnDelete(DeleteBehavior.SetNull);

            // DetallePedido -> Pedido (Cascade)
            modelBuilder.Entity<DetallePedido>()
                .HasOne(dp => dp.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(dp => dp.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // DetallePedido -> Producto (Restrict: proteger historial)
            modelBuilder.Entity<DetallePedido>()
                .HasOne(dp => dp.Producto)
                .WithMany(p => p.Detalles)
                .HasForeignKey(dp => dp.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // HistorialEstado -> Pedido (Cascade)
            modelBuilder.Entity<HistorialEstado>()
                .HasOne(he => he.Pedido)
                .WithMany(p => p.HistorialEstados)
                .HasForeignKey(he => he.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // HistorialEstado -> Usuario (Restrict)
            modelBuilder.Entity<HistorialEstado>()
                .HasOne(he => he.UsuarioCambio)
                .WithMany()
                .HasForeignKey(he => he.UsuarioCambioId)
                .OnDelete(DeleteBehavior.Restrict);

            // HistorialPrecio -> Producto (Cascade)
            modelBuilder.Entity<HistorialPrecio>()
                .HasOne(hp => hp.Producto)
                .WithMany(p => p.HistorialPrecios)
                .HasForeignKey(hp => hp.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Promocion -> Producto (SetNull)
            modelBuilder.Entity<Promocion>()
                .HasOne(pr => pr.Producto)
                .WithMany(p => p.Promociones)
                .HasForeignKey(pr => pr.ProductoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Direccion -> Usuario (Cascade)
            modelBuilder.Entity<Direccion>()
                .HasOne(d => d.Usuario)
                .WithMany(u => u.Direcciones)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // CalificacionProducto -> Producto (Cascade)
            modelBuilder.Entity<CalificacionProducto>()
                .HasOne(cp => cp.Producto)
                .WithMany(p => p.Calificaciones)
                .HasForeignKey(cp => cp.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // CalificacionProducto -> Usuario (Cascade)
            modelBuilder.Entity<CalificacionProducto>()
                .HasOne(cp => cp.Usuario)
                .WithMany(u => u.Calificaciones)
                .HasForeignKey(cp => cp.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // CarritoTemporal -> SesionInvitado (Cascade)
            modelBuilder.Entity<CarritoTemporal>()
                .HasOne(c => c.SesionInvitado)
                .WithMany(s => s.CarritoItems)
                .HasForeignKey(c => c.SesionInvitadoId)
                .OnDelete(DeleteBehavior.Cascade);

            // CarritoTemporal -> Usuario (Cascade)
            modelBuilder.Entity<CarritoTemporal>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // CarritoTemporal -> Producto (Cascade)
            modelBuilder.Entity<CarritoTemporal>()
                .HasOne(c => c.Producto)
                .WithMany()
                .HasForeignKey(c => c.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ═══════════════════════════════════════════════════════════
            // ÍNDICES ÚNICOS
            // ═══════════════════════════════════════════════════════════
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<SesionInvitado>()
                .HasIndex(s => s.TokenSesion)
                .IsUnique();

            modelBuilder.Entity<Cupon>()
                .HasIndex(c => c.Codigo)
                .IsUnique();
        }
    }
}
