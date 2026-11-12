using EOP.Gestion.Core.DTOs.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;

namespace EOP.Gestion.Core.CasosDeUso.Usuarios;

// US-046 / RF-75: el Administrador activa o inactiva a un usuario registrado en EOP.
// Inactivar NO elimina nada: solo cambia la columna Activo, así el usuario conserva su
// registro y todas sus relaciones (proyectos, tareas, etc.) y se lo puede reactivar después.
//
// Que el que pide esto sea Administrador lo controla el Controller con [Authorize], y que
// el Administrador haya confirmado la inactivación es parte de la pantalla (el diálogo
// de confirmación). Acá van las reglas del negocio.
//
// Alcance de S2-T07: el cambio de estado y la protección del último Administrador activo.
// S2-T08 va a agregar acá la restricción de los responsables de proyectos (RF-75) y
// S2-T09 la integración completa con el control de acceso.
public class CambiarEstadoUsuario
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;

    public CambiarEstadoUsuario(IRepositorioUsuarios repositorioUsuarios)
    {
        _repositorioUsuarios = repositorioUsuarios;
    }

    public async Task<ResultadoCambiarEstadoUsuario> EjecutarAsync(int usuarioId, bool nuevoEstadoActivo, CancellationToken cancellationToken = default)
    {
        // Inactivar a un Administrador activo puede dejar al sistema sin administradores,
        // igual que degradarlo de rol (US-044). Por eso pedimos el MISMO turno que el cambio
        // de rol, ANTES de leer ningún dato: si no, dos solicitudes simultáneas contarían 2
        // administradores, inactivarían cada una a uno distinto y quedaría ninguno.
        //
        // El "await using" libera el turno al salir del método, por cualquier camino.
        // Si salimos sin llegar a ConfirmarAsync (un rechazo o una excepción), no se guarda nada.
        await using IBloqueoAdministradores bloqueo = await _repositorioUsuarios.BloquearAdministradoresAsync(cancellationToken);

        Usuario? usuarioEncontrado = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);

        if (usuarioEncontrado == null)
        {
            return ResultadoCambiarEstadoUsuario.UsuarioNoEncontrado;
        }

        if (usuarioEncontrado.Activo == nuevoEstadoActivo)
        {
            return ResultadoCambiarEstadoUsuario.YaTeniaEseEstado;
        }

        // RF-75: "no podrá cambiarse a Inactivo al último Administrador activo".
        if (!nuevoEstadoActivo)
        {
            bool dejariaSinAdministradores = await DejariaAlSistemaSinAdministradoresActivosAsync(usuarioEncontrado, cancellationToken);

            if (dejariaSinAdministradores)
            {
                return ResultadoCambiarEstadoUsuario.UltimoAdministradorActivo;
            }
        }

        await _repositorioUsuarios.ActualizarEstadoAsync(usuarioId, nuevoEstadoActivo, cancellationToken);

        await bloqueo.ConfirmarAsync(cancellationToken);

        if (nuevoEstadoActivo)
        {
            return ResultadoCambiarEstadoUsuario.UsuarioReactivado;
        }

        return ResultadoCambiarEstadoUsuario.UsuarioInactivado;
    }

    // Solo inactivar a un Administrador que hoy está activo baja la cantidad de administradores
    // activos. Para cualquier otro usuario, la cuenta no cambia.
    private async Task<bool> DejariaAlSistemaSinAdministradoresActivosAsync(Usuario usuarioAInactivar, CancellationToken cancellationToken)
    {
        if (usuarioAInactivar.Rol != RolUsuario.Administrador)
        {
            return false;
        }

        int cantidadAdministradoresActivos = await _repositorioUsuarios.ContarAdministradoresActivosAsync(cancellationToken);

        // La cuenta lo incluye a él: si da 1, es el único que queda.
        if (cantidadAdministradoresActivos <= 1)
        {
            return true;
        }

        return false;
    }
}
