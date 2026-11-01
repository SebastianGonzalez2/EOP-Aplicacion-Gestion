using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Tests.Compartido;

namespace EOP.Gestion.Tests.Unitarias;

// Pruebas del caso de uso de la T11 que responde una sola pregunta:
// "¿la persona dueña de esta sesión sigue siendo un usuario habilitado de EOP?"
// (US-042: "Un usuario Inactivo no podrá acceder"; RF-69: "Solamente los
// usuarios habilitados ... deberán poder acceder al sistema").
//
// Se ejecuta en CADA pedido de alguien que ya tiene la cookie de sesión, así
// que tiene que ser estricto: ante la duda, la respuesta es "no".
//
// No hay base de datos: el repositorio es una lista en memoria que además
// anota cómo lo llamaron, para comprobar QUÉ le pidió el caso de uso.
public class VerificarUsuarioHabilitadoTests
{
    private const string ObjectIdJuan = "11111111-1111-1111-1111-111111111111";
    private const string ObjectIdAna = "22222222-2222-2222-2222-222222222222";
    private const string ObjectIdSinUsuarioEnEop = "99999999-9999-9999-9999-999999999999";

    // ---------- Usuarios habilitados y no habilitados ----------

    [Fact]
    public async Task UsuarioRegistradoVinculadoYActivo_EstaHabilitado()
    {
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan, activo: true));
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        bool estaHabilitado = await verificador.EstaHabilitadoAsync(ObjectIdJuan);

        Assert.True(estaHabilitado);

        // Buscó exactamente por el identificador de la sesión, sin modificarlo.
        Assert.Equal([ObjectIdJuan], repositorio.IdentificadoresConsultados);
    }

    [Fact]
    public async Task UsuarioRegistradoYVinculadoPeroInactivo_NoEstaHabilitado()
    {
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan, activo: false));
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        bool estaHabilitado = await verificador.EstaHabilitadoAsync(ObjectIdJuan);

        Assert.False(estaHabilitado);
    }

    [Fact]
    public async Task IdentidadDeMicrosoftSinUsuarioDeEopVinculado_NoEstaHabilitada()
    {
        // Microsoft conoce a esta persona, pero en EOP nadie tiene ese Object ID.
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        bool estaHabilitado = await verificador.EstaHabilitadoAsync(ObjectIdSinUsuarioEnEop);

        Assert.False(estaHabilitado);
        Assert.Equal([ObjectIdSinUsuarioEnEop], repositorio.IdentificadoresConsultados);
    }

    [Fact]
    public async Task SinNingunUsuarioRegistradoEnEop_NoEstaHabilitado()
    {
        var verificador = new VerificarUsuarioHabilitado(new RepositorioUsuariosQueAnotaLasConsultas());

        Assert.False(await verificador.EstaHabilitadoAsync(ObjectIdJuan));
    }

    [Fact]
    public async Task ObjectIdDeOtroUsuarioActivo_SoloHabilitaAlDuenoDeEseObjectId()
    {
        // Juan está inactivo y Ana activa. La sesión de Juan no puede "colarse"
        // por el hecho de que exista otro usuario activo en EOP.
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(id: 1, correo: "juan.perez@eop.com.uy", entraObjectId: ObjectIdJuan, activo: false));
        repositorio.Usuarios.Add(CrearUsuario(id: 2, correo: "ana.gomez@eop.com.uy", entraObjectId: ObjectIdAna, activo: true));
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        Assert.False(await verificador.EstaHabilitadoAsync(ObjectIdJuan));
        Assert.True(await verificador.EstaHabilitadoAsync(ObjectIdAna));
    }

    [Fact]
    public async Task UsuarioActivoQueTodaviaNoSeVinculo_NoQuedaHabilitadoPorUnaSesionYaIniciada()
    {
        // Juan está activo pero nunca se vinculó (EntraObjectId null). La
        // vinculación por correo solo ocurre al iniciar sesión (T10), nunca al
        // validar una sesión: este caso de uso no busca por correo ni vincula.
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: null, activo: true));
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        bool estaHabilitado = await verificador.EstaHabilitadoAsync(ObjectIdJuan);

        Assert.False(estaHabilitado);
        Assert.Equal(0, repositorio.CantidadDeBusquedasPorCorreo);
        Assert.Equal(0, repositorio.CantidadDeVinculacionesIntentadas);
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task UsuarioHabilitado_LaVerificacionNoBuscaPorCorreoNiModificaLaVinculacion()
    {
        // Validar una sesión es solo una consulta: no tiene que cambiar nada.
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        await verificador.EstaHabilitadoAsync(ObjectIdJuan);

        Assert.Equal(0, repositorio.CantidadDeBusquedasPorCorreo);
        Assert.Equal(0, repositorio.CantidadDeVinculacionesIntentadas);
    }

    // ---------- Identificador externo nulo, vacío o inválido ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public async Task IdentificadorExternoNuloVacioOSoloEspacios_NoEstaHabilitadoYNiSiquieraConsultaLaBase(string? identificadorExterno)
    {
        // Ponemos un usuario activo con ese mismo valor "raro" como Object ID.
        // Si el caso de uso consultara la base igual, lo encontraría y diría
        // que sí: así la prueba distingue "rechazó por el identificador" de
        // "rechazó porque no encontró a nadie".
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: identificadorExterno, activo: true));
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        bool estaHabilitado = await verificador.EstaHabilitadoAsync(identificadorExterno);

        Assert.False(estaHabilitado);
        Assert.Empty(repositorio.IdentificadoresConsultados);
    }

    [Theory]
    [InlineData("no-es-un-guid")]
    [InlineData("11111111-1111-1111-1111-11111111111")]
    [InlineData("' OR 1=1 --")]
    [InlineData(" 11111111-1111-1111-1111-111111111111 ")]
    public async Task IdentificadorExternoConFormatoInvalidoOQueNoCoincideExactamente_NoEstaHabilitado(string identificadorExterno)
    {
        // El caso de uso no valida el formato (un Object ID de Entra es un GUID):
        // lo trata como cualquier valor desconocido. Lo importante es que un
        // valor que no coincide EXACTAMENTE con el vinculado nunca habilita,
        // ni siquiera uno que solo difiere en espacios alrededor.
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan, activo: true));
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        bool estaHabilitado = await verificador.EstaHabilitadoAsync(identificadorExterno);

        Assert.False(estaHabilitado);
    }

    // ---------- Fallas y cancelaciones del repositorio ----------

    [Fact]
    public async Task FallaDelRepositorio_LaExcepcionLlegaAQuienLlamaSinConvertirseEnUnSi()
    {
        // El caso de uso no "traga" los errores: si los atrapara y devolviera
        // false, el manejador de sesiones no podría registrarlos como error
        // (RNF-12). Quien decide qué hacer es el manejador, no el caso de uso.
        var errorDeBaseDeDatos = new InvalidOperationException("La base de datos no responde.");
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas { ErrorAlBuscar = errorDeBaseDeDatos };
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => verificador.EstaHabilitadoAsync(ObjectIdJuan));

        Assert.Same(errorDeBaseDeDatos, excepcion);
    }

    [Fact]
    public async Task TiempoDeEsperaAgotadoEnElRepositorio_LaExcepcionLlegaAQuienLlama()
    {
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas
        {
            ErrorAlBuscar = new TimeoutException("Tiempo de espera agotado.")
        };
        var verificador = new VerificarUsuarioHabilitado(repositorio);

        await Assert.ThrowsAsync<TimeoutException>(() => verificador.EstaHabilitadoAsync(ObjectIdJuan));
    }

    [Fact]
    public async Task TokenDeCancelacion_SeLePasaAlRepositorioSinCambios()
    {
        // ASP.NET Core le pasa al caso de uso el token de la solicitud
        // (RequestAborted). Tiene que llegar hasta EF Core para que la consulta
        // se corte si el navegador se va.
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var verificador = new VerificarUsuarioHabilitado(repositorio);
        using var fuenteDeCancelacion = new CancellationTokenSource();

        await verificador.EstaHabilitadoAsync(ObjectIdJuan, fuenteDeCancelacion.Token);

        Assert.Equal(fuenteDeCancelacion.Token, Assert.Single(repositorio.TokensRecibidos));
    }

    [Fact]
    public async Task SolicitudYaCancelada_LanzaOperationCanceledExceptionEnLugarDeResponder()
    {
        // Igual que EF Core: si la solicitud ya se canceló, no hay respuesta,
        // ni "sí" ni "no". El manejador decide qué hacer con la cancelación.
        var repositorio = new RepositorioUsuariosQueAnotaLasConsultas();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var verificador = new VerificarUsuarioHabilitado(repositorio);
        using var fuenteDeCancelacion = new CancellationTokenSource();
        fuenteDeCancelacion.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => verificador.EstaHabilitadoAsync(ObjectIdJuan, fuenteDeCancelacion.Token));
    }

    // ---------- Ayudas ----------

    private static Usuario CrearUsuario(
        int id = 1,
        string correo = "juan.perez@eop.com.uy",
        bool activo = true,
        string? entraObjectId = null)
    {
        return new Usuario
        {
            Id = id,
            NombreCompleto = "Juan Pérez",
            CorreoCorporativo = correo,
            Rol = RolUsuario.Operador,
            Activo = activo,
            EntraObjectId = entraObjectId
        };
    }
}
