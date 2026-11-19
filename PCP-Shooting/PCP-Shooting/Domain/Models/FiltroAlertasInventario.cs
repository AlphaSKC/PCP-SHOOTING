using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Models;

public class FiltroAlertasInventario : FiltroPagina
{
    [EnumDataType(typeof(CategoriaProducto))]
    public CategoriaProducto? Categoria { get; set; }
}