using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class ConfiguracionAvisosConfiguration : IEntityTypeConfiguration<ConfiguracionAvisos>
{
    public void Configure(EntityTypeBuilder<ConfiguracionAvisos> e)
    {
        e.HasKey(c => c.Id);
        e.Property(c => c.Id).ValueGeneratedNever();
        e.Property(c => c.NotifyEmail).HasMaxLength(254);
        e.Property(c => c.WaOrderReady).HasMaxLength(4000).IsRequired();
        e.Property(c => c.WaReminder).HasMaxLength(4000).IsRequired();
        e.Property(c => c.Version).IsConcurrencyToken();
        e.ToTable("ConfiguracionAvisos", t =>
        {
            t.HasCheckConstraint("CK_Avisos_Unica", "\"Id\" = 'e4ec8df0-0000-4000-8000-000000000010'::uuid");
            t.HasCheckConstraint("CK_Avisos_Correo", "\"NotifyEmail\" IS NULL OR length(trim(\"NotifyEmail\")) > 0");
        });
        e.HasData(new ConfiguracionAvisos { Id = ConfiguracionAvisos.IdUnico,
            Version = Guid.Parse("e4ec8df0-0000-4000-8000-000000000011"),
            ActualizadoEnUtc = new DateTime(2026,11,23,0,0,0,DateTimeKind.Utc) });
    }
}
