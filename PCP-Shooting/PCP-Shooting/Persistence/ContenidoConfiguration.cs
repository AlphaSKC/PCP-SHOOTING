using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class ContenidoConfiguration : IEntityTypeConfiguration<MarcaAliada>, IEntityTypeConfiguration<CargaImagenContenido>
{
    public void Configure(EntityTypeBuilder<MarcaAliada> e)
    {
        e.HasKey(m => m.Id); e.Property(m => m.Nombre).HasMaxLength(80).IsRequired();
        e.Property(m => m.Descripcion).HasMaxLength(60); e.Property(m => m.Url).HasMaxLength(2048);
        e.Property(m => m.LogoUrl).HasMaxLength(2048); e.Property(m => m.Version).IsConcurrencyToken();
        e.HasIndex(m => new { m.Activo, m.Orden });
        e.ToTable("MarcasAliadas", t => { t.HasCheckConstraint("CK_Marca_Nombre", "length(trim(\"Nombre\")) > 0"); t.HasCheckConstraint("CK_Marca_Orden", "\"Orden\" >= 0"); });
        var nombres = new[] { "Airguns FV", "Altaros", "ZAN Projectiles", "Donny FL", "AEA", "H&V" };
        for (var i = 0; i < nombres.Length; i++) e.HasData(new MarcaAliada
        {
            Id = Guid.Parse($"e4ec8df0-0000-4000-8000-{110 + i:000000000000}"),
            Nombre = nombres[i],
            Activo = true,
            Orden = i,
            Version = Guid.Parse($"e4ec8df0-0000-4000-8000-{210 + i:000000000000}"),
            CreadoEnUtc = new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc),
            ActualizadoEnUtc = new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc)
        });
    }
    public void Configure(EntityTypeBuilder<CargaImagenContenido> e)
    {
        e.HasKey(c => c.Id); e.Property(c => c.Id).ValueGeneratedNever();
        e.Property(c => c.Ruta).HasMaxLength(200).IsRequired(); e.HasIndex(c => c.Ruta).IsUnique();
        e.Property(c => c.Sha256).HasMaxLength(64).IsRequired(); e.Property(c => c.TipoContenido).HasMaxLength(30).IsRequired();
        e.HasOne<MarcaAliada>().WithMany().HasForeignKey(c => c.MarcaId);
        e.HasOne<ArticuloBlog>().WithMany().HasForeignKey(c => c.ArticuloBlogId);
        e.HasOne<PerfilUsuario>().WithMany().HasForeignKey(c => c.CreadorId);
        e.ToTable("CargasImagenContenido", t =>
        {
            t.HasCheckConstraint("CK_CargaContenido_Destino", "(\"MarcaId\" IS NOT NULL AND \"ArticuloBlogId\" IS NULL) OR (\"MarcaId\" IS NULL AND \"ArticuloBlogId\" IS NOT NULL)");
            t.HasCheckConstraint("CK_CargaContenido_Tamano", "\"Tamano\" > 0 AND \"Tamano\" <= CASE WHEN \"MarcaId\" IS NOT NULL THEN 2097152 ELSE 5242880 END");
            t.HasCheckConstraint("CK_CargaContenido_Disponible", "NOT \"Disponible\" OR \"SubidaEnUtc\" IS NOT NULL");
        });
    }
}
