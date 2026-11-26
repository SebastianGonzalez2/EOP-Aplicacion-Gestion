using System.Text.Json.Serialization;

namespace EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;

// Estas clases son el "molde" del JSON que responde datosUruguay, por ejemplo:
//
//   { "data": [ { "date": "2026-11-24", "series": "usd", "value": 40.697 }, ... ],
//     "meta": { "source": "BCU", "unit": "UYU/USD", "has_more": true, ... } }
//
// Son internas a propósito: nadie fuera de ClienteDatosUruguay tiene que conocer el formato del
// proveedor. Todas las propiedades son anulables porque no podemos confiar en que el JSON venga
// completo; ClienteDatosUruguay revisa cada una antes de usarla.
//
// Solo mapeamos los campos que usamos. Los demás (license, attribution, total, etc.) se ignoran,
// que es lo que pide el propio proveedor: los clientes tienen que ignorar los campos que no conocen.
internal sealed class RespuestaDatosUruguay
{
    [JsonPropertyName("data")]
    public List<PuntoDatosUruguay?>? Datos { get; set; }

    [JsonPropertyName("meta")]
    public MetaDatosUruguay? Meta { get; set; }
}

internal sealed class PuntoDatosUruguay
{
    // Fecha de la publicación en formato ISO 8601 ("2026-11-24").
    [JsonPropertyName("date")]
    public string? Fecha { get; set; }

    // Qué cotización es; para este endpoint tiene que ser "usd".
    [JsonPropertyName("series")]
    public string? Serie { get; set; }

    // El valor en UYU por USD. Usamos decimal y no double: System.Text.Json lee el número tal cual
    // viene escrito en el JSON (40.697 queda exactamente 40.697), sin los errores de redondeo de double.
    [JsonPropertyName("value")]
    public decimal? Valor { get; set; }
}

internal sealed class MetaDatosUruguay
{
    // Organismo que produjo el dato; tiene que ser el BCU (RF-29).
    [JsonPropertyName("source")]
    public string? Fuente { get; set; }

    // Unidad del valor; tiene que ser pesos por dólar.
    [JsonPropertyName("unit")]
    public string? Unidad { get; set; }

    // true si hay más resultados en la página siguiente.
    [JsonPropertyName("has_more")]
    public bool? HayMas { get; set; }
}
