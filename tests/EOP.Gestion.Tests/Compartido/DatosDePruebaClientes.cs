using EOP.Gestion.Core.Entidades.Clientes;

namespace EOP.Gestion.Tests.Compartido;

internal static class DatosDePruebaClientes
{
    public const string AnaPereira = "Ana Pereira";
    public const string AnaGarcia = "Ana Garcia";
    public const string BrunoSilva = "Bruno Silva";

    public const string RutAnaPereira = "215521750017";
    public const string RutAnaGarcia = "215521750025";
    public const string RutBrunoSilva = "215521750033";

    public static readonly string[] Nombres = [AnaGarcia, AnaPereira, BrunoSilva];

    // Se insertan fuera de orden y con combinaciones distintas de correos y estado.
    public static Cliente[] CrearClientes() =>
    [
        new()
        {
            Nombre = BrunoSilva,
            Rut = RutBrunoSilva,
            CorreoContacto = "bruno@ejemplo.com",
            CorreoFacturacion = "facturas@bruno.com",
            NumeroWhatsApp = "+59899345678",
            Activo = true
        },
        new()
        {
            Nombre = AnaPereira,
            Rut = RutAnaPereira,
            CorreoContacto = "ana@ejemplo.com",
            NumeroWhatsApp = "+59899123456",
            Activo = true
        },
        new()
        {
            Nombre = AnaGarcia,
            Rut = RutAnaGarcia,
            CorreoFacturacion = "facturas@garcia.com",
            NumeroWhatsApp = "+59899234567",
            Activo = false
        }
    ];
}
