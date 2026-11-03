using System.ComponentModel.DataAnnotations;

namespace EOP.Gestion.Core.Comun;

// Las dos únicas monedas del sistema (RF-23). Va en Comun y no en Proveedores porque
// también las van a usar el catálogo de materiales, los presupuestos y las facturas.
//
// Arranca en 1 a propósito: un enum sin asignar vale 0, y como 0 no es ninguna moneda,
// la base rechaza una cuenta a la que se olvidaron de ponerle moneda. Si arrancara en 0,
// esa cuenta se guardaría en silencio como si fuera en pesos.
//
// Estos números son lo que queda guardado en la base, así que no hay que cambiarlos.
public enum Moneda
{
    [Display(Name = "UYU — Pesos uruguayos")]
    UYU = 1,

    [Display(Name = "USD — Dólares estadounidenses")]
    USD = 2
}
