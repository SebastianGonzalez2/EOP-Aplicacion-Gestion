
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios;

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
}
