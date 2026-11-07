
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Interfaces.Proveedores;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;

public class RepositorioProveedores : IRepositorioProveedores
{
    private readonly EopDbContext _context;

    // Recibimos el contexto para poder consultar los
    // proveedores registrados en la base de datos.
    public RepositorioProveedores(EopDbContext context)
    {
        _context = context;
    }

    // AnyAsync le pregunta a la base "¿hay al menos uno?" y corta en cuanto encuentra el primero.
    // No trae ningún proveedor a memoria, así que tampoco hay nada que se pueda modificar sin querer.
    public Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado,CancellationToken cancellationToken = default)
    {
        return _context.Proveedores.AnyAsync(proveedor => proveedor.NombreNormalizado == nombreNormalizado,cancellationToken);
    }

    // Comprueba si ya existe un proveedor con el RUT normalizado.
    public Task<bool> ExistePorRutAsync(string rutNormalizado,CancellationToken cancellationToken = default)
    {
        return _context.Proveedores.AnyAsync(proveedor => proveedor.Rut == rutNormalizado,cancellationToken);
    }

    // Comprueba si existe un proveedor con el Id recibido.
    // Devuelve true si existe y false si no existe.
    public async Task<bool> ExistePorIdAsync(int proveedorId,CancellationToken cancellationToken = default)
    {
        return await _context.Proveedores.AnyAsync(proveedor => proveedor.Id == proveedorId,cancellationToken);
    }

    public async Task<ResultadoAgregarProveedor> AgregarAsync(Proveedor proveedor,CancellationToken cancellationToken = default)
    {
        // Add no marca solo al Proveedor: también marca las cuentas de Proveedor.CuentasBancarias.
        // Al guardar, EF inserta primero el Proveedor, obtiene su Id y se lo pone a cada cuenta.
        _context.Proveedores.Add(proveedor);

        // Con un único SaveChangesAsync alcanza para que sea todo o nada: como son varios INSERT,
        // EF los mete en una sola transacción. Si falla la última cuenta, se deshace también el
        // Proveedor. Por eso no abrimos una transacción a mano.
        //
        // Los catch tienen un "when": solo atrapan el error si esa condición da true. Si no, la
        // excepción sigue su camino intacta y termina en el manejo de errores de la aplicación,
        // que la registra y muestra una pantalla segura (RNF-12). Así una base caída o una
        // restricción violada nunca se le informan al usuario como si fueran un duplicado.
        // Cómo se reconoce el índice repetido está explicado en IndicesUnicos.
        //
        // Si se repiten el nombre y el RUT a la vez, SQL Server avisa solo del primero que encuentra.
        // Da igual: el alta se rechaza y el mensaje que recibe el usuario sigue siendo cierto.
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            return ResultadoAgregarProveedor.Agregado;
        }
        catch (DbUpdateException excepcion) when (IndicesUnicos.RepitioElIndice(excepcion, IndicesUnicos.ProveedoresNombreNormalizado))
        {
            OlvidarElProveedorRechazado();

            return ResultadoAgregarProveedor.NombreDuplicado;
        }
        catch (DbUpdateException excepcion) when (IndicesUnicos.RepitioElIndice(excepcion, IndicesUnicos.ProveedoresRut))
        {
            OlvidarElProveedorRechazado();

            return ResultadoAgregarProveedor.RutDuplicado;
        }
    }

    // Después del rechazo, EF sigue teniendo anotado el Proveedor y sus cuentas como "pendientes
    // de guardar". El contexto dura todo el pedido, así que los borramos de la lista para que un
    // SaveChanges posterior no intente insertarlos de nuevo.
    private void OlvidarElProveedorRechazado()
    {
        _context.ChangeTracker.Clear();
    }
}
