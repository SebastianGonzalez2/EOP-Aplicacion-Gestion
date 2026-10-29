
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces;

namespace EOP.Gestion.Core.CasosDeUso.Usuarios;

public class IdentificarOVincularUsuario
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;

    public IdentificarOVincularUsuario(IRepositorioUsuarios repositorioUsuarios)
    {
        _repositorioUsuarios = repositorioUsuarios;
    }

    public async Task<Usuario?> EjecutarAsync(string entraObjectId, string? correoCorporativo, CancellationToken cancellationToken = default){
        // Sin identificador externo no podemos identificar al usuario.
        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            return null;
        }

        // Primero buscamos si el usuario ya está vinculado.
        Usuario? usuario = await _repositorioUsuarios.ObtenerPorIdentidadExternaAsync(entraObjectId, cancellationToken);

        if (usuario != null)
        {
            if (usuario.Activo)
            {
                return usuario;
            }
            else
            {
                return null;
            }
        }

        // Si todavía no está vinculado, necesitamos su correo.
        if (string.IsNullOrWhiteSpace(correoCorporativo))
        {
            return null;
        }

        usuario = await _repositorioUsuarios.ObtenerPorCorreoCorporativoAsync(correoCorporativo.Trim(), cancellationToken);

        // No vinculamos usuarios inexistentes, inactivos
        // ni usuarios vinculados con otra identidad.
        if (usuario == null || !usuario.Activo || usuario.EntraObjectId != null)
        {
            return null;
        }

        // Intentamos vincular la identidad externa al usuario existente. Si otro proceso ya lo vinculó, no hacemos nada.
        bool vinculacionExitosa = await _repositorioUsuarios.VincularIdentidadSiNoVinculadaAsync(usuario.Id, entraObjectId,  cancellationToken);

        if (!vinculacionExitosa)
        {
            return null;
        }

        // Recuperamos el usuario con la vinculación ya guardada.
        return await _repositorioUsuarios.ObtenerPorIdentidadExternaAsync(entraObjectId, cancellationToken);
    }
}
