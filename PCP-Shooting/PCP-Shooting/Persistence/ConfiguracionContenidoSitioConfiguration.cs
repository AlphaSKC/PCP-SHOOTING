using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

public sealed class ConfiguracionContenidoSitioConfiguration : IEntityTypeConfiguration<ConfiguracionContenidoSitio>
{
    public void Configure(EntityTypeBuilder<ConfiguracionContenidoSitio> b)
    {
        b.ToTable("ConfiguracionContenidoSitio", t => {
            t.HasCheckConstraint("CK_ContenidoSitio_Unico", "\"Id\" = 'e4ec8df0-0000-4000-8000-000000000020'::uuid");
            t.HasCheckConstraint("CK_ContenidoSitio_Objeto", "jsonb_typeof(\"DatosJson\") = 'object'");
        });
        b.HasKey(c => c.Id); b.Property(c => c.Id).ValueGeneratedNever(); b.Property(c => c.DatosJson).HasColumnType("jsonb").IsRequired();
        b.Property(c => c.Version).IsConcurrencyToken();
        b.HasData(new ConfiguracionContenidoSitio { Id = ConfiguracionContenidoSitio.IdUnico,
            DatosJson = JsonSerializer.Serialize(new ContenidoSitioDatos(), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Version = Guid.Parse("e4ec8df0-0000-4000-8000-000000000021"), ActualizadoEnUtc = new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc) });
    }
}
