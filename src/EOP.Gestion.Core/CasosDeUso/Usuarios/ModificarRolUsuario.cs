using EOP.Gestion.Core.DTOs.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;

namespace EOP.Gestion.Core.CasosDeUso.Usuarios;

// US-044 / RF-72: el Administrador le cambia el rol a un usuario registrado en EOP.
// Solo toca la tabla Usuarios de nuestra base: los roles e identidades de Microsoft
// Entra ID no se modifican. Que el que pide esto sea Administrador lo controla el
// Controller con [Authorize]; acá van las reglas del negocio.
public class ModificarRolUsuario
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;

    public ModificarRolUsuario(IRepositorioUsuarios repositorioUsuarios)
    {
        _repositorioUsuarios = repositorioUsuarios;
    }

    public async Task<ResultadoModificarRol> EjecutarAsync(int usuarioId, RolUsuario nuevoRol, CancellationToken cancellationToken = default)
    {
        // Un enum de C# acepta cualquier número aunque no tenga nombre: (RolUsuario)99
        // compila sin problemas. Por eso comprobamos que sea uno de los tres roles fijos.
        if (!Enum.IsDefined(nuevoRol))
        {
            return ResultadoModificarRol.RolNoPermitido;
        }

        // Esperamos nuestro turno ANTES de leer cualquier dato. Si leyéramos primero,
        // podríamos decidir con información que otra solicitud cambia un instante después.
        //
        // El "await using" libera el turno al salir del método, por cualquier camino.
        // Si salimos sin llegar a ConfirmarAsync (un rechazo o una excepción), no se guarda nada.
        await using IBloqueoAdministradores bloqueo = await _repositorioUsuarios.BloquearAdministradoresAsync(cancellationToken);

        Usuario? usuarioEncontrado = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);

        if (usuarioEncontrado == null)
        {
            return ResultadoModificarRol.UsuarioNoEncontrado;
        }

        bool dejariaSinAdministradores = await DejariaAlSistemaSinAdministradoresActivosAsync(usuarioEncontrado, nuevoRol, cancellationToken);

        if (dejariaSinAdministradores)
        {
            return ResultadoModificarRol.UltimoAdministradorActivo;
        }

        await _repositorioUsuarios.ActualizarRolAsync(usuarioId, nuevoRol, cancellationToken);

        await bloqueo.ConfirmarAsync(cancellationToken);

        return ResultadoModificarRol.RolModificado;
    }

    // RF-72: "No podrá modificarse el rol del último Administrador activo si con ello
    // no queda ningún Administrador activo".
    private async Task<bool> DejariaAlSistemaSinAdministradoresActivosAsync(Usuario usuario, RolUsuario nuevoRol, CancellationToken cancellationToken)
    {
        // Solo un cambio que le saca el rol a un Administrador puede bajar la cantidad.
        if (nuevoRol == RolUsuario.Administrador)
        {
            return false;
        }

        // Si el usuario no era Administrador, o lo era pero estaba inactivo,
        // no estaba sumando a la cuenta: cambiarle el rol no la modifica.
        bool esAdministradorActivo = usuario.Rol == RolUsuario.Administrador && usuario.Activo;

        if (!esAdministradorActivo)
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
