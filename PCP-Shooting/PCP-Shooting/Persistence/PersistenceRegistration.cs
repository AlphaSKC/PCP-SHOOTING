using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Services;

namespace PCP_Shooting.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDatabase)
    {
        services.AddDbContext<PcpShootingDbContext>(configureDatabase);
        services.AddScoped<IProductosPersistence, ProductosPersistence>();
        services.AddScoped<IPedidosPersistence, PedidosPersistence>();
        services.AddScoped<IPagosPersistence, PagosPersistence>();
        services.AddScoped<IMembresiasPersistence, MembresiasPersistence>();
        services.AddScoped<IInventarioPersistence, InventarioPersistence>();
        return services;
    }
}
