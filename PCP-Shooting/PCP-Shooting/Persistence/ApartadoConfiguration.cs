using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class ApartadoConfiguration : IEntityTypeConfiguration<Apartado>, IEntityTypeConfiguration<LineaApartado>
{
    public void Configure(EntityTypeBuilder<Apartado> entidad)
    {
        entidad.HasKey(a => a.Id);
        entidad.Property(a => a.Id).ValueGeneratedNever();
        entidad.Property(a => a.ZonaHoraria).HasMaxLength(80).IsRequired();
        entidad.HasOne<PerfilUsuario>().WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        entidad.HasOne<Paquete>().WithMany().HasForeignKey(a => a.PaqueteId).OnDelete(DeleteBehavior.Restrict);
        entidad.HasIndex(a => new { a.UsuarioId, a.CreadoEnUtc });
        entidad.HasIndex(a => new { a.VenceEnUtc, a.InicioUtc, a.FinOcupacionUtc });
        entidad.ToTable("Apartados", tabla =>
        {
            tabla.HasCheckConstraint("CK_Apartado_Ocupacion", "\"FinActividadUtc\" > \"InicioUtc\" AND \"FinOcupacionUtc\" >= \"FinActividadUtc\"");
            tabla.HasCheckConstraint("CK_Apartado_Vencimiento", "\"VenceEnUtc\" > \"CreadoEnUtc\"");
        });
    }

    public void Configure(EntityTypeBuilder<LineaApartado> entidad)
    {
        entidad.HasKey(l => l.Id);
        entidad.HasOne(l => l.Apartado).WithMany(a => a.Lineas).HasForeignKey(l => l.ApartadoId).OnDelete(DeleteBehavior.Restrict);
        entidad.HasOne<LineaTiro>().WithMany().HasForeignKey(l => l.LineaId).OnDelete(DeleteBehavior.Restrict);
        entidad.HasIndex(l => new { l.ApartadoId, l.LineaId }).IsUnique();
        entidad.HasIndex(l => new { l.LineaId, l.ApartadoId });
        entidad.ToTable("LineasApartado");
    }
}
