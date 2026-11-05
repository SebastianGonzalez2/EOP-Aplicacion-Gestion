using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;
using EOP.Gestion.Core.ServiciosDominio.Compartidos;

namespace EOP.Gestion.Core.CasosDeUso.Usuarios;

// US-043 / RF-70: el Administrador da de alta un usuario de EOP.
// Solo se crea la fila en nuestra tabla Usuarios: la identidad de Microsoft Entra ID se
// administra afuera de la aplicación y acá no se crea ni se modifica. Que quien pide esto sea
// Administrador lo controla el Controller con [Authorize]; acá van las reglas del negocio.
public class RegistrarUsuario
{
    // El mismo largo que tiene la columna en UsuarioConfiguracion. Si lo dejáramos pasar,
    // SQL Server rechazaría el INSERT y el usuario vería un error genérico en vez de este mensaje.
    private const int LargoMaximoNombreCompleto = 150;

    private const string MensajeCorreoYaRegistrado = "El correo corporativo ya se encuentra registrado.";

    private readonly IRepositorioUsuarios _repositorioUsuarios;

    public RegistrarUsuario(IRepositorioUsuarios repositorioUsuarios)
    {
        _repositorioUsuarios = repositorioUsuarios;
    }

    // Devuelve la lista de errores, igual que RegistrarCliente: lista vacía significa que se guardó.
    //
    // Recibe los datos sueltos y no un Usuario ya armado a propósito. Así, quien llama a este
    // método no tiene forma de elegir el Estado ni el EntraObjectId: el único lugar donde se
    // deciden esos dos valores es acá adentro.
    public async Task<List<string>> EjecutarAsync(string? nombreCompleto, string? correoCorporativo, RolUsuario rol, CancellationToken cancellationToken = default)
    {
        string nombreParaGuardar = QuitarEspaciosDeLosExtremos(nombreCompleto);
        string correoParaGuardar = NormalizarCorreo(correoCorporativo);

        // No cortamos en el primer error para que el usuario vea todos juntos.
        List<string> errores = new();

        ValidarNombreCompleto(nombreParaGuardar, errores);
        await ValidarCorreoCorporativoAsync(correoParaGuardar, errores, cancellationToken);
        ValidarRol(rol, errores);

        if (errores.Count > 0)
        {
            return errores;
        }

        Usuario usuarioNuevo = new Usuario
        {
            NombreCompleto = nombreParaGuardar,
            CorreoCorporativo = correoParaGuardar,
            Rol = rol,

            // RF-70: todo usuario creado queda inicialmente Activo.
            Activo = true,

            // Queda sin vincular: IdentificarOVincularUsuario completa este dato durante el primer
            // inicio de sesión, cuando el correo autenticado coincide con el registrado acá.
            EntraObjectId = null
        };

        // ValidarCorreoCorporativoAsync ya comprobó que el correo estuviera libre, pero entre esa
        // consulta y este guardado otra solicitud pudo haber registrado el mismo correo. Por eso
        // la respuesta definitiva la da el índice único de la base, a través del repositorio.
        bool usuarioGuardado = await _repositorioUsuarios.AgregarSiCorreoDisponibleAsync(usuarioNuevo, cancellationToken);

        if (!usuarioGuardado)
        {
            errores.Add(MensajeCorreoYaRegistrado);
        }

        return errores;
    }

    private static void ValidarNombreCompleto(string nombreCompleto, List<string> errores)
    {
        if (nombreCompleto.Length == 0)
        {
            errores.Add("El nombre completo es obligatorio.");
        }
        else if (nombreCompleto.Length > LargoMaximoNombreCompleto)
        {
            errores.Add($"El nombre completo no puede superar los {LargoMaximoNombreCompleto} caracteres.");
        }
    }

    private async Task ValidarCorreoCorporativoAsync(string correoCorporativo, List<string> errores, CancellationToken cancellationToken)
    {
        // Separamos los tres casos porque el mensaje que le sirve al usuario es distinto.
        // A la base solo le preguntamos si el correo es válido: si no, no tendría sentido consultar.
        if (correoCorporativo.Length == 0)
        {
            errores.Add("El correo corporativo es obligatorio.");
        }
        // Reutilizamos el validador compartido porque la regla de formato es la misma. También
        // controla el máximo de 254 caracteres, que es el largo de la columna.
        else if (!ValidadorCorreos.TieneFormatoValido(correoCorporativo))
        {
            errores.Add("El correo corporativo no tiene un formato válido.");
        }
        else
        {
            // RF-70: el correo es único entre todos los usuarios, también los inactivos.
            Usuario? usuarioConEseCorreo = await _repositorioUsuarios.ObtenerPorCorreoCorporativoAsync(correoCorporativo, cancellationToken);

            if (usuarioConEseCorreo != null)
            {
                errores.Add(MensajeCorreoYaRegistrado);
            }
        }
    }

    private static void ValidarRol(RolUsuario rol, List<string> errores)
    {
        // Un enum de C# acepta cualquier número aunque no tenga nombre: (RolUsuario)99
        // compila sin problemas. Por eso comprobamos que sea uno de los tres roles fijos.
        if (!Enum.IsDefined(rol))
        {
            errores.Add("Seleccioná un rol válido.");
        }
    }

    // Guardamos todos los correos en minúsculas porque "Juan@eop.com.uy" y "juan@eop.com.uy" son
    // la misma cuenta. Así la unicidad no depende de cómo compare la base las mayúsculas.
    // Invariant para que el resultado no dependa del idioma configurado en el servidor.
    private static string NormalizarCorreo(string? correoCorporativo)
    {
        string correoSinEspacios = QuitarEspaciosDeLosExtremos(correoCorporativo);

        return correoSinEspacios.ToLowerInvariant();
    }

    private static string QuitarEspaciosDeLosExtremos(string? texto)
    {
        if (texto == null)
        {
            return string.Empty;
        }

        return texto.Trim();
    }
}
