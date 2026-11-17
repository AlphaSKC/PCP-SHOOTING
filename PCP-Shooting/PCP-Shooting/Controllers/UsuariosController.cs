using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController(IUsuariosService usuarios) : ControllerBase
{
    [HttpGet("yo")]
    public ActionResult<UsuarioActual> ConsultarActual()
    {
        var usuario = usuarios.ObtenerActual(User);
        return usuario is null ? Unauthorized() : Ok(usuario);
    }
}
