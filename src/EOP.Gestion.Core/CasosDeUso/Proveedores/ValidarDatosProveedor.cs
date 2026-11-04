using EOP.Gestion.Core.DTOs.Clientes;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.Core.Interfaces.Proveedores;
using EOP.Gestion.Core.ServiciosDominio.Clientes;
using EOP.Gestion.Core.ServiciosDominio.Proveedores;

namespace EOP.Gestion.Core.CasosDeUso.Proveedores;

// Revisa todos los datos de un Proveedor nuevo (US-009, RF-23) y devuelve la lista de errores.
// Lista vacía significa que está todo bien. Acá no se guarda nada: eso le toca a S2-T13, que va
// a llamar a esta clase antes de guardar.
//
// Las reglas sueltas viven en otros lados y esta clase solo las coordina y arma los mensajes.
public class ValidarDatosProveedor
{
    private const string NombreDelNumero = "teléfono";

    // Para consultar si el nombre o el RUT ya están registrados.
    private readonly IRepositorioProveedores _repositorioProveedores;

    // El mismo servicio que usa Clientes para armar el número internacional.
    private readonly IServicioTelefonos _servicioTelefonos;

    public ValidarDatosProveedor(IRepositorioProveedores repositorioProveedores, IServicioTelefonos servicioTelefonos)
    {
        _repositorioProveedores = repositorioProveedores;
        _servicioTelefonos = servicioTelefonos;
    }

    // El país y el número nacional son datos del formulario y por eso vienen aparte: en el
    // Proveedor no se guardan, lo que se guarda es el número internacional que armamos con ellos.
    //
    // No cortamos en el primer error para que el usuario vea todos juntos. Además, RF-23 pide
    // que el Nombre y el RUT se comprueben de forma independiente.
    public async Task<List<string>> ValidarAsync(Proveedor proveedor, string? paisTelefono, string? numeroNacionalTelefono, CancellationToken cancellationToken = default)
    {
        List<string> errores = new();

        await ValidarNombreAsync(proveedor, errores, cancellationToken);

        string rutNormalizado = ValidadorRut.Normalizar(proveedor.Rut);
        await ValidarRutAsync(rutNormalizado, errores, cancellationToken);

        ResultadoNumeroInternacional telefono = _servicioTelefonos.ConvertirAInternacional(paisTelefono, numeroNacionalTelefono, NombreDelNumero);

        if (telefono.Error != null)
        {
            errores.Add(telefono.Error);
        }

        ValidarCorreos(proveedor, errores);
        ValidarCuentasBancarias(proveedor, errores);

        // Solo si está todo bien dejamos los datos en el objeto como se tienen que guardar.
        // Esto toca únicamente el Proveedor que estamos dando de alta, que todavía no está en la base.
        if (errores.Count == 0)
        {
            proveedor.Rut = rutNormalizado;
            proveedor.Telefono = telefono.NumeroInternacional!;
            proveedor.CorreoContacto = ObtenerCorreoParaGuardar(proveedor.CorreoContacto);
            proveedor.CorreoFacturacion = ObtenerCorreoParaGuardar(proveedor.CorreoFacturacion);
        }

        return errores;
    }

    private async Task ValidarNombreAsync(Proveedor proveedor, List<string> errores, CancellationToken cancellationToken)
    {
        // Miramos NombreNormalizado y no Nombre porque es el que tiene la última palabra: un nombre
        // que fuera solo un tilde suelto no parece vacío, pero al normalizarlo no queda nada.
        if (string.IsNullOrWhiteSpace(proveedor.NombreNormalizado))
        {
            errores.Add("El nombre del proveedor es obligatorio.");
        }
        else if (await _repositorioProveedores.ExistePorNombreNormalizadoAsync(proveedor.NombreNormalizado, cancellationToken))
        {
            errores.Add("Ya existe un proveedor registrado con ese nombre.");
        }
    }

    private async Task ValidarRutAsync(string rutNormalizado, List<string> errores, CancellationToken cancellationToken)
    {
        // Separamos el vacío del inválido porque el mensaje que le sirve al usuario es distinto.
        if (rutNormalizado.Length == 0)
        {
            errores.Add("El RUT del proveedor es obligatorio.");
        }
        else if (!ValidadorRut.EsValido(rutNormalizado))
        {
            errores.Add("El RUT ingresado no es válido.");
        }
        else if (await _repositorioProveedores.ExistePorRutAsync(rutNormalizado, cancellationToken))
        {
            errores.Add("Ya existe un proveedor registrado con ese RUT.");
        }
    }

    // Reutilizamos el validador de Clientes porque la regla es la misma: al menos un correo, y
    // el que esté informado tiene que ser válido. Un correo vacío se da por "no informado", así
    // que con uno solo alcanza, y si vienen los dos se revisan los dos.
    private static void ValidarCorreos(Proveedor proveedor, List<string> errores)
    {
        if (!ValidadorCorreosCliente.TieneAlMenosUnCorreo(proveedor.CorreoContacto, proveedor.CorreoFacturacion))
        {
            errores.Add("Debe informar al menos un correo electrónico.");
        }

        if (!ValidadorCorreosCliente.TieneFormatoValido(proveedor.CorreoContacto))
        {
            errores.Add("El correo de contacto no tiene un formato válido.");
        }

        if (!ValidadorCorreosCliente.TieneFormatoValido(proveedor.CorreoFacturacion))
        {
            errores.Add("El correo de facturación no tiene un formato válido.");
        }
    }

    private static void ValidarCuentasBancarias(Proveedor proveedor, List<string> errores)
    {
        if (proveedor.CuentasBancarias.Count == 0)
        {
            errores.Add("Debe informar al menos una cuenta bancaria.");
        }

        // Revisamos todas, no alcanza con que una esté bien: RF-23 no permite guardar una cuenta
        // incompleta. Numeramos cada mensaje para que se sepa cuál hay que corregir.
        int numeroDeCuenta = 1;

        foreach (CuentaBancaria cuenta in proveedor.CuentasBancarias)
        {
            foreach (string errorDeLaCuenta in ValidadorCuentaBancaria.ObtenerErrores(cuenta))
            {
                errores.Add($"Cuenta bancaria {numeroDeCuenta}: {errorDeLaCuenta}");
            }

            numeroDeCuenta++;
        }
    }

    // Un correo que es solo espacios se considera no informado, así que se guarda como NULL.
    // Sin esto quedarían espacios en la base, en un campo que parece tener un dato y no lo tiene.
    private static string? ObtenerCorreoParaGuardar(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            return null;
        }

        return correo;
    }
}
