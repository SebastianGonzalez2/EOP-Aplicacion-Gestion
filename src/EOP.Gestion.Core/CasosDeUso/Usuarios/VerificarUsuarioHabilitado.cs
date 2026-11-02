using System;
using System.Collections.Generic;
using System.Text;
using EOP.Gestion.Core.Interfaces.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;  

namespace EOP.Gestion.Core.CasosDeUso.Usuarios;

public class VerificarUsuarioHabilitado
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;

    public VerificarUsuarioHabilitado(IRepositorioUsuarios repositorioUsuarios)
    {
        _repositorioUsuarios = repositorioUsuarios;
    }

    //Task significa que el método es asincrónico y devuelve un valor de tipo Task. Esto permite que el método se ejecute de manera asíncrona, lo que significa que puede
    //realizar operaciones que pueden tardar un tiempo en completarse (como acceder a una base de datos o llamar a un servicio externo) sin bloquear el hilo principal de ejecución.

    //CancellationToken bien simple sirve para cancelar la operación si es necesario, por ejemplo,
    //si el usuario decide cancelar la acción antes de que se complete.
    public async Task<Usuario?> ObtenerUsuarioHabilitadoAsync(string? entraObjectId,CancellationToken cancellationToken = default){
        // Sin una identidad externa válida no podemos autorizar al usuario.
        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            return null;
        }

        // Buscamos al usuario de EOP vinculado a esta identidad de Microsoft.
        var usuario = await _repositorioUsuarios.ObtenerPorIdentidadExternaAsync(entraObjectId, cancellationToken);

        // Solo está habilitado si existe en EOP y está activo.
        //Le estamos diciendo: Si el usuario no es null, accede a su propiedad Activo. Si es null, devuelve directamente null.
        return usuario?.Activo == true ? usuario : null;
    }

    public async Task<bool> EstaHabilitadoAsync(string? entraObjectId,CancellationToken cancellationToken = default){

        var usuario = await ObtenerUsuarioHabilitadoAsync(entraObjectId,cancellationToken);

        return usuario != null;
    }
}