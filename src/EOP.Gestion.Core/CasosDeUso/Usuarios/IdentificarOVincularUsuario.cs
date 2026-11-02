
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;

namespace EOP.Gestion.Core.CasosDeUso.Usuarios;

public class IdentificarOVincularUsuario
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;

    public IdentificarOVincularUsuario(IRepositorioUsuarios repositorioUsuarios)
    {
        _repositorioUsuarios = repositorioUsuarios;
    }

    /*
     el método identifica qué usuario de EOP corresponde a la persona que inició sesión con Microsoft.

        Hace lo siguiente:
        1. Primero busca por EntraObjectId: si el usuario ya está vinculado y activo, lo encuentra y lo devuelve.
        2. Si no está vinculado, busca por correo: comprueba si existe un usuario de EOP con el mismo correo corporativo.
        3. Si lo encuentra y está activo y sin vinculación, guarda su EntraObjectId para reconocerlo en futuros inicios de sesión.
        4. Finalmente devuelve el usuario de EOP. Si no puede identificarlo o vincularlo correctamente, devuelve null. 
    */
    public async Task<Usuario?> ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(string entraObjectId, string? correoCorporativo, CancellationToken cancellationToken = default){
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
        // Si devuelve false no cortamos todavía: puede ser que otro inicio de sesión
        // simultáneo de la misma persona lo haya vinculado un instante antes.
        await _repositorioUsuarios.VincularIdentidadSiNoVinculadaAsync(usuario.Id, entraObjectId, cancellationToken);

        // Recuperamos el usuario con la vinculación ya guardada.
        Usuario? usuarioVinculado = await _repositorioUsuarios.ObtenerPorIdentidadExternaAsync(entraObjectId, cancellationToken);

        // Solo lo aceptamos si quedó vinculado ESTE mismo usuario y sigue activo
        // (pudieron inactivarlo justo mientras se vinculaba).
        if (usuarioVinculado == null || usuarioVinculado.Id != usuario.Id || !usuarioVinculado.Activo)
        {
            return null;
        }

        return usuarioVinculado;
    }
}
