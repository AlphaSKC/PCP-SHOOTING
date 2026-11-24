using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class PerfilesService(IPerfilesPersistence perfiles) : IPerfilesService
{
    public async Task<PerfilConsulta?> ConsultarAsync(Guid id, CancellationToken cancellationToken)
    {
        var perfil = await perfiles.ConsultarAsync(id, cancellationToken);
        return perfil is null ? null : Convertir(perfil);
    }

    public Task<PaginaPerfiles> ListarAsync(FiltroPerfiles filtro, CancellationToken cancellationToken)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), validateAllProperties: true);
        return perfiles.ListarAsync(filtro, cancellationToken);
    }

    public async Task<PerfilConsulta> GuardarPropioAsync(Guid id, EditarPerfil datos, CancellationToken cancellationToken)
    {
        Validator.ValidateValue(datos.Nombre, new ValidationContext(datos),
            new ValidationAttribute[] { new RequiredAttribute(), new StringLengthAttribute(150) });
        Validator.ValidateValue(datos.Telefono, new ValidationContext(datos),
            new ValidationAttribute[] { new StringLengthAttribute(30), new PhoneAttribute() });
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            throw new ValidationException("El nombre es obligatorio.");
        await perfiles.GuardarPropioAsync(id, datos.Nombre.Trim(), datos.Telefono?.Trim(), cancellationToken);
        return (await ConsultarAsync(id, cancellationToken))!;
    }

    public async Task<PerfilConsulta?> CambiarAccesoAsync(Guid id, CambiarAcceso datos, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(datos.Rol))
            throw new ValidationException("El rol indicado no es válido.");
        var perfil = await perfiles.CambiarAccesoAsync(id, datos, cancellationToken);
        return perfil is null ? null : Convertir(perfil);
    }

    private static PerfilConsulta Convertir(PerfilUsuario perfil) =>
        new(perfil.Id, perfil.Nombre, perfil.Telefono, perfil.Rol, perfil.Activo);
}