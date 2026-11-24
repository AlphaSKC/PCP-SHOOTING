using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class AvisoPublicoConfiguration : IEntityTypeConfiguration<AvisoPublico>
{
    public void Configure(EntityTypeBuilder<AvisoPublico> b)
    {
        b.ToTable("AvisosPublicos", t => {
            t.HasCheckConstraint("CK_AvisoPublico_Texto", "length(trim(\"Texto\")) BETWEEN 1 AND 1000");
            t.HasCheckConstraint("CK_AvisoPublico_Orden", "\"Orden\" >= 0");
            t.HasCheckConstraint("CK_AvisoPublico_Vigencia", "\"InicioUtc\" IS NULL OR \"FinUtc\" IS NULL OR \"FinUtc\" > \"InicioUtc\"");
        });
        b.HasKey(a => a.Id); b.Property(a => a.Id).ValueGeneratedNever();
        b.Property(a => a.Titulo).HasMaxLength(120); b.Property(a => a.Texto).HasMaxLength(1000).IsRequired();
        b.Property(a => a.Version).IsConcurrencyToken();
        b.HasIndex(a => new { a.Activo, a.Orden, a.Id });
        b.HasIndex(a => a.InicioUtc); b.HasIndex(a => a.FinUtc);
    }
}
