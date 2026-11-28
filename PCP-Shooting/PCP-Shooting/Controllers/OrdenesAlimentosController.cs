using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController,Route("api/ordenes-alimentos"),Authorize(Policy="PerfilActivo")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class OrdenesAlimentosController(IOrdenesAlimentosService servicio,IUsuariosService usuarios) : ControllerBase
{
    private Guid Actor=>usuarios.ObtenerActual(User)?.Id ?? throw new OperacionException(401,"Inicia sesión.");
    [HttpPost] public async Task<ActionResult<OrdenAlimentosConsulta>> Crear(CrearOrdenAlimentos datos,CancellationToken ct)=>Ok(await servicio.CrearAsync(Actor,datos,false,ct));
    [HttpGet] public async Task<ActionResult<PaginaConsulta<OrdenAlimentosConsulta>>> Listar([FromQuery] FiltroOrdenesAlimentos filtro,CancellationToken ct)=>Ok(await servicio.ListarAsync(Actor,filtro,false,ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<OrdenAlimentosConsulta>> Consultar(Guid id,CancellationToken ct)=>Ok(await servicio.ConsultarAsync(Actor,id,false,ct));
}

[ApiController,Route("api/administracion/ordenes-alimentos"),Authorize(Policy="OperacionPedidos")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class OperacionAlimentosController(IOrdenesAlimentosService servicio,IUsuariosService usuarios) : ControllerBase
{
    private Guid Actor=>usuarios.ObtenerActual(User)?.Id ?? throw new OperacionException(401,"Inicia sesión.");
    [HttpPost] public async Task<ActionResult<OrdenAlimentosConsulta>> Crear(CrearOrdenAlimentos datos,CancellationToken ct)=>Ok(await servicio.CrearAsync(Actor,datos,true,ct));
    [HttpGet] public async Task<ActionResult<PaginaConsulta<OrdenAlimentosConsulta>>> Listar([FromQuery] FiltroOrdenesAlimentos filtro,CancellationToken ct)=>Ok(await servicio.ListarAsync(Actor,filtro,true,ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<OrdenAlimentosConsulta>> Consultar(Guid id,CancellationToken ct)=>Ok(await servicio.ConsultarAsync(Actor,id,true,ct));
    [HttpPost("{id:guid}/estado")] public async Task<ActionResult<OrdenAlimentosConsulta>> Estado(Guid id,CambiarEstadoOrdenAlimentos datos,CancellationToken ct)=>Ok(await servicio.CambiarEstadoAsync(Actor,id,datos,ct));
    [HttpPost("{id:guid}/cobro")] public async Task<ActionResult<OrdenAlimentosConsulta>> Cobro(Guid id,RegistrarCobroAlimentos datos,CancellationToken ct)=>Ok(await servicio.CobrarAsync(Actor,id,datos,ct));
    [HttpPost("{id:guid}/entrega")] public async Task<ActionResult<OrdenAlimentosConsulta>> Entrega(Guid id,MutacionOrdenAlimentos datos,CancellationToken ct)=>Ok(await servicio.EntregarAsync(Actor,id,datos,ct));
}
