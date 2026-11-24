using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class SolicitudContactoConfiguration : IEntityTypeConfiguration<SolicitudContacto>
{
    public void Configure(EntityTypeBuilder<SolicitudContacto> b)
    {
        b.ToTable("SolicitudesContacto", t => {
            t.HasCheckConstraint("CK_Contacto_Nombre", "length(trim(\"Nombre\")) BETWEEN 2 AND 150");
            t.HasCheckConstraint("CK_Contacto_Mensaje", "length(trim(\"Mensaje\")) BETWEEN 10 AND 2000");
            t.HasCheckConstraint("CK_Contacto_Estado", "\"Estado\" BETWEEN 0 AND 2");
            t.HasCheckConstraint("CK_Contacto_Motivo", "\"Motivo\" IN ('reservacion','grupos','tienda','cotizacion','otro')");
        });
        b.HasKey(s => s.Id); b.Property(s => s.Id).ValueGeneratedNever();
        b.Property(s => s.Nombre).HasMaxLength(150); b.Property(s => s.Email).HasMaxLength(254); b.Property(s => s.Telefono).HasMaxLength(20);
        b.Property(s => s.Motivo).HasMaxLength(20); b.Property(s => s.Mensaje).HasMaxLength(2000); b.Property(s => s.SolicitudHash).HasMaxLength(64);
        b.Property(s => s.NotasInternas).HasMaxLength(4000); b.Property(s => s.Version).IsConcurrencyToken();
        b.HasIndex(s => new { s.Estado, s.CreadoEnUtc, s.Id }); b.HasIndex(s => new { s.CreadoEnUtc, s.Id });
    }
}
