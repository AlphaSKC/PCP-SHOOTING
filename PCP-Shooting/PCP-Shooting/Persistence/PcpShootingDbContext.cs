using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class PcpShootingDbContext(DbContextOptions<PcpShootingDbContext> options) : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<DetallePedido> DetallesPedido => Set<DetallePedido>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<MembresiaCartaq> Membresias => Set<MembresiaCartaq>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>().HasIndex(producto => producto.Sku).IsUnique();
        modelBuilder.Entity<Producto>().Property(producto => producto.EspecificacionesTecnicas).HasColumnType("jsonb");
        modelBuilder.Entity<Producto>().Property(producto => producto.Precio).HasPrecision(12, 2);
        modelBuilder.Entity<DetallePedido>().Property(detalle => detalle.PrecioUnitario).HasPrecision(12, 2);
        modelBuilder.Entity<Pago>().Property(pago => pago.Importe).HasPrecision(12, 2);
        modelBuilder.Entity<Pago>().HasIndex(pago => pago.ReferenciaExterna).IsUnique();
        modelBuilder.Entity<Pedido>().HasIndex(pedido => new { pedido.UsuarioId, pedido.CreadoEnUtc });
        modelBuilder.Entity<MembresiaCartaq>().HasIndex(membresia => new { membresia.UsuarioId, membresia.InicioVigencia });
        modelBuilder.Entity<MovimientoInventario>().HasIndex(movimiento => new { movimiento.ProductoId, movimiento.RegistradoEnUtc });
    }
}
