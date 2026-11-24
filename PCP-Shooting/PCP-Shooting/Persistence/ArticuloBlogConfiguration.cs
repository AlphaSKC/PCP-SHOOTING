using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class ArticuloBlogConfiguration : IEntityTypeConfiguration<ArticuloBlog>
{
    public void Configure(EntityTypeBuilder<ArticuloBlog> articulo)
    {
        articulo.HasKey(a => a.Id);
        articulo.Property(a => a.Slug).HasMaxLength(160).IsRequired();
        articulo.HasIndex(a => a.Slug).IsUnique();
        articulo.Property(a => a.Titulo).HasMaxLength(200).IsRequired();
        articulo.Property(a => a.Resumen).HasMaxLength(600).IsRequired();
        articulo.Property(a => a.ContenidoMarkdown).HasMaxLength(100000).IsRequired();
        articulo.Property(a => a.ImagenUrl).HasMaxLength(2048);
        articulo.Property(a => a.Version).IsConcurrencyToken();
        articulo.HasOne<PerfilUsuario>().WithMany().HasForeignKey(a => a.AutorId).OnDelete(DeleteBehavior.Restrict);
        articulo.HasIndex(a => new { a.Estado, a.PublicarEnUtc });
        articulo.HasIndex(a => new { a.Categoria, a.PublicarEnUtc });
        articulo.ToTable("ArticulosBlog", tabla =>
        {
            tabla.HasCheckConstraint("CK_Blog_Estado", "\"Estado\" IN (0, 1, 2, 3)");
            tabla.HasCheckConstraint("CK_Blog_Categoria", "\"Categoria\" IN (0, 1, 2, 3)");
            tabla.HasCheckConstraint("CK_Blog_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            tabla.HasCheckConstraint("CK_Blog_Textos", "length(trim(\"Titulo\")) > 0 AND length(trim(\"Resumen\")) > 0 AND length(trim(\"ContenidoMarkdown\")) > 0");
            tabla.HasCheckConstraint("CK_Blog_FechaPublicacion", "\"Estado\" NOT IN (1, 2) OR \"PublicarEnUtc\" IS NOT NULL");
        });
    }
}
