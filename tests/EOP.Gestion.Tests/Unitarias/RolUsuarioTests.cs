
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
namespace EOP.Gestion.Tests.Unitarias;

public class RolUsuarioTests
{
    [Fact]
    public void ExistenExactamenteTresRolesFijos()
    {
        var roles = Enum.GetValues<RolUsuario>();

        Assert.Equal(3, roles.Length);
        Assert.Equal(1, (int)RolUsuario.Administrador);
        Assert.Equal(2, (int)RolUsuario.Supervisor);
        Assert.Equal(3, (int)RolUsuario.Operador);
    }

    [Fact]
    public void LaConfiguracionDeUsuarioRestringeLosRoles()
    {
        // No necesitamos conectarnos a SQL Server.
        // Solo inspeccionamos el modelo configurado por EF Core.
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer("Server=localhost;Database=SoloPruebas")
            .Options;

        using var contexto = new EopDbContext(opciones);

        var entidad = contexto.GetService<IDesignTimeModel>()
        .Model.FindEntityType(typeof(Usuario));
        Assert.NotNull(entidad);

        // Todo usuario debe tener un rol obligatorio.
        var propiedadRol = entidad.FindProperty(nameof(Usuario.Rol));
        Assert.NotNull(propiedadRol);
        Assert.False(propiedadRol.IsNullable);

        // Verificamos la restricción definida en UsuarioConfiguracion.cs.
        Assert.Contains(
            entidad.GetCheckConstraints(),
            restriccion =>
                restriccion.Name == "CK_Usuarios_RolValido" &&
                restriccion.Sql == "[Rol] IN (1, 2, 3)");
    }
}
