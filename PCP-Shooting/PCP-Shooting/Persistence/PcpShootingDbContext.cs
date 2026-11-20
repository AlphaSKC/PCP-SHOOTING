using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class PcpShootingDbContext(DbContextOptions<PcpShootingDbContext> options, IHttpContextAccessor? solicitud = null) : DbContext(options)
{
    public DbSet<ConfiguracionOperativa> ConfiguracionOperativa => Set<ConfiguracionOperativa>();
    public DbSet<HorarioOperacion> HorariosOperacion => Set<HorarioOperacion>();
    public DbSet<LineaTiro> LineasTiro => Set<LineaTiro>();
    public DbSet<BloqueoOperacion> BloqueosOperacion => Set<BloqueoOperacion>();
    public DbSet<ArticuloBlog> ArticulosBlog => Set<ArticuloBlog>();
    public DbSet<ImagenProducto> ImagenesProducto => Set<ImagenProducto>();
    public DbSet<EventoAuditoria> EventosAuditoria => Set<EventoAuditoria>();
    public DbSet<PerfilUsuario> PerfilesUsuario => Set<PerfilUsuario>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<DetallePedido> DetallesPedido => Set<DetallePedido>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<MembresiaCartaq> Membresias => Set<MembresiaCartaq>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var eventos = AuditoriaCambios.Preparar(this, solicitud?.HttpContext);
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch
        {
            foreach (var evento in eventos) Entry(evento).State = EntityState.Detached;
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var eventos = AuditoriaCambios.Preparar(this, solicitud?.HttpContext);
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch
        {
            foreach (var evento in eventos) Entry(evento).State = EntityState.Detached;
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ArticuloBlogConfiguration());
        var operacion = new OperacionConfiguration();
        modelBuilder.ApplyConfiguration<ConfiguracionOperativa>(operacion);
        modelBuilder.ApplyConfiguration<HorarioOperacion>(operacion);
        modelBuilder.ApplyConfiguration<LineaTiro>(operacion);
        modelBuilder.ApplyConfiguration<BloqueoOperacion>(operacion);
        modelBuilder.Entity<ImagenProducto>().HasIndex(i => i.Ruta).IsUnique();
        modelBuilder.Entity<ImagenProducto>().Property(i => i.Ruta).HasMaxLength(200);
        modelBuilder.Entity<ImagenProducto>().Property(i => i.Sha256).HasMaxLength(64);
        modelBuilder.Entity<ImagenProducto>().Property(i => i.TipoContenido).HasMaxLength(30);
        modelBuilder.Entity<ImagenProducto>().HasOne(i => i.Producto).WithMany(p => p.Imagenes).HasForeignKey(i => i.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ImagenProducto>().ToTable(table =>
            table.HasCheckConstraint("CK_Imagen_Tamano", "\"Tamano\" > 0 AND \"Tamano\" <= 5242880"));
        modelBuilder.Entity<EventoAuditoria>().HasIndex(e => new { e.EntidadId, e.FechaUtc });
        modelBuilder.Entity<EventoAuditoria>().HasIndex(e => new { e.ActorId, e.FechaUtc });
        modelBuilder.Entity<EventoAuditoria>().HasIndex(e => e.FechaUtc);
        modelBuilder.Entity<PerfilUsuario>().Property(p => p.Nombre).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<PerfilUsuario>().Property(p => p.Telefono).HasMaxLength(30);
        modelBuilder.Entity<PerfilUsuario>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_Perfil_Rol", "\"Rol\" IN (0, 1, 2)");
            table.HasCheckConstraint("CK_Perfil_Nombre", "length(trim(\"Nombre\")) > 0");
        });
        modelBuilder.Entity<Producto>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_Producto_Precio", "\"Precio\" IS NULL OR \"Precio\" >= 0");
            table.HasCheckConstraint("CK_Producto_Vendible", "NOT \"EsVendible\" OR \"Precio\" IS NOT NULL");
        });
        modelBuilder.Entity<DetallePedido>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_Detalle_Cantidad", "\"Cantidad\" > 0");
            table.HasCheckConstraint("CK_Detalle_Precio", "\"PrecioUnitario\" >= 0");
        });
        modelBuilder.Entity<Pago>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_Pago_Importe", "\"Importe\" > 0");
            table.HasCheckConstraint("CK_Pago_Estado", "\"Estado\" IN (0, 1, 2, 3)");
            table.HasCheckConstraint("CK_Pago_Confirmacion", "\"Estado\" <> 1 OR \"ConfirmadoEnUtc\" IS NOT NULL");
        });
        modelBuilder.Entity<MembresiaCartaq>().ToTable(table =>
            table.HasCheckConstraint("CK_Membresia_Vigencia", "\"FinVigenciaExclusivo\" > \"InicioVigencia\""));
        modelBuilder.Entity<MovimientoInventario>().ToTable(table =>
            table.HasCheckConstraint("CK_Movimiento_Variacion",
                "(\"Tipo\" = 0 AND \"VariacionCantidad\" > 0) OR (\"Tipo\" = 1 AND \"VariacionCantidad\" < 0) OR (\"Tipo\" = 2 AND \"VariacionCantidad\" <> 0)"));
        modelBuilder.Entity<Pedido>().ToTable(table =>
            table.HasCheckConstraint("CK_Pedido_Estado", "\"Estado\" IN (0, 1, 2, 3)"));

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
        modelBuilder.Entity<Producto>().Property(p => p.ImagenUrl).HasMaxLength(2048);
        modelBuilder.Entity<Producto>().Property(p => p.NotasMarkdown).HasMaxLength(10000);
        modelBuilder.Entity<Producto>().Property(p => p.Categoria).HasDefaultValue(CategoriaProducto.SinClasificar);
        modelBuilder.Entity<Producto>().HasIndex(p => new { p.Categoria, p.Activo });
        modelBuilder.Entity<Producto>().ToTable(table =>
            table.HasCheckConstraint("CK_Producto_Categoria", "\"Categoria\" IN (0, 1, 2, 3)"));
        modelBuilder.Entity<Producto>().Property(p => p.Activo).HasDefaultValue(true);
        modelBuilder.Entity<Producto>().HasIndex(p => new { p.Activo, p.Nombre });
        modelBuilder.Entity<Producto>().ToTable(table =>
            table.HasCheckConstraint("CK_Producto_StockMinimo", "\"StockMinimo\" IS NULL OR \"StockMinimo\" >= 0"));
        modelBuilder.Entity<Producto>().HasIndex(producto => producto.Sku).IsUnique();
        modelBuilder.Entity<Producto>().Property(producto => producto.EspecificacionesTecnicas).HasColumnType("jsonb");
        modelBuilder.Entity<Producto>().Property(producto => producto.Precio).HasPrecision(12, 2);
        modelBuilder.Entity<DetallePedido>().Property(detalle => detalle.PrecioUnitario).HasPrecision(12, 2);
        modelBuilder.Entity<Pago>().Property(pago => pago.Importe).HasPrecision(12, 2);
        modelBuilder.Entity<Pago>().HasIndex(pago => pago.ReferenciaExterna).IsUnique();
        modelBuilder.Entity<MembresiaCartaq>().HasIndex(m => m.PagoId).IsUnique();
        modelBuilder.Entity<MovimientoInventario>().HasIndex(m => m.DetallePedidoId)
            .IsUnique().HasFilter("\"DetallePedidoId\" IS NOT NULL");
        modelBuilder.Entity<Pedido>().HasIndex(pedido => new { pedido.UsuarioId, pedido.CreadoEnUtc });
        modelBuilder.Entity<MembresiaCartaq>().HasIndex(membresia => new { membresia.UsuarioId, membresia.InicioVigencia });
        modelBuilder.Entity<MovimientoInventario>().HasIndex(movimiento => new { movimiento.ProductoId, movimiento.RegistradoEnUtc });
    }
}

