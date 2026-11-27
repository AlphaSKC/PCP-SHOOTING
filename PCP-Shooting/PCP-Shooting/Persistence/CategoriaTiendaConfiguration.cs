using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;
namespace PCP_Shooting.Persistence;

public sealed class CategoriaTiendaConfiguration : IEntityTypeConfiguration<CategoriaTienda>
{
    public void Configure(EntityTypeBuilder<CategoriaTienda> b)
    {
        b.ToTable("CategoriasTienda", t => {
            t.HasCheckConstraint("CK_CategoriaTienda_Nombre", "length(trim(\"Nombre\")) > 0");
            t.HasCheckConstraint("CK_CategoriaTienda_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$' AND \"Slug\" NOT IN ('carrito','checkout','pedido','producto')");
            t.HasCheckConstraint("CK_CategoriaTienda_Orden", "\"Orden\" >= 0");
        });
        b.HasKey(c => c.Id); b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.Nombre).HasMaxLength(60).IsRequired(); b.Property(c => c.Slug).HasMaxLength(60).IsRequired();
        b.Property(c => c.Descripcion).HasMaxLength(300); b.Property(c => c.ImagenUrl).HasMaxLength(2048);
        b.Property(c => c.Version).IsConcurrencyToken(); b.HasIndex(c => c.Slug).IsUnique(); b.HasIndex(c => new { c.Activa, c.Orden });
    }
}
