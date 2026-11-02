using System.Globalization;
using EOP.Gestion.Core.DTOs.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.Core.ServiciosDominio.Clientes;
using PhoneNumbers;

namespace EOP.Gestion.Infrastructure.Telefonia;

// Toda la "sabiduría telefónica" viene de libphonenumber, la biblioteca de Google que
// mantiene las reglas de numeración de cada país. No escribimos reglas propias porque
// cambian seguido y son muy distintas entre países: en Uruguay el 0 inicial se saca
// (099 -> +59899), en Italia se conserva (06 -> +3906) y en Argentina los celulares
// llevan un 15 que se cambia por un 9 adelante (011 15 2345-6789 -> +5491123456789).
//
// Para actualizar países o reglas alcanza con subir la versión del paquete.
public sealed class ServicioTelefonos : IServicioTelefonos
{
    // Un número con separadores y todo no llega a esto. Es solo un tope
    // para no procesar textos enormes si alguien manipula el formulario.
    private const int MaximoCaracteresNumeroNacional = 30;

    // Lo único que escribimos a mano: la biblioteca conoce estas tres regiones,
    // pero no trae su nombre en español. Si en una versión futura aparece otra
    // en la misma situación, lo avisa la prueba ObtenerPaises_DebeTenerNombreEnEspanolParaTodos.
    private static readonly Dictionary<string, string> NombresQueFaltanEnLaBiblioteca = new()
    {
        ["AC"] = "Isla de la Ascensión",
        ["TA"] = "Tristán de Acuña",
        ["XK"] = "Kosovo"
    };

    private readonly PhoneNumberUtil _telefonos = PhoneNumberUtil.GetInstance();
    private readonly IReadOnlyList<PaisTelefonico> _paises;
    private readonly Dictionary<string, PaisTelefonico> _paisesPorCodigo;

    public ServicioTelefonos()
    {
        // Ordenamos con las reglas del español. Sin esto se compara por código de
        // carácter, y un nombre que empieza con tilde quedaría después de la Z.
        var comparadorEspanol = StringComparer.Create(CultureInfo.GetCultureInfo("es"), ignoreCase: true);

        _paises = _telefonos.GetSupportedRegions()
            .Select(CrearPais)
            .OrderBy(pais => pais.Nombre, comparadorEspanol)
            .ToList();

        // Este diccionario distingue mayúsculas a propósito: "uy" no es un código válido.
        _paisesPorCodigo = _paises.ToDictionary(pais => pais.CodigoIso);
    }

    public IReadOnlyList<PaisTelefonico> ObtenerPaises() => _paises;

    public ResultadoNumeroInternacional ConvertirAInternacional(string? codigoPais, string? numeroNacional)
    {
        // Solo aceptamos países de nuestra lista. El desplegable no alcanza como control:
        // cualquiera puede editar el HTML o mandar un POST a mano con lo que quiera.
        if (codigoPais is null || !_paisesPorCodigo.TryGetValue(codigoPais, out PaisTelefonico? pais))
        {
            return ResultadoNumeroInternacional.Invalido("Seleccioná un país válido para el número de WhatsApp.");
        }

        string numeroSinSeparadores = ValidadorWhatsApp.Normalizar(numeroNacional);

        if (numeroSinSeparadores.Length == 0)
        {
            return ResultadoNumeroInternacional.Invalido("Ingresá el número de WhatsApp.");
        }

        if (numeroNacional!.Length > MaximoCaracteresNumeroNacional)
        {
            return ResultadoNumeroInternacional.Invalido("El número de WhatsApp es demasiado largo.");
        }

        // Filtramos ANTES de llamar a la biblioteca, porque ella es muy permisiva: acepta
        // letras (099-CALL), extensiones (099123456 ext 5) o números escritos con +,
        // y acá queremos solamente el número habitual, con sus separadores.
        if (!ValidadorWhatsApp.TieneSoloCaracteresDeNumeroNacional(numeroNacional))
        {
            return ResultadoNumeroInternacional.Invalido(
                "El número de WhatsApp solo puede tener dígitos, espacios, guiones, puntos y paréntesis. " +
                "No escribas el prefijo internacional: se toma del país seleccionado.");
        }

        try
        {
            // Parse aplica las reglas del país para interpretar el número (acá se va el 0 de Uruguay).
            PhoneNumber numero = _telefonos.Parse(numeroSinSeparadores, pais.CodigoIso);

            // IsValidNumberForRegion y no IsValidNumber: este último aceptaría un número de
            // Toronto con Estados Unidos elegido, porque es válido para el +1 en general.
            if (_telefonos.IsValidNumberForRegion(numero, pais.CodigoIso))
            {
                string numeroInternacional = _telefonos.Format(numero, PhoneNumberFormat.E164);

                // Última red de seguridad: lo que se guarda tiene que pasar el mismo
                // control de formato que ya usa el resto de la aplicación.
                if (ValidadorWhatsApp.EsValido(numeroInternacional))
                {
                    return ResultadoNumeroInternacional.Valido(numeroInternacional);
                }
            }
        }
        catch (NumberParseException)
        {
            // La biblioteca avisa con una excepción cuando el texto ni siquiera se puede
            // interpretar (muy corto o muy largo). Para el usuario es lo mismo: número inválido.
        }

        return ResultadoNumeroInternacional.Invalido(
            $"El número de WhatsApp no es válido para {pais.Descripcion}. Revisá el número y el país seleccionado.");
    }

    private PaisTelefonico CrearPais(string codigoIso)
    {
        string nombre = NombresQueFaltanEnLaBiblioteca.GetValueOrDefault(codigoIso)
            ?? new Locale("es", codigoIso).GetDisplayCountry("es");

        // Si la biblioteca suma una región sin nombre, mostramos el código antes que una opción en blanco.
        if (string.IsNullOrEmpty(nombre))
        {
            nombre = codigoIso;
        }

        return new PaisTelefonico(codigoIso, nombre, _telefonos.GetCountryCodeForRegion(codigoIso));
    }
}
