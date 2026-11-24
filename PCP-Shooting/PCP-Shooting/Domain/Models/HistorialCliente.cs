using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Domain.Models;

public sealed class FiltroHistorialCliente
{
    [Range(1, int.MaxValue)] public int PaginaPedidos { get; init; } = 1;
    [Range(1, int.MaxValue)] public int PaginaPagos { get; init; } = 1;
    [Range(1, int.MaxValue)] public int PaginaMembresias { get; init; } = 1;
    [Range(1, int.MaxValue)] public int PaginaReservas { get; init; } = 1;
    [Range(1, 100)] public int TamanoPagina { get; init; } = 20;
}

public sealed record HistorialCliente(
    PerfilConsulta Perfil,
    PaginaConsulta<PedidoAdministrativo> Pedidos,
    PaginaConsulta<PagoAdministrativo> Pagos,
    PaginaConsulta<MembresiaHistorial> Membresias,
    PaginaConsulta<ReservaWebResumen> Reservas,
    string? Correo = null);
