using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia;

/// <summary>
/// Contexto de EF Core del sistema. Las entidades y sus configuraciones
/// se agregan en tareas posteriores, a partir del modelo de datos validado.
/// </summary>
public class EopDbContext(DbContextOptions<EopDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cada entidad se configura en su propia clase IEntityTypeConfiguration<T>
        // dentro de Persistencia/Configuraciones.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EopDbContext).Assembly);
    }
}
