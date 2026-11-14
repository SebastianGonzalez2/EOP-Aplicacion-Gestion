
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Usuarios;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

public class RegistrarPresupuestoTests
{
    private const string IdentidadMicrosoft = "entra-usuario-prueba";

    // Simulamos el guardado para probar las reglas del caso de uso
    // sin necesitar una secuencia de SQL Server.
    private class RepositorioPresupuestosFalso : IRepositorioPresupuestos
    {
        public Presupuesto? PresupuestoGuardado { get; private set; }

        public Task AgregarConCodigoAsignadoAsync(
            Presupuesto presupuesto,
            CancellationToken cancellationToken = default)
        {
            presupuesto.NumeroCorrelativo = 1;
            presupuesto.Codigo = GeneradorCodigoPresupuesto.GenerarDesdeFechaUtc(
                presupuesto.FechaCreacion,
                presupuesto.NombreProyecto,
                1);

            PresupuestoGuardado = presupuesto;

            return Task.CompletedTask;
        }
    }

    // Preparamos una base SQLite temporal con los datos necesarios.
    private static async Task<(
        SqliteConnection Conexion,
        EopDbContext Contexto,
        int ClienteId,
        int TipoProyectoId,
        int UsuarioId)> PrepararDatosAsync()
    {
        var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();

        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(conexion)
            .Options;

        var contexto = new EopDbContext(opciones);
        await contexto.Database.EnsureCreatedAsync();

        var cliente = new Cliente
        {
            Nombre = "Cliente de prueba",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456",
            Activo = true
        };

        var usuario = new Usuario
        {
            NombreCompleto = "Ana Pereira",
            CorreoCorporativo = "ana@eop.com.uy",
            EntraObjectId = IdentidadMicrosoft,
            Rol = RolUsuario.Operador,
            Activo = true
        };

        contexto.Clientes.Add(cliente);
        contexto.Usuarios.Add(usuario);

        await contexto.SaveChangesAsync();

        // Utilizamos uno de los tipos provisorios ya configurados.
        int tipoProyectoId = await contexto.TiposProyecto
            .Where(tipo => tipo.Nombre == "Obra")
            .Select(tipo => tipo.Id)
            .SingleAsync();

        return (conexion, contexto, cliente.Id, tipoProyectoId, usuario.Id);
    }

    private static RegistrarPresupuesto CrearCasoDeUso(
        EopDbContext contexto,
        RepositorioPresupuestosFalso repositorio)
    {
        return new RegistrarPresupuesto(
            repositorio,
            new ConsultaReferenciasPresupuesto(contexto),
            new RepositorioUsuarios(contexto),
            TimeProvider.System);
    }

    private static Presupuesto CrearPresupuestoValido(
        int clienteId,
        int tipoProyectoId)
    {
        return new Presupuesto
        {
            NombreProyecto = "Instalación solar",
            ClienteId = clienteId,
            TipoProyectoId = tipoProyectoId,
            PlazoVigenciaDias = 30
        };
    }

    [Fact]
    public async Task DatosValidos_GuardaPresupuestoConResponsableYEstadoBorrador()
    {
        var datos = await PrepararDatosAsync();
        using var conexion = datos.Conexion;
        using var contexto = datos.Contexto;

        var repositorio = new RepositorioPresupuestosFalso();
        var casoDeUso = CrearCasoDeUso(contexto, repositorio);

        var presupuesto = CrearPresupuestoValido(
            datos.ClienteId,
            datos.TipoProyectoId);

        presupuesto.NombreProyecto = "  Instalación solar  ";

        // Simulamos datos que no deben poder determinarse desde el formulario.
        presupuesto.ResponsableId = 99999;
        presupuesto.Estado = EstadoPresupuesto.Aprobado;

        var errores = await casoDeUso.EjecutarAsync(
            presupuesto,
            IdentidadMicrosoft);

        Assert.Empty(errores);
        Assert.Same(presupuesto, repositorio.PresupuestoGuardado);
        Assert.Equal(datos.UsuarioId, presupuesto.ResponsableId);
        Assert.Equal(EstadoPresupuesto.Borrador, presupuesto.Estado);
        Assert.Equal("Instalación solar", presupuesto.NombreProyecto);
        Assert.StartsWith("PRES-", presupuesto.Codigo);
        Assert.NotEqual(default, presupuesto.FechaCreacion);
    }

    // FIX-UTC-FECHAS: la fecha de creación se guarda en UTC y el código usa el día de Uruguay.
    [Fact]
    public async Task FechaDeCreacion_SeGuardaEnUtcYElCodigoUsaElDiaDeUruguay()
    {
        var datos = await PrepararDatosAsync();
        using var conexion = datos.Conexion;
        using var contexto = datos.Contexto;

        // 22:00 del 14/11 en Uruguay = 01:00 UTC del 15/11.
        var instante = new DateTimeOffset(2026, 11, 15, 1, 0, 0, TimeSpan.Zero);
        var repositorio = new RepositorioPresupuestosFalso();
        var casoDeUso = new RegistrarPresupuesto(
            repositorio,
            new ConsultaReferenciasPresupuesto(contexto),
            new RepositorioUsuarios(contexto),
            new RelojFijo(instante));
        var presupuesto = CrearPresupuestoValido(datos.ClienteId, datos.TipoProyectoId);

        var errores = await casoDeUso.EjecutarAsync(presupuesto, IdentidadMicrosoft);

        Assert.Empty(errores);
        Assert.Equal(new DateTime(2026, 11, 15, 1, 0, 0), presupuesto.FechaCreacion);
        Assert.Equal(DateTimeKind.Utc, presupuesto.FechaCreacion.Kind);

        Assert.Equal("PRES-20261114-INSTALACIÓN SOLAR-0001", presupuesto.Codigo);
    }

    [Fact]
    public async Task DatosObligatoriosInvalidos_NoGuardaPresupuesto()
    {
        var datos = await PrepararDatosAsync();
        using var conexion = datos.Conexion;
        using var contexto = datos.Contexto;

        var repositorio = new RepositorioPresupuestosFalso();
        var casoDeUso = CrearCasoDeUso(contexto, repositorio);

        var presupuesto = new Presupuesto
        {
            NombreProyecto = "   ",
            ClienteId = 0,
            TipoProyectoId = 0,
            PlazoVigenciaDias = 0
        };

        var errores = await casoDeUso.EjecutarAsync(
            presupuesto,
            IdentidadMicrosoft);

        Assert.Equal(4, errores.Count);
        Assert.Null(repositorio.PresupuestoGuardado);
    }

    [Fact]
    public async Task ClienteInactivo_NoGuardaPresupuesto()
    {
        var datos = await PrepararDatosAsync();
        using var conexion = datos.Conexion;
        using var contexto = datos.Contexto;

        // Inactivamos al cliente antes de intentar crear el presupuesto.
        var cliente = await contexto.Clientes.SingleAsync();
        cliente.Activo = false;
        await contexto.SaveChangesAsync();

        var repositorio = new RepositorioPresupuestosFalso();
        var casoDeUso = CrearCasoDeUso(contexto, repositorio);

        var presupuesto = CrearPresupuestoValido(
            datos.ClienteId,
            datos.TipoProyectoId);

        var errores = await casoDeUso.EjecutarAsync(
            presupuesto,
            IdentidadMicrosoft);

        Assert.NotEmpty(errores);
        Assert.Null(repositorio.PresupuestoGuardado);
    }

    [Fact]
    public async Task TipoProyectoInexistente_NoGuardaPresupuesto()
    {
        var datos = await PrepararDatosAsync();
        using var conexion = datos.Conexion;
        using var contexto = datos.Contexto;

        var repositorio = new RepositorioPresupuestosFalso();
        var casoDeUso = CrearCasoDeUso(contexto, repositorio);

        var presupuesto = CrearPresupuestoValido(
            datos.ClienteId,
            999999);

        var errores = await casoDeUso.EjecutarAsync(
            presupuesto,
            IdentidadMicrosoft);

        Assert.NotEmpty(errores);
        Assert.Null(repositorio.PresupuestoGuardado);
    }

    [Fact]
    public async Task UsuarioNoHabilitado_NoGuardaPresupuesto()
    {
        var datos = await PrepararDatosAsync();
        using var conexion = datos.Conexion;
        using var contexto = datos.Contexto;

        var usuario = await contexto.Usuarios.SingleAsync();
        usuario.Activo = false;
        await contexto.SaveChangesAsync();

        var repositorio = new RepositorioPresupuestosFalso();
        var casoDeUso = CrearCasoDeUso(contexto, repositorio);

        var presupuesto = CrearPresupuestoValido(
            datos.ClienteId,
            datos.TipoProyectoId);

        var errores = await casoDeUso.EjecutarAsync(
            presupuesto,
            IdentidadMicrosoft);

        Assert.NotEmpty(errores);
        Assert.Null(repositorio.PresupuestoGuardado);
    }
}
