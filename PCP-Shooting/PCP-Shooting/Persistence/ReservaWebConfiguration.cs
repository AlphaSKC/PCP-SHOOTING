using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class ReservaWebConfiguration : IEntityTypeConfiguration<ReservaWeb>, IEntityTypeConfiguration<ParticipanteReserva>, IEntityTypeConfiguration<ConsumoReserva>
{
    public void Configure(EntityTypeBuilder<ReservaWeb> e)
    {
        e.HasKey(r => r.Id);
        e.Property(r => r.Id).ValueGeneratedNever();
        e.HasIndex(r => r.ApartadoId).IsUnique().HasFilter("\"ApartadoId\" IS NOT NULL");
        e.HasIndex(r => new { r.UsuarioId, r.CreadoEnUtc });
        e.HasOne<Apartado>().WithMany().HasForeignKey(r => r.ApartadoId).IsRequired(false);
        e.Property(r => r.EsMostrador).HasDefaultValue(false);
        e.HasOne(r => r.Usuario).WithMany().HasForeignKey(r => r.UsuarioId);
        e.HasOne(r => r.CreadoPor).WithMany().HasForeignKey(r => r.CreadoPorId);
        e.Property(r => r.CodigoPaquete).HasMaxLength(80).IsRequired();
        e.Property(r => r.NombrePaquete).HasMaxLength(200).IsRequired();
        e.Property(r => r.DescripcionPaquete).HasMaxLength(4000);
        e.Property(r => r.IncluyeMarkdown).HasMaxLength(10000).IsRequired();
        e.Property(r => r.RequisitosMarkdown).HasMaxLength(10000);
        e.Property(r => r.SolicitudHash).HasMaxLength(64).IsRequired();
        e.Property(r => r.ZonaHoraria).HasMaxLength(80).IsRequired();
        e.Property(r => r.PrecioPorPersona).HasPrecision(12, 2);
        e.Property(r => r.PrecioSocioInformativo).HasPrecision(12, 2);
        e.Property(r => r.Total).HasPrecision(12, 2);
        e.ToTable("ReservasWeb", t =>
        {
            t.HasCheckConstraint("CK_ReservaWeb_Precios", "\"PrecioPorPersona\" > 0 AND \"Total\" > 0 AND (\"PrecioSocioInformativo\" IS NULL OR \"PrecioSocioInformativo\" > 0)");
            t.HasCheckConstraint("CK_ReservaWeb_Tiempos", "\"FinActividadUtc\" > \"InicioUtc\" AND \"FinOcupacionUtc\" >= \"FinActividadUtc\" AND \"VencePagoEnUtc\" > \"CreadoEnUtc\"");
        });
    }
    public void Configure(EntityTypeBuilder<ParticipanteReserva> e)
    {
        e.HasKey(p => p.Id);
        e.HasOne(p => p.Reserva).WithMany(r => r.Participantes).HasForeignKey(p => p.ReservaId);
        e.HasOne(p => p.Linea).WithMany().HasForeignKey(p => p.LineaId);
        e.HasIndex(p => new { p.ReservaId, p.LineaId }).IsUnique();
        e.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
        e.Property(p => p.TutorNombre).HasMaxLength(150);
        e.ToTable("ParticipantesReserva", t => t.HasCheckConstraint("CK_Participante_Nombre", "length(trim(\"Nombre\")) > 0"));
    }
    public void Configure(EntityTypeBuilder<ConsumoReserva> e)
    {
        e.HasKey(c => c.Id);
        e.HasOne(c => c.Reserva).WithMany(r => r.Consumibles).HasForeignKey(c => c.ReservaId);
        e.HasOne<Producto>().WithMany().HasForeignKey(c => c.ProductoId);
        e.HasIndex(c => new { c.ReservaId, c.ProductoId }).IsUnique();
        e.Property(c => c.Sku).IsRequired();
        e.Property(c => c.Nombre).IsRequired();
        e.ToTable("ConsumosReserva", t => t.HasCheckConstraint("CK_ConsumoReserva_Cantidades", "\"CantidadPorPersona\" BETWEEN 1 AND 100000 AND \"CantidadTotal\" BETWEEN 1 AND 1500000"));
    }
}
