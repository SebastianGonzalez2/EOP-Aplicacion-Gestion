using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S2-T23: la entidad Presupuesto, el código interno y el correlativo global contra un SQL Server
// real. Las restricciones se prueban guardando directo con el contexto, a propósito: lo que se
// verifica es la defensa de la base si algún camino se saltea las validaciones. Las secuencias
// y los bloqueos de concurrencia solo existen en SQL Server, por eso no alcanza con SQLite.
public class PresupuestosSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    private int _clienteId;
    private int _responsableId;
    private int _tipoProyectoId;

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();

        var cliente = new Cliente
        {
            Nombre = "Cliente de prueba",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456"
        };
        var responsable = new Usuario
        {
            NombreCompleto = "Ana Pereira",
            CorreoCorporativo = "ana@eop.com.uy",
            Rol = RolUsuario.Operador,
            Activo = true
        };
        // La lista real de tipos de proyecto está pendiente de validar con EOP: el tipo es de prueba.
        var tipoProyecto = new TipoProyecto { Nombre = "Tipo de prueba" };

        contexto.AddRange(cliente, responsable, tipoProyecto);
        await contexto.SaveChangesAsync();

        _clienteId = cliente.Id;
        _responsableId = responsable.Id;
        _tipoProyectoId = tipoProyecto.Id;
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

    private Presupuesto CrearPresupuesto(string nombreProyecto = "Instalación solar") => new()
    {
        NombreProyecto = nombreProyecto,
        ClienteId = _clienteId,
        TipoProyectoId = _tipoProyectoId,
        PlazoVigenciaDias = 30,
        ResponsableId = _responsableId,
        FechaCreacion = new DateTime(2026, 11, 9, 15, 30, 0)
    };

    // Un presupuesto con código y correlativo ya puestos, para guardar directo sin pasar por el repositorio.
    private Presupuesto CrearPresupuestoConCodigo(string codigo = "PRES-20261109-OBRA-0001", int correlativo = 1)
    {
        Presupuesto presupuesto = CrearPresupuesto("Obra");
        presupuesto.Codigo = codigo;
        presupuesto.NumeroCorrelativo = correlativo;

        return presupuesto;
    }

    private async Task<Presupuesto> AgregarConContextoNuevoAsync(Presupuesto presupuesto)
    {
        using EopDbContext contexto = CrearContexto();
        await new RepositorioPresupuestos(contexto).AgregarConCodigoAsignadoAsync(presupuesto);

        return presupuesto;
    }

    private async Task<int> ContarAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.Presupuestos.CountAsync();
    }

    // Mira el motivo además del rechazo: sin eso, la prueba podría pasar porque falló otra restricción.
    private async Task ComprobarQueLaBaseRechazaAsync(Presupuesto presupuesto, string motivoEsperado)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Presupuestos.Add(presupuesto);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains(motivoEsperado, excepcion.GetBaseException().Message);
        Assert.Equal(0, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task PresupuestoValido_SeGuardaEnBorradorConSuCodigoYSuResponsable()
    {
        Presupuesto guardado = await AgregarConContextoNuevoAsync(CrearPresupuesto());

        using EopDbContext contexto = CrearContexto();
        Presupuesto leido = await contexto.Presupuestos.SingleAsync();

        Assert.Equal(guardado.Id, leido.Id);
        Assert.Equal(EstadoPresupuesto.Borrador, leido.Estado);
        Assert.Equal("PRES-20261109-INSTALACIÓN SOLAR-0001", leido.Codigo);
        Assert.Equal(1, leido.NumeroCorrelativo);
        Assert.Equal(_responsableId, leido.ResponsableId);
        Assert.Equal(_clienteId, leido.ClienteId);
        Assert.Equal(_tipoProyectoId, leido.TipoProyectoId);
        Assert.Equal(30, leido.PlazoVigenciaDias);
        Assert.Equal(new DateTime(2026, 11, 9, 15, 30, 0), leido.FechaCreacion);
    }

    [FactConSqlServer]
    public async Task ElCorrelativoEsGlobal_SigueAunqueCambienFechaClienteTipoONombre()
    {
        var otroCliente = new Cliente { Nombre = "Otro cliente", Rut = "215521750025", CorreoContacto = "otro@ejemplo.com", NumeroWhatsApp = "+59899234567" };
        var otroTipo = new TipoProyecto { Nombre = "Otro tipo de prueba" };
        using (EopDbContext contexto = CrearContexto())
        {
            contexto.AddRange(otroCliente, otroTipo);
            await contexto.SaveChangesAsync();
        }

        Presupuesto primero = await AgregarConContextoNuevoAsync(CrearPresupuesto("Obra norte"));

        Presupuesto segundo = CrearPresupuesto("Obra norte");
        segundo.FechaCreacion = new DateTime(2026, 11, 10);
        await AgregarConContextoNuevoAsync(segundo);

        Presupuesto tercero = CrearPresupuesto("Obra sur");
        tercero.ClienteId = otroCliente.Id;
        tercero.TipoProyectoId = otroTipo.Id;
        tercero.FechaCreacion = new DateTime(2027, 1, 2);
        await AgregarConContextoNuevoAsync(tercero);

        Assert.Equal([1, 2, 3], [primero.NumeroCorrelativo, segundo.NumeroCorrelativo, tercero.NumeroCorrelativo]);
        Assert.EndsWith("-0003", tercero.Codigo);
        Assert.StartsWith("PRES-20270102-OBRA SUR-", tercero.Codigo);
    }

    [FactConSqlServer]
    public async Task ElCodigoNoCambiaAlModificarDespuesElPresupuesto()
    {
        Presupuesto guardado = await AgregarConContextoNuevoAsync(CrearPresupuesto("Nombre original"));
        string codigoOriginal = guardado.Codigo;

        using (EopDbContext contexto = CrearContexto())
        {
            Presupuesto leido = await contexto.Presupuestos.SingleAsync();
            leido.NombreProyecto = "Nombre modificado";
            leido.PlazoVigenciaDias = 45;
            await contexto.SaveChangesAsync();
        }

        using EopDbContext contextoDeLectura = CrearContexto();
        Assert.Equal(codigoOriginal, (await contextoDeLectura.Presupuestos.SingleAsync()).Codigo);
    }

    [FactConSqlServer]
    public async Task NombreDe200Caracteres_EntraEnLaColumnaYEnElCodigo()
    {
        Presupuesto guardado = await AgregarConContextoNuevoAsync(CrearPresupuesto(new string('a', 200)));

        // "PRES-" + fecha de 8 + "-" + 200 + "-" + 4 cifras.
        Assert.Equal(219, guardado.Codigo.Length);
    }

    [FactConSqlServer]
    public async Task NombreDe201Caracteres_LoRechazaLaBase()
    {
        Presupuesto presupuesto = CrearPresupuestoConCodigo();
        presupuesto.NombreProyecto = new string('a', 201);

        await ComprobarQueLaBaseRechazaAsync(presupuesto, "truncated");
    }

    [FactConSqlServer]
    public async Task CodigoRepetido_LoRechazaElIndiceUnico()
    {
        await GuardarDirectoAsync(CrearPresupuestoConCodigo("PRES-20261109-OBRA-0001", 1));

        using EopDbContext contexto = CrearContexto();
        contexto.Presupuestos.Add(CrearPresupuestoConCodigo("PRES-20261109-OBRA-0001", 2));

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("IX_Presupuestos_Codigo", excepcion.GetBaseException().Message);
        Assert.Equal(1, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task CorrelativoRepetido_LoRechazaElIndiceUnico()
    {
        await GuardarDirectoAsync(CrearPresupuestoConCodigo("PRES-20261109-OBRA-0001", 1));

        using EopDbContext contexto = CrearContexto();
        contexto.Presupuestos.Add(CrearPresupuestoConCodigo("PRES-20261110-OTRA-0001", 1));

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("IX_Presupuestos_NumeroCorrelativo", excepcion.GetBaseException().Message);
        Assert.Equal(1, await ContarAsync());
    }

    private async Task GuardarDirectoAsync(Presupuesto presupuesto)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Presupuestos.Add(presupuesto);
        await contexto.SaveChangesAsync();
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    public async Task EstadoQueNoEsUnoDeLosOcho_LoRechazaLaBase(int estado)
    {
        Presupuesto presupuesto = CrearPresupuestoConCodigo();
        presupuesto.Estado = (EstadoPresupuesto)estado;

        await ComprobarQueLaBaseRechazaAsync(presupuesto, "CK_Presupuestos_EstadoValido");
    }

    [TheoryConSqlServer]
    [InlineData(EstadoPresupuesto.Borrador)]
    [InlineData(EstadoPresupuesto.ParaRevisionInterna)]
    [InlineData(EstadoPresupuesto.EnviadoPendienteDeRespuestaDelCliente)]
    [InlineData(EstadoPresupuesto.Aprobado)]
    [InlineData(EstadoPresupuesto.Rechazado)]
    [InlineData(EstadoPresupuesto.SolicitudDeModificacionDelCliente)]
    [InlineData(EstadoPresupuesto.Vencido)]
    [InlineData(EstadoPresupuesto.Cancelado)]
    public async Task CadaUnoDeLosOchoEstados_SePuedeGuardar(EstadoPresupuesto estado)
    {
        Presupuesto presupuesto = CrearPresupuestoConCodigo();
        presupuesto.Estado = estado;

        await GuardarDirectoAsync(presupuesto);

        using EopDbContext contexto = CrearContexto();
        Assert.Equal(estado, (await contexto.Presupuestos.SingleAsync()).Estado);
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task PlazoDeVigenciaQueNoEsPositivo_LoRechazaLaBase(int plazo)
    {
        Presupuesto presupuesto = CrearPresupuestoConCodigo();
        presupuesto.PlazoVigenciaDias = plazo;

        await ComprobarQueLaBaseRechazaAsync(presupuesto, "CK_Presupuestos_PlazoVigenciaPositivo");
    }

    [TheoryConSqlServer]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NombreDelProyectoEnBlanco_LoRechazaLaBase(string nombre)
    {
        Presupuesto presupuesto = CrearPresupuestoConCodigo();
        presupuesto.NombreProyecto = nombre;

        await ComprobarQueLaBaseRechazaAsync(presupuesto, "CK_Presupuestos_NombreProyectoNoVacio");
    }

    [TheoryConSqlServer]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CodigoEnBlanco_LoRechazaLaBase(string codigo)
    {
        Presupuesto presupuesto = CrearPresupuestoConCodigo();
        presupuesto.Codigo = codigo;

        await ComprobarQueLaBaseRechazaAsync(presupuesto, "CK_Presupuestos_CodigoNoVacio");
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(10000)]
    public async Task CorrelativoFueraDeCuatroCifras_LoRechazaLaBase(int correlativo)
    {
        Presupuesto presupuesto = CrearPresupuestoConCodigo(correlativo: correlativo);

        await ComprobarQueLaBaseRechazaAsync(presupuesto, "CK_Presupuestos_CorrelativoDeCuatroCifras");
    }

    [TheoryConSqlServer]
    [InlineData("cliente")]
    [InlineData("tipo")]
    [InlineData("responsable")]
    public async Task RelacionConUnRegistroInexistente_LoRechazaLaClaveForanea(string relacion)
    {
        Presupuesto presupuesto = CrearPresupuesto();
        if (relacion == "cliente")
        {
            presupuesto.ClienteId = 999_999;
        }
        else if (relacion == "tipo")
        {
            presupuesto.TipoProyectoId = 999_999;
        }
        else
        {
            presupuesto.ResponsableId = 999_999;
        }

        using EopDbContext contexto = CrearContexto();
        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(
            () => new RepositorioPresupuestos(contexto).AgregarConCodigoAsignadoAsync(presupuesto));

        Assert.Contains("FK_Presupuestos_", excepcion.GetBaseException().Message);
        Assert.Equal(0, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task ClienteConPresupuestos_NoSePuedeBorrarFisicamente()
    {
        await AgregarConContextoNuevoAsync(CrearPresupuesto());

        using EopDbContext contexto = CrearContexto();
        Cliente cliente = await contexto.Clientes.SingleAsync();
        contexto.Clientes.Remove(cliente);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("FK_Presupuestos_Clientes_ClienteId", excepcion.GetBaseException().Message);
    }

    [FactConSqlServer]
    public async Task SiElGuardadoFalla_ElCorrelativoUsadoNoSeReutiliza()
    {
        // Es el comportamiento de una secuencia: el número se pidió antes de que fallara el guardado,
        // así que se pierde. Lo importante es que el siguiente presupuesto nunca repita uno ya entregado.
        Presupuesto conClienteInexistente = CrearPresupuesto();
        conClienteInexistente.ClienteId = 999_999;
        using (EopDbContext contexto = CrearContexto())
        {
            await Assert.ThrowsAsync<DbUpdateException>(
                () => new RepositorioPresupuestos(contexto).AgregarConCodigoAsignadoAsync(conClienteInexistente));
        }

        Presupuesto siguiente = await AgregarConContextoNuevoAsync(CrearPresupuesto());

        Assert.Equal(2, siguiente.NumeroCorrelativo);
        Assert.Equal(1, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task MuchasAltasSimultaneas_RecibenCorrelativosYCodigosTodosDistintos()
    {
        const int cantidadDeAltas = 25;
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        List<Task<Presupuesto>> altas = Enumerable.Range(0, cantidadDeAltas)
            .Select(_ => Task.Run(async () =>
            {
                await largada.Task;
                return await AgregarConContextoNuevoAsync(CrearPresupuesto("Misma obra"));
            }))
            .ToList();

        largada.SetResult();
        Presupuesto[] guardados = await Task.WhenAll(altas);

        // Sin saltos ni repetidos: del 1 al 25, cada número una sola vez.
        Assert.Equal(
            Enumerable.Range(1, cantidadDeAltas),
            guardados.Select(p => p.NumeroCorrelativo).Order());
        Assert.Equal(cantidadDeAltas, guardados.Select(p => p.Codigo).Distinct().Count());
        Assert.Equal(cantidadDeAltas, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task ElUltimoCorrelativoDeCuatroCifrasEs9999_YDespuesLaSecuenciaSeAgota()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Database.ExecuteSqlRawAsync($"ALTER SEQUENCE [{EopDbContext.NombreSecuenciaPresupuestos}] RESTART WITH 9999");
        }

        Presupuesto ultimo = await AgregarConContextoNuevoAsync(CrearPresupuesto());
        Assert.Equal(9999, ultimo.NumeroCorrelativo);
        Assert.EndsWith("-9999", ultimo.Codigo);

        // No es cíclica: en vez de repetir el 1, SQL Server da error y no se guarda nada más.
        using EopDbContext contextoSiguiente = CrearContexto();
        SqlException excepcion = await Assert.ThrowsAsync<SqlException>(
            () => new RepositorioPresupuestos(contextoSiguiente).AgregarConCodigoAsignadoAsync(CrearPresupuesto()));

        Assert.Contains(EopDbContext.NombreSecuenciaPresupuestos, excepcion.Message);
        Assert.Equal(1, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task TiposDeProyecto_NoAdmitenNombresRepetidosNiEnBlanco()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            contexto.TiposProyecto.Add(new TipoProyecto { Nombre = "TIPO DE PRUEBA" });
            DbUpdateException repetido = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
            Assert.Contains("IX_TiposProyecto_Nombre", repetido.GetBaseException().Message);
        }

        using EopDbContext otroContexto = CrearContexto();
        otroContexto.TiposProyecto.Add(new TipoProyecto { Nombre = "   " });
        DbUpdateException enBlanco = await Assert.ThrowsAsync<DbUpdateException>(() => otroContexto.SaveChangesAsync());
        Assert.Contains("CK_TiposProyecto_NombreNoVacio", enBlanco.GetBaseException().Message);
    }

    [FactConSqlServer]
    public async Task LaTablaDeTiposDeProyecto_NoTraeValoresInventados()
    {
        // La lista definitiva está pendiente de validar con EOP (RF-01): la migración no carga ninguno.
        // Acá solo existe el tipo que sembró esta prueba.
        using EopDbContext contexto = CrearContexto();

        Assert.Equal(["Tipo de prueba"], await contexto.TiposProyecto.Select(t => t.Nombre).ToListAsync());
    }
}
