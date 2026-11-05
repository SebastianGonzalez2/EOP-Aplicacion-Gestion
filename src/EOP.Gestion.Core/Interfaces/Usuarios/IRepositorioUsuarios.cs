using System;
using System.Collections.Generic;
using System.Text;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Core.Interfaces.Usuarios;

public interface IRepositorioUsuarios
{

    //Task representa una operación asincrónica que puede completarse en el futuro.
    // Usuario? indica que el resultado de la operación puede ser un objeto Usuario o null.
    // CancellationToken es un mecanismo para cancelar operaciones asincrónicas. Por ejemplo si el usuario cierra la aplicación antes de que se complete la operación,
    // podemos usar el token para cancelar la operación y liberar recursos.
    Task<Usuario?> ObtenerPorIdentidadExternaAsync(string entraObjectId, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorCorreoCorporativoAsync(string correoCorporativo, CancellationToken cancellationToken = default);

    Task<bool> VincularIdentidadSiNoVinculadaAsync(int usuarioId,string entraObjectId,CancellationToken cancellationToken = default);

    // Espera su turno para operar sobre los administradores y abre una transacción.
    // Los métodos de abajo que se llamen mientras dure el turno forman parte de ella.
    // Ver la explicación completa en IBloqueoAdministradores.
    Task<IBloqueoAdministradores> BloquearAdministradoresAsync(CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorIdAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken = default);

    Task ActualizarRolAsync(int usuarioId, RolUsuario nuevoRol, CancellationToken cancellationToken = default);

    // Guarda un usuario nuevo y devuelve true. Si el correo ya lo tiene otro usuario, no guarda
    // nada y devuelve false: que dos solicitudes pidan el mismo correo a la vez es algo esperable
    // del negocio, no un error del sistema. Cualquier otro problema de la base sí es una excepción.
    Task<bool> AgregarSiCorreoDisponibleAsync(Usuario usuario, CancellationToken cancellationToken = default);

}

