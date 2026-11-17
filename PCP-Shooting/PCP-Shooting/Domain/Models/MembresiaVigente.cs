namespace PCP_Shooting.Domain.Models;

public sealed record MembresiaVigente(Guid Id, DateOnly InicioVigencia, DateOnly FinVigenciaExclusivo);
