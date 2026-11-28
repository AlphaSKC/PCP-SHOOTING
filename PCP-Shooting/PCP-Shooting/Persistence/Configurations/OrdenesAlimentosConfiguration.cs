using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Configurations;

public sealed class OrdenesAlimentosConfiguration : IEntityTypeConfiguration<OrdenAlimentos>,
    IEntityTypeConfiguration<DetalleOrdenAlimentos>,IEntityTypeConfiguration<OperacionOrdenAlimentos>
{
    public void Configure(EntityTypeBuilder<OrdenAlimentos> b)
    {
        b.ToTable("OrdenesAlimentos",t=>
        {
            t.HasCheckConstraint("CK_OrdenAlimentos_Estado","\"Estado\" IN (0,1,2,3)");
            t.HasCheckConstraint("CK_OrdenAlimentos_Total","\"Total\" > 0 AND \"ImporteConfirmado\" >= 0 AND \"ImporteConfirmado\" <= \"Total\"");
            t.HasCheckConstraint("CK_OrdenAlimentos_Pago","(\"PagoConfirmadoEnUtc\" IS NULL AND \"ImporteConfirmado\"=0 AND \"MetodoCobro\" IS NULL AND \"ReferenciaCobro\" IS NULL) OR (\"PagoConfirmadoEnUtc\" IS NOT NULL AND \"ImporteConfirmado\"=\"Total\" AND \"MetodoCobro\" IN (0,1) AND \"MetodoCobro\" IS NOT NULL AND length(trim(\"ReferenciaCobro\"))>0 AND \"ReferenciaCobro\" IS NOT NULL)");
            t.HasCheckConstraint("CK_OrdenAlimentos_Entrega","(\"Estado\"=3 AND \"EntregadaEnUtc\" IS NOT NULL AND \"PagoConfirmadoEnUtc\" IS NOT NULL) OR (\"Estado\"<>3 AND \"EntregadaEnUtc\" IS NULL)");
            t.HasCheckConstraint("CK_OrdenAlimentos_Cliente","\"EsMostrador\" OR \"ClienteId\" IS NOT NULL");
            t.HasCheckConstraint("CK_OrdenAlimentos_Nombre","length(trim(\"NombreRecogida\"))>0");
        });
        b.Property(o=>o.NombreRecogida).HasMaxLength(150); b.Property(o=>o.Notas).HasMaxLength(500);
        b.Property(o=>o.SolicitudHash).HasMaxLength(64); b.Property(o=>o.ReferenciaCobro).HasMaxLength(150);
        b.Property(o=>o.Total).HasPrecision(12,2); b.Property(o=>o.ImporteConfirmado).HasPrecision(12,2);
        b.Property(o=>o.Version).IsConcurrencyToken();
        b.HasOne<PerfilUsuario>().WithMany().HasForeignKey(o=>o.ClienteId);
        b.HasOne<PerfilUsuario>().WithMany().HasForeignKey(o=>o.CreadaPorId);
        b.HasMany(o=>o.Detalles).WithOne().HasForeignKey(d=>d.OrdenId);
        b.HasIndex(o=>new{o.ClienteId,o.CreadaEnUtc}); b.HasIndex(o=>new{o.Estado,o.CreadaEnUtc});
    }
    public void Configure(EntityTypeBuilder<DetalleOrdenAlimentos> b)
    {
        b.ToTable("DetallesOrdenAlimentos",t=>
        {
            t.HasCheckConstraint("CK_DetalleAlimentos_Cantidad","\"Cantidad\" BETWEEN 1 AND 100");
            t.HasCheckConstraint("CK_DetalleAlimentos_Precio","\"PrecioUnitario\">0");
        });
        b.Property(d=>d.Sku).HasMaxLength(80); b.Property(d=>d.Nombre).HasMaxLength(200);
        b.Property(d=>d.PrecioUnitario).HasPrecision(12,2);
        b.HasOne<Producto>().WithMany().HasForeignKey(d=>d.ProductoId);
        b.HasIndex(d=>new{d.OrdenId,d.ProductoId}).IsUnique();
    }
    public void Configure(EntityTypeBuilder<OperacionOrdenAlimentos> b)
    {
        b.ToTable("OperacionesOrdenAlimentos",t=>t.HasCheckConstraint("CK_OperacionAlimentos_Tipo","\"Tipo\" IN ('EnPreparacion','Listo','Cobro','Entrega')"));
        b.Property(o=>o.Tipo).HasMaxLength(20); b.Property(o=>o.SolicitudHash).HasMaxLength(64);
        b.HasOne<OrdenAlimentos>().WithMany().HasForeignKey(o=>o.OrdenId);
        b.HasOne<PerfilUsuario>().WithMany().HasForeignKey(o=>o.ActorId);
        b.HasIndex(o=>new{o.OrdenId,o.CreadaEnUtc});
    }
}
