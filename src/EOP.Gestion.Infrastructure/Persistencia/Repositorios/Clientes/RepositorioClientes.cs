using System;
using System.Collections.Generic;
using System.Text;
namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using EOP.Gestion.Core.Entidades.Clientes;

public class RepositorioClientes : IRepositorioClientes
{

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
    public async Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        // Indicamos a Entity Framework que queremos agregar
        // el cliente a la tabla Clientes.
        _context.Clientes.Add(cliente);

        // Guardamos los cambios en la base de datos.
        await _context.SaveChangesAsync(cancellationToken);
    }



}
