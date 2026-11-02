using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EOP.Gestion.Tests.Compartido;

// Controller que existe SOLO en el proyecto de pruebas (T13). Todavía ninguna
// pantalla real de EOP usa [Authorize(Roles = ...)], porque eso llega con cada
// módulo. Para probar el atributo de verdad, pasando por toda la app, necesitamos
// alguna acción protegida con él, y no vamos a inventar una funcionalidad para eso.
//
// La app real no lo conoce: solo lo suma la fábrica de las pruebas de integración
// con AddApplicationPart. Tiene que ser público porque MVC ignora los Controllers
// que no lo son.
public sealed class AccionesDePruebaPorRolController : Controller
{
    // Si este texto aparece en la respuesta, la acción se ejecutó.
    public const string TextoDeAccionEjecutada = "Accion protegida ejecutada";

    [Authorize(Roles = "Administrador")]
    public IActionResult SoloAdministrador() => Content(TextoDeAccionEjecutada);

    [Authorize(Roles = "Supervisor")]
    public IActionResult SoloSupervisor() => Content(TextoDeAccionEjecutada);

    [Authorize(Roles = "Operador")]
    public IActionResult SoloOperador() => Content(TextoDeAccionEjecutada);

    // Con la coma alcanza con tener UNO de los roles de la lista.
    [Authorize(Roles = "Administrador,Supervisor")]
    public IActionResult AdministradorOSupervisor() => Content(TextoDeAccionEjecutada);
}
