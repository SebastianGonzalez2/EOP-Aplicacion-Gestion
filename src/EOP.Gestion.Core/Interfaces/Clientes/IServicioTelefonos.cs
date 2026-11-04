using EOP.Gestion.Core.DTOs.Clientes;

namespace EOP.Gestion.Core.Interfaces.Clientes;

// Core sabe QUÉ necesita (la lista de países y convertir un número), pero no CÓMO se hace.
// El cómo lo resuelve Infrastructure con una biblioteca de telefonía. Es el mismo trato
// que con los repositorios: así Core sigue sin depender de nadie, como pide AGENTS.md.
public interface IServicioTelefonos
{
    // Todos los países disponibles para el selector, ordenados por nombre.
    IReadOnlyList<PaisTelefonico> ObtenerPaises();

    // Convierte el número que el usuario usa en su país (099 123 456) al formato
    // internacional E.164 (+59899123456), aplicando las reglas del país elegido.
    //
    // nombreDelNumero es cómo se llama ese número en los mensajes de error. Lo usan Clientes
    // (WhatsApp) y Proveedores (teléfono), y a un proveedor no le tiene que aparecer "WhatsApp".
    ResultadoNumeroInternacional ConvertirAInternacional(string? codigoPais, string? numeroNacional, string nombreDelNumero = "WhatsApp");
}
