using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;

namespace EOP.Gestion.Tests.Compartido;

// Repositorio en memoria para probar el cambio de rol (T14) sin base de datos.
// Imita lo importante de una transacción: el rol nuevo recién se aplica al confirmar,
// y si el turno se libera sin confirmar, el cambio se pierde.
//
// También anota cada paso que le piden, para poder comprobar el ORDEN: por ejemplo,
// que el caso de uso espere su turno antes de leer los datos.
//
// También lo usan las pruebas del alta de usuarios (S1-T17).
internal sealed class RepositorioUsuariosConTurnoEnMemoria : IRepositorioUsuarios
{
    public const string PasoBloquear = "Bloquear";
    public const string PasoObtener = "Obtener";
    public const string PasoContar = "Contar";
    public const string PasoActualizar = "Actualizar";
    public const string PasoConfirmar = "Confirmar";
    public const string PasoLiberar = "Liberar";
    public const string PasoObtenerPorCorreo = "ObtenerPorCorreo";
    public const string PasoAgregar = "Agregar";

    public const string PasoActualizarEstado = "ActualizarEstado";

    private readonly Dictionary<int, RolUsuario> _rolesSinConfirmar = [];
    private readonly Dictionary<int, bool> _estadosSinConfirmar = [];

    public List<Usuario> Usuarios { get; } = [];

    public List<string> Pasos { get; } = [];

    // Si no es null, guardar el rol lanza esta excepción (por ejemplo, se cayó la base).
    public Exception? ErrorAlActualizar { get; set; }

    // Si no es null, guardar un usuario nuevo lanza esta excepción.
    public Exception? ErrorAlAgregar { get; set; }

    // Simula que otra solicitud registró el mismo correo entre la consulta previa y el guardado.
    public bool OtraSolicitudGanaLaCarrera { get; set; }

    public Task<Usuario?> ObtenerPorCorreoCorporativoAsync(string correoCorporativo, CancellationToken cancellationToken = default)
    {
        Pasos.Add(PasoObtenerPorCorreo);
        return Task.FromResult(Usuarios.SingleOrDefault(u => u.CorreoCorporativo == correoCorporativo));
    }

    public Task<bool> AgregarSiCorreoDisponibleAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        Pasos.Add(PasoAgregar);

        if (ErrorAlAgregar != null)
        {
            throw ErrorAlAgregar;
        }

        if (OtraSolicitudGanaLaCarrera)
        {
            return Task.FromResult(false);
        }

        Usuarios.Add(usuario);
        return Task.FromResult(true);
    }

    public Task<IBloqueoAdministradores> BloquearAdministradoresAsync(CancellationToken cancellationToken = default)
    {
        Pasos.Add(PasoBloquear);
        IBloqueoAdministradores bloqueo = new BloqueoEnMemoria(this);
        return Task.FromResult(bloqueo);
    }

    public Task<Usuario?> ObtenerPorIdAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        Pasos.Add(PasoObtener);
        return Task.FromResult(Usuarios.SingleOrDefault(u => u.Id == usuarioId));
    }

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken = default)
    {
        Pasos.Add(PasoContar);
        return Task.FromResult(Usuarios.Count(u => u.Rol == RolUsuario.Administrador && u.Activo));
    }

    public Task ActualizarRolAsync(int usuarioId, RolUsuario nuevoRol, CancellationToken cancellationToken = default)
    {
        Pasos.Add(PasoActualizar);

        if (ErrorAlActualizar != null)
        {
            throw ErrorAlActualizar;
        }

        _rolesSinConfirmar[usuarioId] = nuevoRol;
        return Task.CompletedTask;
    }

    public Task ActualizarEstadoAsync(int usuarioId, bool activo, CancellationToken cancellationToken = default)
    {
        Pasos.Add(PasoActualizarEstado);

        if (ErrorAlActualizar != null)
        {
            throw ErrorAlActualizar;
        }

        _estadosSinConfirmar[usuarioId] = activo;
        return Task.CompletedTask;
    }

    // La vinculación con Entra ID (T10) no se usa en estas pruebas.
    public Task<Usuario?> ObtenerPorIdentidadExternaAsync(string entraObjectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> VincularIdentidadSiNoVinculadaAsync(int usuarioId, string entraObjectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    private sealed class BloqueoEnMemoria : IBloqueoAdministradores
    {
        private readonly RepositorioUsuariosConTurnoEnMemoria _repositorio;

        public BloqueoEnMemoria(RepositorioUsuariosConTurnoEnMemoria repositorio)
        {
            _repositorio = repositorio;
        }

        public Task ConfirmarAsync(CancellationToken cancellationToken = default)
        {
            _repositorio.Pasos.Add(PasoConfirmar);

            foreach (KeyValuePair<int, RolUsuario> cambio in _repositorio._rolesSinConfirmar)
            {
                _repositorio.Usuarios.Single(u => u.Id == cambio.Key).Rol = cambio.Value;
            }

            _repositorio._rolesSinConfirmar.Clear();

            foreach (KeyValuePair<int, bool> cambio in _repositorio._estadosSinConfirmar)
            {
                _repositorio.Usuarios.Single(u => u.Id == cambio.Key).Activo = cambio.Value;
            }

            _repositorio._estadosSinConfirmar.Clear();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _repositorio.Pasos.Add(PasoLiberar);
            _repositorio._rolesSinConfirmar.Clear();
            _repositorio._estadosSinConfirmar.Clear();
            return ValueTask.CompletedTask;
        }
    }
}
