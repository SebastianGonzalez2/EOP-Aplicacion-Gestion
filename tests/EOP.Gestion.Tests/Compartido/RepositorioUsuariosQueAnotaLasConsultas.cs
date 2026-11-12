using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;

namespace EOP.Gestion.Tests.Compartido;

// Repositorio en memoria con la misma búsqueda que el real (igualdad exacta
// del Object ID) que además anota qué le pidieron. Lo usan las pruebas
// unitarias de la T11 para comprobar no solo QUÉ respondió el código, sino
// también QUÉ le preguntó a la base (por ejemplo, que ante un identificador
// vacío ni siquiera la consulte).
//
// No reutilizamos el de IdentificarOVincularUsuarioTests porque acá hace falta
// contar llamadas y guardar los tokens de cancelación recibidos.
internal sealed class RepositorioUsuariosQueAnotaLasConsultas : IRepositorioUsuarios
{
    public List<Usuario> Usuarios { get; } = [];

    // Si no es null, la búsqueda por Object ID lanza esta excepción (por ejemplo, se cayó la base).
    public Exception? ErrorAlBuscar { get; set; }

    public List<string> IdentificadoresConsultados { get; } = [];
    public List<CancellationToken> TokensRecibidos { get; } = [];
    public int CantidadDeBusquedasPorCorreo { get; private set; }
    public int CantidadDeVinculacionesIntentadas { get; private set; }

    public Task<Usuario?> ObtenerPorIdentidadExternaAsync(string entraObjectId, CancellationToken cancellationToken = default)
    {
        IdentificadoresConsultados.Add(entraObjectId);
        TokensRecibidos.Add(cancellationToken);

        // Igual que EF Core: si la solicitud ya se canceló, no seguimos.
        cancellationToken.ThrowIfCancellationRequested();

        if (ErrorAlBuscar != null)
        {
            throw ErrorAlBuscar;
        }

        return Task.FromResult(Usuarios.SingleOrDefault(u => u.EntraObjectId == entraObjectId));
    }

    public Task<Usuario?> ObtenerPorCorreoCorporativoAsync(string correoCorporativo, CancellationToken cancellationToken = default)
    {
        CantidadDeBusquedasPorCorreo++;
        return Task.FromResult(Usuarios.SingleOrDefault(u => u.CorreoCorporativo == correoCorporativo));
    }

    public Task<bool> VincularIdentidadSiNoVinculadaAsync(int usuarioId, string entraObjectId, CancellationToken cancellationToken = default)
    {
        CantidadDeVinculacionesIntentadas++;
        return Task.FromResult(false);
    }

    // El cambio de rol (T14) no se usa en las pruebas de este repositorio.
    public Task<IBloqueoAdministradores> BloquearAdministradoresAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Usuario?> ObtenerPorIdAsync(int usuarioId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task ActualizarRolAsync(int usuarioId, RolUsuario nuevoRol, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task ActualizarEstadoAsync(int usuarioId, bool activo, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> AgregarSiCorreoDisponibleAsync(Usuario usuario, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
