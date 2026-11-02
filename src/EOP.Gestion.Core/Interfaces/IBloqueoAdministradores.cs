namespace EOP.Gestion.Core.Interfaces;

// Representa el "turno" que se pide con IRepositorioUsuarios.BloquearAdministradoresAsync.
// Mientras alguien lo tiene, ninguna otra solicitud puede pedir el suyo: tiene que esperar.
//
// Lo necesitamos para que "contar los administradores y después guardar el cambio"
// sea un solo paso indivisible. Si no, dos solicitudes simultáneas podrían contar
// 2 administradores, degradar cada una a uno distinto y dejar el sistema sin ninguno.
//
// Core no sabe cómo se implementa: esa parte es de Infrastructure.
//
// IMPORTANTE para las próximas tareas: toda operación que pueda dejar a alguien
// sin ser Administrador activo (por ejemplo, inactivar un usuario en US-046) tiene
// que pedir este mismo turno antes de contar. Si no lo pide, la protección no la cubre.
//
// IAsyncDisposable es lo que permite usarlo con "await using": al terminar el bloque
// el turno se libera solo. Si no se llamó a ConfirmarAsync, los cambios se descartan.
public interface IBloqueoAdministradores : IAsyncDisposable
{
    // Guarda definitivamente los cambios hechos durante el turno y lo libera.
    Task ConfirmarAsync(CancellationToken cancellationToken = default);
}
