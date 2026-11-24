using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Configurations;

public sealed class GaleriaConfiguration : IEntityTypeConfiguration<AlbumGaleria>, IEntityTypeConfiguration<FotoGaleria>, IEntityTypeConfiguration<CargaFotoGaleria>
{
    public void Configure(EntityTypeBuilder<AlbumGaleria> b)
    {
        b.ToTable("AlbumsGaleria", t => { t.HasCheckConstraint("CK_Album_Nombre", "length(trim(\"Nombre\")) > 0"); t.HasCheckConstraint("CK_Album_Orden", "\"Orden\" >= 0"); });
        b.HasKey(x => x.Id); b.Property(x => x.Nombre).HasMaxLength(120); b.Property(x => x.Descripcion).HasMaxLength(500);
        b.Property(x => x.Version).IsConcurrencyToken(); b.HasIndex(x => new { x.Publicado, x.Orden, x.Id });
    }
    public void Configure(EntityTypeBuilder<FotoGaleria> b)
    {
        b.ToTable("FotosGaleria", t => {
            t.HasCheckConstraint("CK_Foto_Textos", "length(trim(\"Titulo\")) > 0 AND length(trim(\"TextoAlternativo\")) > 0");
            t.HasCheckConstraint("CK_Foto_Orden", "\"Orden\" >= 0");
            t.HasCheckConstraint("CK_Foto_Publicada", "NOT \"Publicada\" OR \"ImagenUrl\" IS NOT NULL");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Titulo).HasMaxLength(150); b.Property(x => x.TextoAlternativo).HasMaxLength(250);
        b.Property(x => x.Descripcion).HasMaxLength(1000); b.Property(x => x.ImagenUrl).HasMaxLength(2048); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne(x => x.Album).WithMany(x => x.Fotos).HasForeignKey(x => x.AlbumId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.AlbumId, x.Publicada, x.Orden, x.Id });
    }
    public void Configure(EntityTypeBuilder<CargaFotoGaleria> b)
    {
        b.ToTable("CargasFotosGaleria", t => t.HasCheckConstraint("CK_CargaGaleria_Tamano", "\"Tamano\" > 0 AND \"Tamano\" <= 5242880"));
        b.HasKey(x => x.Id); b.Property(x => x.Ruta).HasMaxLength(200); b.HasIndex(x => x.Ruta).IsUnique();
        b.Property(x => x.Sha256).HasMaxLength(64); b.Property(x => x.TipoContenido).HasMaxLength(30);
        b.HasOne(x => x.Foto).WithMany().HasForeignKey(x => x.FotoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PerfilUsuario>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
