using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;

namespace EOP.Gestion.Tests.Unitarias;

// Pruebas del caso de uso que, después del login con Microsoft, decide qué
// usuario de EOP es la persona que entró (US-042 / RF-69).
// No usamos base de datos: el repositorio es una lista en memoria que
// imita las mismas reglas que el repositorio real.
public class IdentificarOVincularUsuarioTests
{
    private const string ObjectIdJuan = "11111111-1111-1111-1111-111111111111";
    private const string ObjectIdOtro = "22222222-2222-2222-2222-222222222222";

    // Simulamos el repositorio guardando los usuarios en una lista.
    private class RepositorioUsuariosFalso : IRepositorioUsuarios
    {
        public List<Usuario> Usuarios { get; } = [];

        // Si es true, la vinculación devuelve false como si ninguna fila se hubiera actualizado.
        public bool VinculacionDevuelveFalse { get; set; }

        // Si no es null, la vinculación lanza esta excepción (por ejemplo, se cayó la base).
        public Exception? ErrorAlVincular { get; set; }

        public int CantidadDeVinculacionesIntentadas { get; private set; }

        // Permiten simular que "otro proceso" cambia los datos justo antes o
        // justo después del UPDATE (dos inicios de sesión a la vez, un
        // Administrador que inactiva al usuario, etc.).
        public Action? AntesDeBuscarPorCorreo { get; set; }
        public Action? AntesDeVincular { get; set; }
        public Action? DespuesDeVincular { get; set; }

        // El cambio de rol (T14) no se usa en las pruebas de este repositorio.
        public Task<IBloqueoAdministradores> BloquearAdministradoresAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task ActualizarRolAsync(int usuarioId, RolUsuario nuevoRol, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> AgregarSiCorreoDisponibleAsync(Usuario usuario, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Usuario?> ObtenerPorIdentidadExternaAsync(string entraObjectId, CancellationToken cancellationToken = default)
        {
            var usuario = Usuarios.SingleOrDefault(u => u.EntraObjectId == entraObjectId);
            return Task.FromResult(Copiar(usuario));
        }

        public Task<Usuario?> ObtenerPorCorreoCorporativoAsync(string correoCorporativo, CancellationToken cancellationToken = default)
        {
            AntesDeBuscarPorCorreo?.Invoke();

            var usuario = Usuarios.SingleOrDefault(u => u.CorreoCorporativo == correoCorporativo);
            return Task.FromResult(Copiar(usuario));
        }

        public Task<bool> VincularIdentidadSiNoVinculadaAsync(int usuarioId, string entraObjectId, CancellationToken cancellationToken = default)
        {
            CantidadDeVinculacionesIntentadas++;

            if (ErrorAlVincular != null)
            {
                throw ErrorAlVincular;
            }

            if (VinculacionDevuelveFalse)
            {
                return Task.FromResult(false);
            }

            AntesDeVincular?.Invoke();

            // Mismas condiciones que el UPDATE del repositorio real.
            var usuario = Usuarios.SingleOrDefault(u => u.Id == usuarioId && u.EntraObjectId == null && u.Activo);

            if (usuario == null)
            {
                return Task.FromResult(false);
            }

            usuario.EntraObjectId = entraObjectId;
            DespuesDeVincular?.Invoke();
            return Task.FromResult(true);
        }

        // El repositorio real devuelve objetos nuevos (AsNoTracking), así que
        // devolvemos copias: modificar el resultado no cambia lo "guardado".
        private static Usuario? Copiar(Usuario? usuario)
        {
            if (usuario == null)
            {
                return null;
            }

            return new Usuario
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                CorreoCorporativo = usuario.CorreoCorporativo,
                Rol = usuario.Rol,
                Activo = usuario.Activo,
                EntraObjectId = usuario.EntraObjectId
            };
        }
    }

    private static Usuario CrearUsuario(int id = 1, string correo = "juan.perez@eop.com.uy", bool activo = true, string? entraObjectId = null)
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

    [Fact]
    public async Task UsuarioVinculadoYActivo_SeObtienePorEntraObjectId()
    {
        // Juan ya inició sesión antes, así que tiene su identificador guardado.
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal(ObjectIdJuan, resultado.EntraObjectId);

        // Ya estaba vinculado: no hacía falta vincular de nuevo.
        Assert.Equal(0, repositorio.CantidadDeVinculacionesIntentadas);
    }

    [Fact]
    public async Task UsuarioNoVinculadoYActivo_SeEncuentraPorCorreoYQuedaVinculado()
    {
        // Primer acceso de Juan: existe en EOP pero todavía sin identificador.
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        // Devuelve a Juan, ya con el identificador puesto.
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal(ObjectIdJuan, resultado.EntraObjectId);

        // Y la vinculación quedó guardada en el repositorio.
        Assert.Equal(ObjectIdJuan, repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task CorreoConEspaciosAlrededor_SeRecortaYSeVincula()
    {
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "  juan.perez@eop.com.uy  ");

        Assert.NotNull(resultado);
        Assert.Equal(ObjectIdJuan, repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task UsuarioInexistente_DevuelveNull()
    {
        // En EOP solo existe Juan; entra alguien con otro correo.
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdOtro, "desconocido@eop.com.uy");

        Assert.Null(resultado);

        // A Juan no se le tocó nada.
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task UsuarioInactivoSinVincular_NoSeAceptaNiSeVincula()
    {
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario(activo: false));
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.Null(resultado);
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
        Assert.Equal(0, repositorio.CantidadDeVinculacionesIntentadas);
    }

    [Fact]
    public async Task UsuarioInactivoYaVinculado_NoSeAcepta()
    {
        // Juan estuvo activo y se vinculó, pero después lo inactivaron.
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario(activo: false, entraObjectId: ObjectIdJuan));
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task UsuarioVinculadoAOtroIdentificador_NoSeSobrescribeSuVinculacion()
    {
        // El correo de Juan ya está vinculado a otra identidad de Microsoft.
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdOtro));
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        // Entra una identidad distinta que dice tener el mismo correo.
        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.Null(resultado);

        // La vinculación original sigue intacta.
        Assert.Equal(ObjectIdOtro, repositorio.Usuarios.Single().EntraObjectId);
        Assert.Equal(0, repositorio.CantidadDeVinculacionesIntentadas);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IdentificadorExternoVacioONulo_SeRechaza(string? entraObjectId)
    {
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        // El parámetro no admite null en la firma, pero Program.cs podría
        // recibirlo de Microsoft; el "!" fuerza el caso para probarlo igual.
        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            entraObjectId!, "juan.perez@eop.com.uy");

        Assert.Null(resultado);

        // No se vinculó al usuario con un identificador vacío.
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
        Assert.Equal(0, repositorio.CantidadDeVinculacionesIntentadas);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CorreoAusenteEnPrimeraVinculacion_SeRechaza(string? correo)
    {
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, correo);

        Assert.Null(resultado);
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task VinculacionQueNoActualizaNingunaFila_NoSeConsideraExitosa()
    {
        // Simulamos que entre la búsqueda y el UPDATE otro proceso cambió al
        // usuario, y el repositorio responde que no actualizó nada.
        var repositorio = new RepositorioUsuariosFalso { VinculacionDevuelveFalse = true };
        repositorio.Usuarios.Add(CrearUsuario());
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.Null(resultado);
        Assert.Equal(1, repositorio.CantidadDeVinculacionesIntentadas);
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task ErrorDelRepositorioAlVincular_NoDevuelveUsuarioYPropagaElError()
    {
        // Simulamos que la base de datos falla durante la vinculación.
        var repositorio = new RepositorioUsuariosFalso
        {
            ErrorAlVincular = new InvalidOperationException("Fallo simulado de la base de datos")
        };
        repositorio.Usuarios.Add(CrearUsuario());
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        // El caso de uso no atrapa el error: lo deja subir a quien lo llamó,
        // así que nunca llega a devolver un usuario "vinculado".
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
                ObjectIdJuan, "juan.perez@eop.com.uy"));

        Assert.Equal("Fallo simulado de la base de datos", error.Message);
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task UsuarioVinculado_SeIdentificaPorEntraObjectIdAunqueSuCorreoHayaCambiado()
    {
        // Juan se vinculó con su correo viejo y después el Administrador
        // le actualizó el correo en EOP (RF-76).
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario(correo: "juan.nuevo@eop.com.uy", entraObjectId: ObjectIdJuan));
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        // Microsoft todavía informa el correo anterior.
        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.viejo@eop.com.uy");

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal("juan.nuevo@eop.com.uy", resultado.CorreoCorporativo);
    }

    [Fact]
    public async Task DosIniciosDeSesionSimultaneosDeLaMismaPersona_ElSegundoTambienEntra()
    {
        // Juan abre dos pestañas a la vez en su primer acceso. La otra pestaña
        // lo vincula justo antes que esta, así que este UPDATE no actualiza nada.
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        repositorio.AntesDeVincular = () => repositorio.Usuarios[0].EntraObjectId = ObjectIdJuan;
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        // Igual se lo reconoce, porque quedó vinculado con SU identificador.
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal(ObjectIdJuan, resultado.EntraObjectId);
    }

    [Fact]
    public async Task LaOtraPestanaVinculaJustoEntreLasDosBusquedas_ElSegundoInicioTambienEntra()
    {
        // La búsqueda por Object ID no encontró a nadie, pero antes de buscar por correo la otra pestaña
        // lo vinculó con ESTE mismo identificador. Es la misma persona: no hay motivo para rechazarla.
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        repositorio.AntesDeBuscarPorCorreo = () => repositorio.Usuarios[0].EntraObjectId = ObjectIdJuan;
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
    }

    [Fact]
    public async Task OtraIdentidadVinculaJustoEntreLasDosBusquedas_NoSeAceptaNiSePisa()
    {
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        repositorio.AntesDeBuscarPorCorreo = () => repositorio.Usuarios[0].EntraObjectId = ObjectIdOtro;
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.Null(resultado);
        Assert.Equal(ObjectIdOtro, repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task VinculacionSimultaneaConOtraIdentidad_NoSeAceptaNiSePisa()
    {
        // Mientras esta solicitud iba a vincular, otra identidad de Microsoft
        // con el mismo correo se vinculó primero.
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        repositorio.AntesDeVincular = () => repositorio.Usuarios[0].EntraObjectId = ObjectIdOtro;
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.Null(resultado);
        Assert.Equal(ObjectIdOtro, repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task UsuarioInactivadoJustoDespuesDeVincularse_NoSeAcepta()
    {
        // Un Administrador inactiva a Juan en el mismo instante en que se vinculaba.
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario());
        repositorio.DespuesDeVincular = () => repositorio.Usuarios[0].Activo = false;
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task UsuarioVinculado_SeIdentificaAunqueMicrosoftNoEnvieCorreo()
    {
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var casoDeUso = new IdentificarOVincularUsuario(repositorio);

        Usuario? resultado = await casoDeUso.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
            ObjectIdJuan, null);

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
    }
}
