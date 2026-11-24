using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class PaqueteConfiguration : IEntityTypeConfiguration<Paquete>, IEntityTypeConfiguration<ConsumiblePaquete>
{
    public void Configure(EntityTypeBuilder<Paquete> entidad)
    {
        entidad.HasKey(p => p.Id);
        entidad.Property(p => p.Codigo).HasMaxLength(80).IsRequired();
        entidad.HasIndex(p => p.Codigo).IsUnique();
        entidad.HasIndex(p => new { p.Activo, p.Nombre });
        entidad.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
        entidad.Property(p => p.Descripcion).HasMaxLength(4000);
        entidad.Property(p => p.IncluyeMarkdown).HasMaxLength(10000).IsRequired();
        entidad.Property(p => p.RequisitosMarkdown).HasMaxLength(10000);
        entidad.Property(p => p.PrecioPorPersona).HasPrecision(12, 2);
        entidad.Property(p => p.PrecioSocioCartaq).HasPrecision(12, 2);
        entidad.Property(p => p.Version).IsConcurrencyToken();
        entidad.ToTable("Paquetes", tabla =>
        {
            tabla.HasCheckConstraint("CK_Paquete_Codigo", "\"Codigo\" ~ '^[A-Z0-9]+(-[A-Z0-9]+)*$'");
            tabla.HasCheckConstraint("CK_Paquete_Textos", "length(trim(\"Nombre\")) > 0 AND length(trim(\"IncluyeMarkdown\")) > 0");
            tabla.HasCheckConstraint("CK_Paquete_Precios", "(\"PrecioPorPersona\" IS NULL OR \"PrecioPorPersona\" >= 0) AND (\"PrecioSocioCartaq\" IS NULL OR \"PrecioSocioCartaq\" >= 0)");
            tabla.HasCheckConstraint("CK_Paquete_Activo", "NOT \"Activo\" OR (\"PrecioPorPersona\" IS NOT NULL AND \"PrecioPorPersona\" > 0 AND (\"PrecioSocioCartaq\" IS NULL OR \"PrecioSocioCartaq\" > 0))");
        });
    }

    public void Configure(EntityTypeBuilder<ConsumiblePaquete> entidad)
    {
        entidad.HasKey(c => c.Id);
        entidad.HasIndex(c => new { c.PaqueteId, c.ProductoId }).IsUnique();
        entidad.HasOne(c => c.Paquete).WithMany(p => p.Consumibles).HasForeignKey(c => c.PaqueteId).OnDelete(DeleteBehavior.Restrict);
        entidad.HasOne(c => c.Producto).WithMany().HasForeignKey(c => c.ProductoId).OnDelete(DeleteBehavior.Restrict);
        entidad.ToTable("ConsumiblesPaquete", tabla => tabla.HasCheckConstraint("CK_ConsumiblePaquete_Cantidad", "\"CantidadPorPersona\" BETWEEN 1 AND 100000"));
    }
}
