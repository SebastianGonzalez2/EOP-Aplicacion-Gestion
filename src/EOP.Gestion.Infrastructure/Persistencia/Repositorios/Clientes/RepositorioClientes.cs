using System;
using System.Collections.Generic;
using System.Text;
namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using EOP.Gestion.Core.Entidades.Clientes;

public class RepositorioClientes : IRepositorioClientes
{
    // Los códigos con los que SQL Server avisa que se repitió un valor único: 2601 si lo frenó
    // un índice único y 2627 si fue una restricción UNIQUE. Para nosotros significan lo mismo.
    private const int ErrorIndiceUnicoRepetido = 2601;
    private const int ErrorRestriccionUnicaRepetida = 2627;

    // El índice que creó la migración de S1-T18.
    private const string IndiceRut = "IX_Clientes_Rut";

    private readonly EopDbContext _context;

    // Recibimos el contexto mediante el constructor.
    public RepositorioClientes(EopDbContext context)
    {
        _context = context;
    }



    // Comprueba si ya existe un cliente con el RUT normalizado.
    public Task<bool> ExistePorRutAsync(string rutNormalizado, CancellationToken cancellationToken = default)
    {
        return _context.Clientes.AnyAsync(c => c.Rut == rutNormalizado, cancellationToken); //busca al menos un cliente con el mismo rut y permite cancelar la operación si es necesario
    }


    // Agrega un nuevo cliente a la base de datos.
    //
    // S1-T27: el problema que resuelve el try/catch. Dos personas registran el mismo RUT casi al
    // mismo tiempo: las dos preguntan con ExistePorRutAsync, a las dos les dice "no existe" y las
    // dos intentan guardar. El índice único IX_Clientes_Rut deja entrar solo a la primera; la
    // segunda recibe un error de SQL Server. Antes ese error llegaba sin atrapar hasta la pantalla
    // de error genérica. Ahora lo convertimos en "false", y el caso de uso le muestra al usuario
    // el mismo mensaje de RUT repetido que vería en el caso normal.
    public async Task<bool> AgregarSiRutDisponibleAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        // Indicamos a Entity Framework que queremos agregar
        // el cliente a la tabla Clientes.
        _context.Clientes.Add(cliente);

        // El "when" hace que el catch solo atrape el error si es el del índice del RUT. Cualquier
        // otro (la base caída, una restricción CHECK violada) sigue de largo hasta el manejo de
        // errores de la aplicación, que lo registra (RNF-12). Así nunca se le informa al usuario
        // un problema distinto como si fuera un RUT repetido.
        try
        {
            // Guardamos los cambios en la base de datos.
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateException excepcion) when (RepitioElIndiceDelRut(excepcion))
        {
            // EF sigue teniendo anotado este cliente como "pendiente de guardar". El contexto dura
            // todo el pedido, así que lo sacamos para que otro SaveChanges no lo vuelva a intentar.
            _context.Entry(cliente).State = EntityState.Detached;

            return false;
        }
    }

    // Igual que en RepositorioProveedores: miramos el número de error y el nombre del índice, en
    // vez de suponer que todo DbUpdateException es un duplicado.
    //
    // El nombre del índice solo viene dentro del texto del mensaje. Lo usamos únicamente para
    // esta comparación: ese mensaje trae el RUT repetido, así que no se muestra ni se registra.
    private static bool RepitioElIndiceDelRut(DbUpdateException excepcion)
    {
        if (excepcion.InnerException is not SqlException errorDeSqlServer)
        {
            return false;
        }

        if (errorDeSqlServer.Number != ErrorIndiceUnicoRepetido &&
            errorDeSqlServer.Number != ErrorRestriccionUnicaRepetida)
        {
            return false;
        }

        return errorDeSqlServer.Message.Contains(IndiceRut, StringComparison.Ordinal);
    }



}
