using EOP.Gestion.Core.Entidades.Clientes;

namespace EOP.Gestion.Tests.Unitarias;

public class ClienteTests
{
    [Fact]
    public void ClienteNuevo_DebeEstarActivoPorDefecto()
    {
        // Creamos un cliente nuevo.
        var cliente = new Cliente();

        // Comprobamos que comience activo.
        Assert.True(cliente.Activo);
    }
}