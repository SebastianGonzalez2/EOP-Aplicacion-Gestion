using EOP.Gestion.Core.Entidades.Usuarios;
using Microsoft.EntityFrameworkCore;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Proveedores;


namespace EOP.Gestion.Infrastructure.Persistencia;

/// <summary>
/// Contexto de EF Core del sistema. Las entidades y sus configuraciones
/// se agregan en tareas posteriores, a partir del modelo de datos validado.
/// </summary>
// DbContext es la clase de Entity Framework que permite trabajar
// con la base de datos desde C#.
// Nuestro EopDbContext utiliza SQL Server para guardar y consultar datos.
public class EopDbContext(DbContextOptions<EopDbContext> options)
    : DbContext(options)
{
    // DbSet representa un conjunto de entidades de tipo Usuario.
    // Permite consultar, agregar, modificar y eliminar usuarios
    // mediante Entity Framework, sin escribir SQL manualmente.
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<CuentaBancaria> CuentasBancarias => Set<CuentaBancaria>();


    // Se ejecuta cuando Entity Framework construye el modelo
    // de datos de la aplicación.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Busca y aplica las configuraciones de las entidades.
        // Por ejemplo: campos obligatorios, longitudes e índices únicos.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(EopDbContext).Assembly);

        /*
            Podriamos poner:
        
            modelBuilder.ApplyConfiguration(
                new UsuarioConfiguracion());

           Y ahi iria directamente a la clase UsuarioConfiguracion.cs, pero tendriamos que hacer lo mismo para cada entidad digamos.

           Pero poniendo "typeof(EopDbContext).Assembly", le estamos diciendo que obtenga el ensamblado que contenga la clase
           EopDbContext, osea EOP.Gestion.Infrastructure. Y lo que hace es buscar dentro de ese ensamblado las clases que implementen "IEntityTypeConfiguration<T>"
           y despues aplica sus configuraciones (que el nombre sea unique, etc, etc)
        */
    }
}
