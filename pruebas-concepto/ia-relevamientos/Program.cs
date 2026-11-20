
using OpenAI.Responses;
using System.ClientModel;
using System.Text.Json;

// El cliente de Responses todavía tiene algunas API marcadas como experimentales
// en esta versión del SDK.
#pragma warning disable OPENAI001

// Leemos la clave desde la variable de entorno, sin guardarla en el código.
string? claveApi = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrWhiteSpace(claveApi))
{
    Console.WriteLine("ERROR: No se encontró la variable OPENAI_API_KEY.");
    return;
}

try
{
    // Creamos el cliente que se encarga de comunicarse con OpenAI.
    ResponsesClient cliente = new(apiKey: claveApi);

    Console.WriteLine("Enviando solicitud a OpenAI...");

    // Enviamos un mensaje de prueba y esperamos la respuesta.


    CreateResponseOptions opciones = new()
    {
        Model = "gpt-6-luna",

        // Definimos exactamente la estructura que debe devolver la IA.
        TextOptions = new ResponseTextOptions
        {
            TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                "conceptos_relevamiento",
                BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "conceptos": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "tipo": {
                        "type": "string",
                        "enum": [
                          "Material",
                          "ManoDeObra",
                          "Alquiler",
                          "Traslado",
                          "CostosIndirectos",
                          "Margen"
                        ]
                      },
                      "conceptoDetectado": {
                        "type": "string"
                      },
                      "cantidad": {
                        "type": ["number", "null"]
                      },
                      "unidad": {
                        "type": ["string", "null"]
                      }
                    },
                    "required": [
                      "tipo",
                      "conceptoDetectado",
                      "cantidad",
                      "unidad"
                    ],
                    "additionalProperties": false
                  }
                }
              },
              "required": ["conceptos"],
              "additionalProperties": false
            }
            """),
                jsonSchemaIsStrict: true)
        }
    };

    // Enviamos un relevamiento ficticio para identificar sus conceptos.
    opciones.InputItems.Add(ResponseItem.CreateUserMessageItem(
        """
    Analizá este relevamiento de obra e identificá sus conceptos.

    No inventes cantidades, unidades ni precios.
    Cuando una cantidad o unidad no esté especificada,
    devolvé null.
    No selecciones materiales del catálogo.

        
    Reglas para distinguir conceptos confirmados de posibles:

    - Incluí solamente materiales, trabajos y servicios cuya
      necesidad esté confirmada en el relevamiento.
    - Si se menciona un reemplazo como posibilidad, hipótesis
      o decisión pendiente de una inspección, NO lo incluyas
      como concepto presupuestable.
    - Revisar un equipo existente no significa que haya que
      comprar uno nuevo.
    - Si el relevamiento indica expresamente que un servicio
      no es necesario, no generes un concepto para él.
    - No confundas información pendiente con una cantidad
      igual a cero.
    - No inventes información faltante.
    

    Relevamiento:
    En el edificio hay que reemplazar algunas luminarias LED, pero todavía no se determinó cuántas.

    También se debe revisar el tablero eléctrico existente para evaluar su estado. Es posible que haya que reemplazarlo, pero eso todavía no está confirmado.

    La revisión requiere 4 horas de mano de obra.

    No se necesita alquilar equipos ni trasladar materiales.

    Todavía no se definieron precios.
    """
    ));

    ResponseResult respuesta = await cliente.CreateResponseAsync(opciones);




    string jsonRespuesta = respuesta.GetOutputText();

    // Convertimos el texto JSON en un documento que C# puede recorrer.
    using JsonDocument documento = JsonDocument.Parse(jsonRespuesta);

    JsonElement conceptos = documento.RootElement.GetProperty("conceptos");

    if (conceptos.ValueKind != JsonValueKind.Array)
    {
        throw new JsonException("La respuesta no contiene una lista de conceptos.");
    }

    string[] tiposPermitidos =
    {
    "Material",
    "ManoDeObra",
    "Alquiler",
    "Traslado",
    "CostosIndirectos",
    "Margen"
};

    Console.WriteLine("Conceptos identificados y validados:");

    foreach (JsonElement concepto in conceptos.EnumerateArray())
    {
        string? tipo = concepto.GetProperty("tipo").GetString();
        string? nombre = concepto.GetProperty("conceptoDetectado").GetString();

        JsonElement cantidad = concepto.GetProperty("cantidad");
        JsonElement unidad = concepto.GetProperty("unidad");

        // No aceptamos tipos desconocidos ni conceptos sin descripción.
        if (string.IsNullOrWhiteSpace(tipo) ||
            !tiposPermitidos.Contains(tipo) ||
            string.IsNullOrWhiteSpace(nombre))
        {
            throw new JsonException("La IA devolvió un concepto inválido.");
        }

        // La cantidad y la unidad pueden estar ausentes, pero deben
        // conservar el tipo de dato que esperamos.
        if (cantidad.ValueKind != JsonValueKind.Number &&
            cantidad.ValueKind != JsonValueKind.Null)
        {
            throw new JsonException("La cantidad tiene un formato inválido.");
        }

        if (unidad.ValueKind != JsonValueKind.String &&
            unidad.ValueKind != JsonValueKind.Null)
        {
            throw new JsonException("La unidad tiene un formato inválido.");
        }

        string cantidadTexto = cantidad.ValueKind == JsonValueKind.Null
            ? "No especificada"
            : cantidad.GetRawText();

        string unidadTexto = unidad.ValueKind == JsonValueKind.Null
            ? "No especificada"
            : unidad.GetString()!;

        Console.WriteLine($"{tipo}: {nombre}");
        Console.WriteLine($"Cantidad: {cantidadTexto} | Unidad: {unidadTexto}");
        Console.WriteLine();
    }

    Console.WriteLine($"Total de conceptos: {conceptos.GetArrayLength()}");

}

catch (ClientResultException excepcion)
{
    // Mostramos el código de error sin exponer la respuesta completa
    // de OpenAI, que podría contener información sensible.
    Console.WriteLine($"La solicitud a OpenAI falló. Código HTTP: {excepcion.Status}");

    if (excepcion.Status == 401)
    {
        Console.WriteLine("La API key no es válida o fue revocada.");
    }
    else if (excepcion.Status == 429)
    {
        Console.WriteLine("Se alcanzó un límite de uso o no hay cuota disponible.");
    }
    else
    {
        Console.WriteLine("OpenAI no pudo procesar la solicitud.");
    }
}

catch (Exception excepcion)
{
    Console.WriteLine("No se pudo completar la solicitud.");
    Console.WriteLine($"Tipo de error: {excepcion.GetType().Name}");
}

#pragma warning restore OPENAI001
