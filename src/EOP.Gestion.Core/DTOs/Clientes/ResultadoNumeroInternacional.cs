namespace EOP.Gestion.Core.DTOs.Clientes;

// La respuesta de intentar armar el número internacional: o tenemos el número
// listo para guardar, o tenemos el motivo por el que no se pudo armar. Nunca las dos cosas.
public sealed record ResultadoNumeroInternacional
{
    public string? NumeroInternacional { get; private init; }
    public string? Error { get; private init; }

    public static ResultadoNumeroInternacional Valido(string numeroInternacional) =>
        new() { NumeroInternacional = numeroInternacional };

    public static ResultadoNumeroInternacional Invalido(string error) =>
        new() { Error = error };
}
