using System;
using System.Collections.Generic;
using System.Text;
namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

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




}
