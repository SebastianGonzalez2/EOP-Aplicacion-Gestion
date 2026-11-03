using System.ComponentModel.DataAnnotations;

namespace EOP.Gestion.Core.Entidades.Proveedores;

// La lista fija de bancos de RF-23: no se escribe a mano ni se administra desde la aplicación.
// En Display queda el nombre tal como lo pide el requerimiento, que es el que se le va a
// mostrar al usuario.
//
// Igual que en Moneda, arranca en 1 para que una cuenta sin banco (que valdría 0) sea
// rechazada por la base, y los números no se cambian porque son lo que queda guardado.
// Si algún día se agrega un banco, también hay que sumarlo en CuentaBancariaConfiguracion.
public enum Banco
{
    [Display(Name = "Banco de la República Oriental del Uruguay (BROU)")]
    BancoRepublicaOrientalDelUruguay = 1,

    [Display(Name = "Banco Hipotecario del Uruguay (BHU)")]
    BancoHipotecarioDelUruguay = 2,

    [Display(Name = "Banco Bandes Uruguay S.A.")]
    BancoBandesUruguay = 3,

    [Display(Name = "Banco Bilbao Vizcaya Argentaria Uruguay S.A. (BBVA)")]
    BancoBilbaoVizcayaArgentariaUruguay = 4,

    [Display(Name = "Banco BTG Pactual Uruguay S.A.")]
    BancoBtgPactualUruguay = 5,

    [Display(Name = "Banco de la Nación Argentina — Sucursal Uruguay")]
    BancoDeLaNacionArgentina = 6,

    [Display(Name = "Banco Itaú Uruguay S.A.")]
    BancoItauUruguay = 7,

    [Display(Name = "Banco Santander S.A.")]
    BancoSantander = 8,

    [Display(Name = "Banque Heritage (Uruguay) S.A.")]
    BanqueHeritageUruguay = 9,

    [Display(Name = "Citibank N.A. — Sucursal Uruguay")]
    CitibankSucursalUruguay = 10,

    [Display(Name = "Scotiabank Uruguay S.A.")]
    ScotiabankUruguay = 11
}
