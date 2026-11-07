using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EOP.Gestion.Tests.Integracion;

// S1-T27: las restricciones que la base aplica por su cuenta a Clientes y Usuarios, sin pasar
// por los casos de uso. Se guardan las entidades directo con el contexto, a propósito: lo que
// se prueba es la segunda línea de defensa, la que actúa si un camino se saltea las validaciones.
public class RestriccionesClientesYUsuariosSqlServerTests : IAsyncLifetime
{
    private const string MigracionAnterior = "S2_T16_RegistrarMaterialProveedor";

    private static readonly string[] RestriccionesNuevas =
    [
        "CK_Clientes_AlMenosUnCorreo",
        "CK_Clientes_NombreNoVacio",
        "CK_Clientes_RutFormato",
        "CK_Clientes_WhatsAppNoVacio",
        "CK_Usuarios_CorreoCorporativoNoVacio",
        "CK_Usuarios_NombreCompletoNoVacio"
    ];

    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    private EopDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EopDbContext>().UseSqlServer(_cadenaConexion).Options);

    private static Cliente CrearClienteValido() => new()
    {
        Nombre = "Cliente de prueba",
        Rut = "215521750017",
        CorreoContacto = "cliente@ejemplo.com",
        CorreoFacturacion = "facturas@ejemplo.com",
        NumeroWhatsApp = "+59899123456",
        Activo = true
    };

    private static Usuario CrearUsuarioValido() => new()
    {
        NombreCompleto = "Ana Pereira",
        CorreoCorporativo = "ana@eop.com.uy",
        Rol = RolUsuario.Operador,
        Activo = true
    };

    private async Task GuardarAsync<T>(T entidad) where T : class
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Add(entidad);
        await contexto.SaveChangesAsync();
    }

    // Mira el motivo además del rechazo: sin eso, la prueba podría pasar porque falló otra restricción.
    private async Task ComprobarQueLaBaseRechazaAsync<T>(T entidad, string motivoEsperado) where T : class
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Add(entidad);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains(motivoEsperado, excepcion.GetBaseException().Message);
    }

    private static Task<List<string>> ObtenerRestriccionesNuevasAsync(EopDbContext contexto) =>
        contexto.Database
            .SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.check_constraints")
            .Where(nombre => RestriccionesNuevas.Contains(nombre))
            .ToListAsync();

    [FactConSqlServer]
    public async Task Cliente_Valido_SeGuarda()
    {
        await GuardarAsync(CrearClienteValido());

        using EopDbContext contexto = CrearContexto();
        Assert.Equal(1, await contexto.Clientes.CountAsync());
    }

    [TheoryConSqlServer]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Cliente_ConUnSoloCorreo_SeGuarda(bool conCorreoContacto, bool conCorreoFacturacion)
    {
        Cliente cliente = CrearClienteValido();
        cliente.CorreoContacto = conCorreoContacto ? cliente.CorreoContacto : null;
        cliente.CorreoFacturacion = conCorreoFacturacion ? cliente.CorreoFacturacion : null;

        await GuardarAsync(cliente);
    }

    [FactConSqlServer]
    public async Task Cliente_ConNombreDe200Caracteres_EntraEnLaColumna()
    {
        Cliente cliente = CrearClienteValido();
        cliente.Nombre = new string('a', 200);

        await GuardarAsync(cliente);
    }

    [FactConSqlServer]
    public async Task Cliente_ConNombreDe201Caracteres_LoRechazaLaBase()
    {
        Cliente cliente = CrearClienteValido();
        cliente.Nombre = new string('a', 201);

        await ComprobarQueLaBaseRechazaAsync(cliente, "truncated");
    }

    [TheoryConSqlServer]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Cliente_ConNombreEnBlanco_LoRechazaLaBase(string nombre)
    {
        Cliente cliente = CrearClienteValido();
        cliente.Nombre = nombre;

        await ComprobarQueLaBaseRechazaAsync(cliente, "CK_Clientes_NombreNoVacio");
    }

    [TheoryConSqlServer]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Cliente_ConWhatsAppEnBlanco_LoRechazaLaBase(string numeroWhatsApp)
    {
        Cliente cliente = CrearClienteValido();
        cliente.NumeroWhatsApp = numeroWhatsApp;

        await ComprobarQueLaBaseRechazaAsync(cliente, "CK_Clientes_WhatsAppNoVacio");
    }

    [TheoryConSqlServer]
    [InlineData("21552175-001")]   // con separador: no está normalizado
    [InlineData("21.552.175")]     // con puntos y corto
    [InlineData("21552175001")]    // 11 dígitos
    [InlineData("")]
    [InlineData("21552175001²")]   // '²' pasa un LIKE '[0-9]' según la intercalación
    [InlineData("21552175001٧")]   // dígito arábigo
    public async Task Cliente_ConRutQueNoSonDoceDigitos_LoRechazaLaBase(string rut)
    {
        Cliente cliente = CrearClienteValido();
        cliente.Rut = rut;

        await ComprobarQueLaBaseRechazaAsync(cliente, "CK_Clientes_RutFormato");
    }

    [TheoryConSqlServer]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", null)]
    [InlineData(null, "   ")]
    public async Task Cliente_SinNingunCorreo_LoRechazaLaBase(string? correoContacto, string? correoFacturacion)
    {
        Cliente cliente = CrearClienteValido();
        cliente.CorreoContacto = correoContacto;
        cliente.CorreoFacturacion = correoFacturacion;

        await ComprobarQueLaBaseRechazaAsync(cliente, "CK_Clientes_AlMenosUnCorreo");
    }

    [FactConSqlServer]
    public async Task Cliente_ConRutRepetido_LoRechazaElIndiceUnico()
    {
        await GuardarAsync(CrearClienteValido());

        Cliente repetido = CrearClienteValido();
        repetido.Nombre = "Otro nombre";

        await ComprobarQueLaBaseRechazaAsync(repetido, "IX_Clientes_Rut");
    }

    [FactConSqlServer]
    public async Task Usuario_Valido_SeGuarda()
    {
        await GuardarAsync(CrearUsuarioValido());
    }

    [TheoryConSqlServer]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Usuario_ConNombreCompletoEnBlanco_LoRechazaLaBase(string nombreCompleto)
    {
        Usuario usuario = CrearUsuarioValido();
        usuario.NombreCompleto = nombreCompleto;

        await ComprobarQueLaBaseRechazaAsync(usuario, "CK_Usuarios_NombreCompletoNoVacio");
    }

    [TheoryConSqlServer]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Usuario_ConCorreoCorporativoEnBlanco_LoRechazaLaBase(string correoCorporativo)
    {
        Usuario usuario = CrearUsuarioValido();
        usuario.CorreoCorporativo = correoCorporativo;

        await ComprobarQueLaBaseRechazaAsync(usuario, "CK_Usuarios_CorreoCorporativoNoVacio");
    }

    // Deshacer la migración tiene que quitar solo sus restricciones y devolver la columna a
    // nvarchar(max), sin perder los clientes ni los usuarios ya guardados.
    [FactConSqlServer]
    public async Task Migracion_AlRevertirlaYVolverAAplicarla_ConservaLosDatos()
    {
        await GuardarAsync(CrearClienteValido());
        await GuardarAsync(CrearUsuarioValido());

        using EopDbContext contexto = CrearContexto();
        IMigrator migrador = contexto.GetService<IMigrator>();
        Assert.Equal(RestriccionesNuevas.Length, (await ObtenerRestriccionesNuevasAsync(contexto)).Count);

        await migrador.MigrateAsync(MigracionAnterior);

        Assert.Empty(await ObtenerRestriccionesNuevasAsync(contexto));
        Assert.Equal(1, await contexto.Clientes.CountAsync());
        Assert.Equal(1, await contexto.Usuarios.CountAsync());

        await contexto.Database.MigrateAsync();

        Assert.Equal(RestriccionesNuevas.Length, (await ObtenerRestriccionesNuevasAsync(contexto)).Count);
        Assert.Equal(1, await contexto.Clientes.CountAsync());
    }

    // Si la base ya tiene un dato que viola una restricción nueva, la migración tiene que
    // fallar en lugar de aplicarse a medias o borrar el dato.
    [FactConSqlServer]
    public async Task Migracion_ConUnClienteQueNoCumple_NoSeAplica()
    {
        using EopDbContext contexto = CrearContexto();
        IMigrator migrador = contexto.GetService<IMigrator>();
        await migrador.MigrateAsync(MigracionAnterior);

        await contexto.Database.ExecuteSqlRawAsync(
            "INSERT INTO Clientes (Nombre, Rut, CorreoContacto, CorreoFacturacion, NumeroWhatsApp, Activo) " +
            "VALUES (N'Viejo', N'21552175-001', NULL, NULL, N'+59899123456', 1)");

        await Assert.ThrowsAnyAsync<Exception>(() => contexto.Database.MigrateAsync());

        Assert.Contains(MigracionAnterior, (await contexto.Database.GetAppliedMigrationsAsync()).Last());
        Assert.Equal(1, await contexto.Clientes.CountAsync());
    }
}
