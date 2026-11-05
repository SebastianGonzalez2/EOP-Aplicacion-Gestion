using EOP.Gestion.WebApp.ViewModels.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EOP.Gestion.Tests.Compartido;

// Controller que existe SOLO en el proyecto de pruebas, igual que AccionesDePruebaPorRolController.
// Sirve para comprobar que el formulario está bien armado para el model binding, mirando qué
// entendió ASP.NET Core sin mezclarlo con las validaciones de Core ni con la base. Esta acción
// recibe el ViewModel real, pasa por el binding real y devuelve lo que entendió. No guarda nada:
// el POST real de proveedores se prueba en FormularioRegistrarProveedorTests.Post.cs.
public sealed class RecepcionDePruebaProveedoresController : Controller
{
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Recibir(RegistrarProveedorViewModel modelo)
    {
        var errores = new List<string>();

        foreach (ModelStateEntry campo in ModelState.Values)
        {
            foreach (ModelError error in campo.Errors)
            {
                errores.Add(error.ErrorMessage);
            }
        }

        return Json(new DatosRecibidosPorLaPrueba(modelo, errores));
    }
}

public sealed record DatosRecibidosPorLaPrueba(RegistrarProveedorViewModel Modelo, List<string> Errores);
