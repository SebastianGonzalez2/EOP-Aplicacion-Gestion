using System;
using System.Collections.Generic;
using System.Text;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Core.Interfaces;

public interface IRepositorioUsuarios
{

    //Task representa una operación asincrónica que puede completarse en el futuro.
    // Usuario? indica que el resultado de la operación puede ser un objeto Usuario o null.
    // CancellationToken es un mecanismo para cancelar operaciones asincrónicas. Por ejemplo si el usuario cierra la aplicación antes de que se complete la operación,
    // podemos usar el token para cancelar la operación y liberar recursos.
    Task<Usuario?> ObtenerPorIdentidadExternaAsync(string entraObjectId, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorCorreoCorporativoAsync(string correoCorporativo, CancellationToken cancellationToken = default);

    Task<bool> VincularIdentidadSiNoVinculadaAsync(int usuarioId,string entraObjectId,CancellationToken cancellationToken = default);

}

