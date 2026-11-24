using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> b)
    {
        b.ToTable("Eventos", t => {
            t.HasCheckConstraint("CK_Evento_Estado", "\"Estado\" IN (0, 1, 2)");
            t.HasCheckConstraint("CK_Evento_Fechas", "\"FinUtc\" > \"InicioUtc\"");
            t.HasCheckConstraint("CK_Evento_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            t.HasCheckConstraint("CK_Evento_Textos", "length(trim(\"Titulo\")) > 0 AND length(trim(\"Resumen\")) > 0 AND length(trim(\"DescripcionMarkdown\")) > 0");
        });
        b.HasKey(e => e.Id); b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.Slug).HasMaxLength(160).IsRequired(); b.HasIndex(e => e.Slug).IsUnique();
        b.Property(e => e.Titulo).HasMaxLength(200).IsRequired(); b.Property(e => e.Resumen).HasMaxLength(600).IsRequired();
        b.Property(e => e.DescripcionMarkdown).HasMaxLength(30000).IsRequired();
        b.Property(e => e.Ubicacion).HasMaxLength(250); b.Property(e => e.ImagenUrl).HasMaxLength(2048);
        b.Property(e => e.Version).IsConcurrencyToken();
        b.HasIndex(e => new { e.Estado, e.InicioUtc, e.Id }); b.HasIndex(e => e.FinUtc);
    }
}
