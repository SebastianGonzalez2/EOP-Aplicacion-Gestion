using EOP.Gestion.Core.DTOs.Presupuestos;

namespace EOP.Gestion.WebApp.ViewModels.Cuenta;

// S3-T15 (US-003, US-042): datos de la advertencia que aparece al cerrar sesión con cambios sin
// guardar. La misma información se muestra de dos formas:
// - EnModal = true: como diálogo de Bootstrap, desde cualquier pantalla (lo arma el componente
//   AdvertenciaCierreSesion del _Layout).
// - EnModal = false: como pantalla común (/Cuenta/CambiosPendientes). Es la que ve el usuario si
//   el navegador no ejecutó JavaScript, si la lista cambió o si algo no se pudo guardar.
public class CambiosPendientesCierreSesionViewModel
{
    public List<CambioPendienteDto> Pendientes { get; init; } = [];

    public bool EnModal { get; init; }

    // Solo en la pantalla: muestra directamente el paso de confirmar el descarte.
    public bool ConfirmandoDescarte { get; init; }

    // Cómo terminó cada presupuesto en el último intento de guardar o descartar.
    public List<ResultadoPresupuestoAlCerrarSesion> Resultados { get; init; } = [];
}

// Lo que el formulario de la advertencia manda de cada borrador: la "foto" que vio el usuario.
// El servidor la compara con la sesión antes de guardar o descartar.
public class BorradorEsperadoViewModel
{
    public int PresupuestoId { get; set; }

    public string? BorradorId { get; set; }

    public int Version { get; set; }
}
