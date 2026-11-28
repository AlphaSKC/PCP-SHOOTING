using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class OrdenesAlimentosService(PcpShootingDbContext db,ITransaccionesPersistence tx,
    IOptions<OrdenesAlimentosOptions> opciones,TimeProvider reloj) : IOrdenesAlimentosService
{
    public Task<OrdenAlimentosConsulta> CrearAsync(Guid actor,CrearOrdenAlimentos datos,bool mostrador,CancellationToken ct) =>
        tx.EjecutarAsync(async ()=>
        {
            var perfil=await Autorizar(actor,mostrador,ct); Habilitada(); Validar(datos); Id(datos.IdOperacion);
            Exigir(datos.Partidas is {Count: >0 and <=50} && datos.Partidas.All(p=>p is not null && p.ProductoId!=Guid.Empty && p.Cantidad is >=1 and <=100),400,"Envía entre 1 y 50 partidas de 1 a 100 unidades.");
            Exigir(datos.Partidas.Select(p=>p.ProductoId).Distinct().Count()==datos.Partidas.Count,400,"Agrupa las cantidades por producto.");
            Exigir(mostrador || datos.ClienteId is null || datos.ClienteId==actor,403,"Solo puedes crear tus propias órdenes.");
            var clienteId=mostrador ? datos.ClienteId : actor;
            if(clienteId.HasValue) Id(clienteId.Value);
            var hash=Hash(new {mostrador,clienteId,nombre=datos.NombreRecogida?.Trim(),notas=datos.Notas?.Trim(),
                partidas=datos.Partidas.OrderBy(p=>p.ProductoId).ToArray()});
            var anterior=await Consulta().SingleOrDefaultAsync(o=>o.Id==datos.IdOperacion,ct);
            if(anterior is not null)
            {
                Exigir(anterior.CreadaPorId==actor && anterior.SolicitudHash==hash,409,"El IdOperacion corresponde a otra solicitud.");
                return Dto(anterior);
            }
            PerfilUsuario? cliente=clienteId==actor ? perfil : clienteId.HasValue
                ? await db.PerfilesUsuario.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==clienteId && p.Activo,ct) : null;
            Exigir(!clienteId.HasValue || cliente is not null,400,"Cliente inexistente o inactivo.");
            var nombre=cliente?.Nombre ?? datos.NombreRecogida?.Trim();
            Exigir(!string.IsNullOrWhiteSpace(nombre),400,"Indica el nombre de recogida para una orden de mostrador sin cuenta.");
            var ahora=Ahora();
            var orden=new OrdenAlimentos {Id=datos.IdOperacion,ClienteId=clienteId,CreadaPorId=actor,EsMostrador=mostrador,
                NombreRecogida=nombre!,Notas=datos.Notas?.Trim(),SolicitudHash=hash,Version=Guid.NewGuid(),CreadaEnUtc=ahora,ActualizadaEnUtc=ahora};
            foreach(var partida in datos.Partidas.OrderBy(p=>p.ProductoId))
            {
                var producto=await db.Productos.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==partida.ProductoId,ct);
                Exigir(producto is {Activo:true,EsVendible:true,Categoria:CategoriaProducto.Alimento,UsaTallas:false} &&
                    producto.Precio is >0 and <=9999999999.99m && decimal.Round(producto.Precio.Value,2)==producto.Precio,409,"Solo se pueden pedir alimentos disponibles con precio válido.");
                Exigir(await Stock(partida.ProductoId,ct)>=partida.Cantidad,409,"Existencias insuficientes para solicitar uno de los alimentos.");
                orden.Detalles.Add(new DetalleOrdenAlimentos {Id=Guid.NewGuid(),OrdenId=orden.Id,ProductoId=producto!.Id,
                    Sku=producto.Sku,Nombre=producto.Nombre,Cantidad=partida.Cantidad,PrecioUnitario=producto.Precio!.Value});
            }
            orden.Total=orden.Detalles.Sum(d=>d.Cantidad*d.PrecioUnitario);
            Exigir(orden.Total<=9999999999.99m,400,"El importe de la orden excede el máximo permitido.");
            db.Add(orden); return Dto(orden);
        },ct);

    public async Task<OrdenAlimentosConsulta> ConsultarAsync(Guid actor,Guid id,bool operativa,CancellationToken ct)
    {
        await Autorizar(actor,operativa,ct);
        var orden=await Consulta().AsNoTracking().SingleOrDefaultAsync(o=>o.Id==id && (operativa || o.ClienteId==actor),ct);
        return orden is null ? throw new OperacionException(404,"Orden inexistente.") : Dto(orden);
    }

    public async Task<PaginaConsulta<OrdenAlimentosConsulta>> ListarAsync(Guid actor,FiltroOrdenesAlimentos filtro,bool operativa,CancellationToken ct)
    {
        await Autorizar(actor,operativa,ct); Validar(filtro);
        var consulta=Consulta().AsNoTracking().Where(o=>operativa || o.ClienteId==actor);
        if(filtro.Estado.HasValue) consulta=consulta.Where(o=>o.Estado==filtro.Estado);
        if(filtro.Pagada.HasValue) consulta=consulta.Where(o=>(o.PagoConfirmadoEnUtc!=null)==filtro.Pagada);
        if(filtro.DesdeUtc.HasValue) consulta=consulta.Where(o=>o.CreadaEnUtc>=filtro.DesdeUtc.Value.UtcDateTime);
        if(filtro.HastaUtc.HasValue) consulta=consulta.Where(o=>o.CreadaEnUtc<filtro.HastaUtc.Value.UtcDateTime);
        await using var lectura=db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,ct) : null;
        var pagina=await PaginacionConsulta.CrearAsync(consulta.OrderByDescending(o=>o.CreadaEnUtc).ThenBy(o=>o.Id),filtro,ct);
        return new(pagina.Elementos.Select(Dto).ToArray(),pagina.Total,pagina.Pagina,pagina.TamanoPagina);
    }

    public Task<OrdenAlimentosConsulta> CambiarEstadoAsync(Guid actor,Guid id,CambiarEstadoOrdenAlimentos datos,CancellationToken ct)
    {
        Exigir(datos.Estado is EstadoOrdenAlimentos.EnPreparacion or EstadoOrdenAlimentos.Listo,400,"Usa EnPreparacion o Listo; la entrega tiene su propia operación.");
        return Mutar(actor,id,datos,datos.Estado.ToString(),Hash(new{datos.Version,datos.Estado}),async orden=>
        {
            Exigir((int)datos.Estado==(int)orden.Estado+1,409,"El estado debe avanzar de Recibido a EnPreparacion y después a Listo.");
            orden.Estado=datos.Estado;
            if(datos.Estado==EstadoOrdenAlimentos.EnPreparacion) orden.EnPreparacionEnUtc=Ahora();
            else orden.ListaEnUtc=Ahora();
            await Task.CompletedTask;
        },ct);
    }

    public Task<OrdenAlimentosConsulta> CobrarAsync(Guid actor,Guid id,RegistrarCobroAlimentos datos,CancellationToken ct)
    {
        Validar(datos);
        Exigir(datos.Metodo.HasValue && Enum.IsDefined(datos.Metodo.Value) && !string.IsNullOrWhiteSpace(datos.Referencia),400,"Indica tarjeta o transferencia y una referencia de cobro.");
        return Mutar(actor,id,datos,"Cobro",Hash(new{datos.Version,datos.Metodo,referencia=datos.Referencia.Trim(),datos.Importe}),async orden=>
        {
            Exigir(orden.Estado==EstadoOrdenAlimentos.Listo && orden.PagoConfirmadoEnUtc is null,409,"El cobro local se registra cuando está Listo y no tiene pago confirmado.");
            Exigir(datos.Importe==orden.Total,400,"El cobro debe cubrir exactamente el total de la orden.");
            orden.MetodoCobro=datos.Metodo; orden.ReferenciaCobro=datos.Referencia.Trim();
            orden.ImporteConfirmado=orden.Total; orden.PagoConfirmadoEnUtc=Ahora();
            await Task.CompletedTask;
        },ct);
    }

    public Task<OrdenAlimentosConsulta> EntregarAsync(Guid actor,Guid id,MutacionOrdenAlimentos datos,CancellationToken ct) =>
        Mutar(actor,id,datos,"Entrega",Hash(new{datos.Version}),async orden=>
        {
            Exigir(orden.Estado==EstadoOrdenAlimentos.Listo && orden.PagoConfirmadoEnUtc.HasValue && orden.ImporteConfirmado==orden.Total,
                409,"Para entregar, la orden debe estar Lista y tener el pago completo confirmado.");
            foreach(var partida in orden.Detalles)
                Exigir(await Stock(partida.ProductoId,ct)>=partida.Cantidad,409,"Existencias insuficientes al entregar. No se descontó ningún producto; requiere revisión de Recepción.");
            var ahora=Ahora();
            foreach(var partida in orden.Detalles)
                db.MovimientosInventario.Add(new MovimientoInventario {Id=Guid.NewGuid(),ProductoId=partida.ProductoId,
                    DetalleOrdenAlimentosId=partida.Id,Tipo=TipoMovimientoInventario.Salida,VariacionCantidad=-partida.Cantidad,
                    RegistradoEnUtc=ahora,Motivo=$"Entrega de orden de alimentos {orden.Id}"});
            orden.Estado=EstadoOrdenAlimentos.Entregado; orden.EntregadaEnUtc=ahora;
        },ct);

    private async Task<OrdenAlimentosConsulta> Mutar(Guid actor,Guid id,MutacionOrdenAlimentos datos,string tipo,string hash,
        Func<OrdenAlimentos,Task> aplicar,CancellationToken ct)
    {
        try
        {
            return await tx.EjecutarAsync(async ()=>
            {
                await Autorizar(actor,true,ct); Habilitada(); Id(datos.IdOperacion); Id(datos.Version);
                var anterior=await db.Set<OperacionOrdenAlimentos>().AsNoTracking().SingleOrDefaultAsync(o=>o.Id==datos.IdOperacion,ct);
                if(anterior is not null)
                    Exigir(anterior.OrdenId==id && anterior.ActorId==actor && anterior.Tipo==tipo && anterior.SolicitudHash==hash,409,"El IdOperacion corresponde a otra operación.");
                var orden=await Consulta().SingleOrDefaultAsync(o=>o.Id==id,ct) ?? throw new OperacionException(404,"Orden inexistente.");
                if(anterior is not null) return Dto(orden);
                Exigir(orden.Version==datos.Version,409,"La orden cambió; recarga antes de continuar.");
                await aplicar(orden);
                orden.Version=Guid.NewGuid(); orden.ActualizadaEnUtc=Ahora();
                db.Add(new OperacionOrdenAlimentos {Id=datos.IdOperacion,OrdenId=id,ActorId=actor,Tipo=tipo,SolicitudHash=hash,CreadaEnUtc=Ahora()});
                return Dto(orden);
            },ct);
        }
        catch(DbUpdateConcurrencyException) { throw new OperacionException(409,"La orden cambió; recarga antes de continuar."); }
    }

    private IQueryable<OrdenAlimentos> Consulta()=>db.Set<OrdenAlimentos>().Include(o=>o.Detalles);
    private async Task<long> Stock(Guid id,CancellationToken ct)=>
        await db.MovimientosInventario.Where(m=>m.ProductoId==id).SumAsync(m=>(long?)m.VariacionCantidad,ct)??0;
    private async Task<PerfilUsuario> Autorizar(Guid id,bool personal,CancellationToken ct)
    {
        var perfil=await db.PerfilesUsuario.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==id && p.Activo,ct);
        Exigir(perfil is not null && (!personal || perfil.Rol is RolUsuario.Administrador or RolUsuario.Recepcion),403,"No tienes permiso para esta operación."); return perfil!;
    }
    private void Habilitada()=>Exigir(opciones.Value.OrdenesHabilitadas,503,"Las órdenes de alimentos todavía no están habilitadas para operación.");
    private DateTime Ahora()=>reloj.GetUtcNow().UtcDateTime;
    private static void Id(Guid id)=>Exigir(id!=Guid.Empty,400,"Se requiere un UUID válido para la operación y la versión.");
    private static void Validar(object datos)=>Validator.ValidateObject(datos,new ValidationContext(datos),true);
    private static void Exigir(bool cumple,int estado,string mensaje) { if(!cumple) throw new OperacionException(estado,mensaje); }
    private static string Hash(object datos)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(datos)))).ToLowerInvariant();
    private static OrdenAlimentosConsulta Dto(OrdenAlimentos o)=>new(o.Id,o.ClienteId,o.EsMostrador,o.NombreRecogida,o.Notas,o.Estado.ToString(),o.Total,
        o.PagoConfirmadoEnUtc.HasValue?"Confirmado":"Pendiente",o.ImporteConfirmado,o.MetodoCobro,o.ReferenciaCobro,o.CreadaEnUtc,o.ActualizadaEnUtc,
        o.EnPreparacionEnUtc,o.ListaEnUtc,o.PagoConfirmadoEnUtc,o.EntregadaEnUtc,o.Version,
        o.Detalles.OrderBy(d=>d.Sku).ThenBy(d=>d.Id).Select(d=>new PartidaOrdenAlimentosConsulta(d.Id,d.ProductoId,d.Sku,d.Nombre,d.Cantidad,d.PrecioUnitario)).ToArray());
}
