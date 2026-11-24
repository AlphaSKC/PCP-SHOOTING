using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class OperacionConfiguration : IEntityTypeConfiguration<ConfiguracionOperativa>,
    IEntityTypeConfiguration<HorarioOperacion>, IEntityTypeConfiguration<LineaTiro>, IEntityTypeConfiguration<BloqueoOperacion>
{
    private static Guid Id(string prefijo, int numero) => Guid.Parse($"{prefijo}-0000-4000-8000-{numero:000000000000}");

    public void Configure(EntityTypeBuilder<ConfiguracionOperativa> entidad)
    {
        entidad.HasKey(c => c.Id);
        entidad.Property(c => c.Version).IsConcurrencyToken();
        entidad.ToTable("ConfiguracionOperativa", tabla =>
        {
            tabla.HasCheckConstraint("CK_Operacion_Unica", "\"Id\" = 'e4ec8df0-0000-4000-8000-000000000001'::uuid");
            tabla.HasCheckConstraint("CK_Operacion_Tiempos", "\"DuracionActividadMinutos\" BETWEEN 1 AND 240 AND \"MargenMinutos\" BETWEEN 0 AND 60 AND \"ApartadoPagoMinutos\" BETWEEN 1 AND 60");
            tabla.HasCheckConstraint("CK_Operacion_Anticipacion", "\"AnticipacionMinimaMinutos\" BETWEEN 0 AND 10080 AND \"AnticipacionMaximaDias\" BETWEEN 1 AND 365 AND \"AnticipacionMinimaMinutos\" < \"AnticipacionMaximaDias\" * 1440");
            tabla.HasCheckConstraint("CK_Operacion_Llegadas", "\"LlegadaAnticipadaMinutos\" BETWEEN 0 AND 60 AND \"ToleranciaLlegadaMinutos\" BETWEEN 0 AND 60");
        });
        entidad.HasData(new ConfiguracionOperativa { Id = ConfiguracionOperativa.IdUnico, Version = Id("e4ec8df1", 1) });
    }

    public void Configure(EntityTypeBuilder<HorarioOperacion> entidad)
    {
        entidad.HasKey(h => h.Id);
        entidad.HasIndex(h => h.DiaSemana).IsUnique();
        entidad.ToTable("HorariosOperacion", tabla =>
        {
            tabla.HasCheckConstraint("CK_Horario_Dia", "\"DiaSemana\" BETWEEN 0 AND 6");
            tabla.HasCheckConstraint("CK_Horario_Rango", "\"Cerrado\" OR (\"Apertura\" < \"Cierre\" AND \"PrimerTurno\" >= \"Apertura\" AND \"UltimoTurno\" >= \"PrimerTurno\" AND \"UltimoTurno\" < \"Cierre\")");
        });
        entidad.HasData(Enumerable.Range(0, 7).Select(dia => new HorarioOperacion
        {
            Id = Id("e4ec8df2", dia + 1), DiaSemana = dia, Cerrado = dia == 1, Apertura = new(8, 0), Cierre = new(18, 0),
            PrimerTurno = new(11, 0), UltimoTurno = new(15, 30)
        }));
    }

    public void Configure(EntityTypeBuilder<LineaTiro> entidad)
    {
        entidad.HasKey(l => l.Id);
        entidad.Property(l => l.Nombre).HasMaxLength(80).IsRequired();
        entidad.HasIndex(l => l.Numero).IsUnique();
        entidad.Property(l => l.Version).IsConcurrencyToken();
        entidad.ToTable("LineasTiro", tabla =>
        {
            tabla.HasCheckConstraint("CK_Linea_Numero", "\"Numero\" BETWEEN 1 AND 15");
            tabla.HasCheckConstraint("CK_Linea_Capacidad", "\"Capacidad\" = 1");
            tabla.HasCheckConstraint("CK_Linea_Nombre", "length(trim(\"Nombre\")) > 0");
        });
        entidad.HasData(Enumerable.Range(1, 15).Select(numero => new LineaTiro
        {
            Id = Id("e4ec8df3", numero), Numero = numero, Nombre = $"Línea {numero}", Activa = true, Capacidad = 1,
            Version = Id("e4ec8df4", numero)
        }));
    }

    public void Configure(EntityTypeBuilder<BloqueoOperacion> entidad)
    {
        entidad.HasKey(b => b.Id);
        entidad.Property(b => b.Motivo).HasMaxLength(300).IsRequired();
        entidad.Property(b => b.Version).IsConcurrencyToken();
        entidad.HasOne<LineaTiro>().WithMany().HasForeignKey(b => b.LineaId).OnDelete(DeleteBehavior.Restrict);
        entidad.HasOne<PerfilUsuario>().WithMany().HasForeignKey(b => b.CreadoPorId).OnDelete(DeleteBehavior.Restrict);
        entidad.HasIndex(b => new { b.Activo, b.LineaId, b.InicioUtc, b.FinUtc });
        entidad.ToTable("BloqueosOperacion", tabla =>
        {
            tabla.HasCheckConstraint("CK_Bloqueo_Rango", "\"FinUtc\" > \"InicioUtc\"");
            tabla.HasCheckConstraint("CK_Bloqueo_Motivo", "length(trim(\"Motivo\")) > 0");
        });
    }
}
