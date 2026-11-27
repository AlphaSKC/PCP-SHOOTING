using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;
namespace PCP_Shooting.Persistence;

public sealed class PortadaPaginaConfiguration : IEntityTypeConfiguration<PortadaPagina>, IEntityTypeConfiguration<CargaPortadaPagina>
{
    public void Configure(EntityTypeBuilder<PortadaPagina> b)
    {
        b.ToTable("PortadasPagina", t => {
            t.HasCheckConstraint("CK_Portada_Pagina", "\"Pagina\" IN ('inicio','nosotros','reservar','catalogo','tienda','eventos','alimentos','membresias','contacto')");
            t.HasCheckConstraint("CK_Portada_Imagen", "NOT (\"Configurada\" AND \"Activa\") OR length(trim(\"ImagenUrl\")) > 0 AND \"ImagenUrl\" IS NOT NULL");
            t.HasCheckConstraint("CK_Portada_Posicion", "\"Posicion\" ~ '^(100|[1-9]?[0-9])% (100|[1-9]?[0-9])%$'");
            t.HasCheckConstraint("CK_Portada_Velo", "\"Velo\" IN ('suave','normal','fuerte')");
        });
        b.HasKey(p => p.Id); b.Property(p => p.Id).ValueGeneratedNever(); b.Property(p => p.Pagina).HasMaxLength(20); b.HasIndex(p => p.Pagina).IsUnique();
        b.Property(p => p.ImagenUrl).HasMaxLength(2048); b.Property(p => p.Posicion).HasMaxLength(9); b.Property(p => p.Velo).HasMaxLength(6);
        b.Property(p => p.Version).IsConcurrencyToken();
    }
    public void Configure(EntityTypeBuilder<CargaPortadaPagina> b)
    {
        b.ToTable("CargasPortadaPagina", t => {
            t.HasCheckConstraint("CK_CargaPortada_Tamano", "\"Tamano\" > 0 AND \"Tamano\" <= 8388608");
            t.HasCheckConstraint("CK_CargaPortada_Disponible", "NOT \"Disponible\" OR \"SubidaEnUtc\" IS NOT NULL");
        });
        b.HasKey(c => c.Id); b.Property(c => c.Id).ValueGeneratedNever(); b.HasIndex(c => c.Ruta).IsUnique();
        b.Property(c => c.Ruta).HasMaxLength(200); b.Property(c => c.Sha256).HasMaxLength(64); b.Property(c => c.TipoContenido).HasMaxLength(30);
        b.HasOne<PortadaPagina>().WithMany().HasForeignKey(c => c.PortadaId); b.HasOne<PerfilUsuario>().WithMany().HasForeignKey(c => c.CreadorId);
    }
}
