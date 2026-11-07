
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Usuarios;

public class RepositorioUsuarios : IRepositorioUsuarios
{
    // El contexto de base de datos es el que nos permite acceder a las tablas y realizar operaciones sobre ellas.
    private readonly EopDbContext _contexto;

    public RepositorioUsuarios(EopDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<Usuario?> ObtenerPorIdentidadExternaAsync(string entraObjectId, CancellationToken cancellationToken = default){

        return await _contexto.Usuarios
            // AsNoTracking significa que no vamos a modificar el objeto que traemos. Esto es más eficiente porque Entity Framework no tiene que estar pendiente de cambios.
            .AsNoTracking()
            // SingleOrDefaultAsync busca un único objeto que cumpla la condición. Si no encuentra ninguno, devuelve null. Si encuentra más de uno, lanza una excepción.
            .SingleOrDefaultAsync(
                u => u.EntraObjectId == entraObjectId,
                cancellationToken);
    }

    public async Task<Usuario?> ObtenerPorCorreoCorporativoAsync(string correoCorporativo, CancellationToken cancellationToken = default){
        
        return await _contexto.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(
                u => u.CorreoCorporativo == correoCorporativo,
                cancellationToken);
    }

    // Este metodo lo que hace es vincular la identidad externa (entraObjectId) a un usuario existente
    // en la base de datos, pero solo si ese usuario no tiene ya una identidad vinculada y está activo.
    public async Task<bool> VincularIdentidadSiNoVinculadaAsync(int usuarioId, string entraObjectId, CancellationToken cancellationToken = default){
       
        int filasActualizadas = await _contexto.Usuarios
            // Where filtra los usuarios que cumplen con las condiciones: el Id del usuario es el que nos pasan, no tiene identidad externa vinculada y está activo.
            .Where(u => u.Id == usuarioId
                && u.EntraObjectId == null
                && u.Activo)
            // ExecuteUpdateAsync actualiza los registros que cumplen con el filtro sin necesidad de hacer un SaveChanges(). En este caso,
            // establece la propiedad EntraObjectId del usuario al valor que nos pasan.
            .ExecuteUpdateAsync(
                // cambios es el objeto que nos da Entity Framework para definir qué propiedades queremos actualizar.
                // En este caso, le decimos que queremos establecer la propiedad EntraObjectId al valor que nos pasan.
                cambios => cambios.SetProperty(
                    u => u.EntraObjectId,
                    entraObjectId),
                cancellationToken);

        //ExecuteUpdateAsync() devuelve la cantidad de filas que SQL Server actualizó
        if (filasActualizadas == 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Este método es el que protege la regla del último Administrador activo cuando
    // llegan varias solicitudes al mismo tiempo.
    //
    // El problema: dos solicitudes cuentan 2 administradores a la vez, cada una degrada
    // a uno distinto, y el sistema queda sin ninguno. Cada una hizo bien su cuenta, pero
    // ninguna vio lo que hacía la otra. Hay que lograr que pasen de a una.
    //
    // La solución: abrimos una transacción y, dentro de ella, leemos los administradores
    // activos con dos pedidos especiales para SQL Server:
    //
    // - UPDLOCK: "voy a modificar estas filas". Solo una transacción a la vez puede tener
    //   este bloqueo sobre una fila, y lo conserva hasta terminar. Así, la segunda solicitud
    //   se queda esperando en esta línea hasta que la primera termine. Las lecturas comunes
    //   no se frenan, por eso esto no traba a los demás usuarios que navegan la aplicación.
    // - HOLDLOCK: "y que tampoco cambie el conjunto de filas que leí". Además de las filas,
    //   bloquea el espacio entre ellas, para que ninguna fila nueva pueda aparecer en esa
    //   lectura mientras dure la transacción. Con los dos juntos, todas las solicitudes
    //   que piden turno chocan entre sí y pasan de a una, sin casos especiales que pensar.
    //
    // ¿Por qué no alcanza con una transacción común? Porque por sí sola no impide que dos
    // transacciones lean lo mismo a la vez. En Azure SQL Database y en las bases que crea
    // EF Core es todavía más claro: las lecturas ven una copia de los datos sin esperar a
    // nadie (READ_COMMITTED_SNAPSHOT). Con UPDLOCK la lectura sí espera y ve el último dato real.
    //
    // ¿Por qué no un nivel de aislamiento Serializable sin UPDLOCK? También evitaría el
    // error, pero las dos solicitudes leerían juntas y se trabarían entre sí al escribir
    // (un deadlock): SQL Server cancelaría una con un error. Así, en cambio, simplemente
    // hacen fila.
    public async Task<IBloqueoAdministradores> BloquearAdministradoresAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);

        // UPDLOCK y HOLDLOCK son propios de SQL Server. Las pruebas que corren sobre
        // SQLite no los entienden, pero tampoco los necesitan: ahí, abrir la transacción
        // ya reserva toda la base para escribir (BEGIN IMMEDIATE).
        if (_contexto.Database.IsSqlServer())
        {
            int rolAdministrador = (int)RolUsuario.Administrador;

            await _contexto.Database.ExecuteSqlAsync(
                $"SELECT COUNT(*) FROM Usuarios WITH (UPDLOCK, HOLDLOCK) WHERE Rol = {rolAdministrador} AND Activo = 1",
                cancellationToken);
        }

        return new BloqueoAdministradores(transaccion);
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        return await _contexto.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(
                u => u.Id == usuarioId,
                cancellationToken);
    }

    public async Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken = default)
    {
        return await _contexto.Usuarios.CountAsync(
            u => u.Rol == RolUsuario.Administrador && u.Activo,
            cancellationToken);
    }

    // Cambia únicamente la columna Rol. Los demás datos del usuario y los registros
    // relacionados con él quedan como estaban (RF-73: no se altera su historial).
    public async Task ActualizarRolAsync(int usuarioId, RolUsuario nuevoRol, CancellationToken cancellationToken = default)
    {
        await _contexto.Usuarios
            .Where(u => u.Id == usuarioId)
            .ExecuteUpdateAsync(
                cambios => cambios.SetProperty(u => u.Rol, nuevoRol),
                cancellationToken);
    }

    // Este método es el que evita los correos repetidos cuando llegan dos altas al mismo tiempo.
    //
    // El problema: las dos solicitudes preguntan "¿este correo está libre?", las dos reciben
    // que sí y las dos intentan guardar. Esa consulta previa no alcanza para frenarlas.
    //
    // La solución: dejamos que decida el índice único de CorreoCorporativo. SQL Server garantiza
    // que solo una de las dos pueda insertar; la otra recibe un error y acá lo convertimos en
    // la respuesta "ese correo ya está registrado".
    public async Task<bool> AgregarSiCorreoDisponibleAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        _contexto.Usuarios.Add(usuario);

        // DbUpdateException no significa solamente "correo repetido": también aparece, por
        // ejemplo, si no se cumple la restricción del rol. Por eso el "when" deja atrapar el error
        // solo si SQL Server avisa que se repitió el índice del correo (ver IndicesUnicos). Si el
        // problema es otro, la excepción sigue de largo hasta el manejo de errores de la aplicación,
        // que la registra (RNF-12): no lo ocultamos ni lo informamos como un correo repetido.
        //
        // S1-T34: antes acá le volvíamos a preguntar a la base si el correo existía. Lo cambiamos
        // para que todos los repositorios detecten los duplicados de la misma forma, y porque mirar
        // el error mismo es más preciso que deducirlo con una consulta aparte.
        try
        {
            await _contexto.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateException excepcion) when (IndicesUnicos.RepitioElIndice(excepcion, IndicesUnicos.UsuariosCorreoCorporativo))
        {
            // EF Core sigue recordando que tiene este usuario pendiente de guardar. Lo sacamos
            // para que un guardado posterior con este mismo contexto no lo vuelva a intentar.
            _contexto.Entry(usuario).State = EntityState.Detached;

            return false;
        }
    }
}
