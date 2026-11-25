using System.Text.Json;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.WebApp.Servicios;

// Guarda los cambios pendientes de un presupuesto dentro de la sesión del usuario (ISession),
// sin persistirlos en SQL Server hasta que presione explícitamente «Guardar borrador» (US-003, RF-05).
//
// Cada borrador se identifica por usuario y presupuesto: así dos usuarios no se pisan entre sí y
// el mismo usuario puede continuar navegando por las pantallas de elaboración (por ejemplo,
// SeleccionarMaterial) sin perder sus avances antes de guardar.
//
// También deja lista la detección y limpieza de cambios pendientes para la advertencia de S3-T15.
public class AlmacenBorradorPresupuestoEnSesion : IAlmacenBorradorPresupuesto
{
    private readonly IHttpContextAccessor _accesoHttpContext;

    public AlmacenBorradorPresupuestoEnSesion(IHttpContextAccessor accesoHttpContext)
    {
        _accesoHttpContext = accesoHttpContext;
    }

    public async Task CargarAsync(CancellationToken cancellationToken)
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null)
        {
            return;
        }

        await sesion.LoadAsync(cancellationToken);
    }

    public async Task ConfirmarAsync(CancellationToken cancellationToken)
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null)
        {
            return;
        }

        await sesion.CommitAsync(cancellationToken);
    }

    // Obtiene el borrador de presupuesto desde la sesión del usuario, deserializado desde JSON.
    public BorradorPresupuestoDto? Obtener(int presupuestoId, int usuarioId)
    {
        //La sesion permite conservar informacion entre diferentes solicitudes HTTP del mismo usuari
        //Por ejemplo si va al catalogo y vuelve al presupuest, se conserva la informacion que estaba en el borrador
        ISession? sesion = ObtenerSesion();
        if (sesion == null)
        {
            return null;
        }
        //Con el metodo armar clave se genera una clave unica para cada usuario y presupuesto,
        //de esta manera se evita que un usuario pueda acceder a los borradores de otro usuario
        string clave = ArmarClave(presupuestoId, usuarioId);

        //Se obtiene el contenido del borrador en formato JSON desde la sesión
        string? contenidoJson = sesion.GetString(clave);

        if (string.IsNullOrWhiteSpace(contenidoJson))
        {
            return null;
        }

        try
        {
            BorradorPresupuestoDto? borrador = JsonSerializer.Deserialize<BorradorPresupuestoDto>(contenidoJson);
            if (borrador == null || borrador.PresupuestoId != presupuestoId)
            {
                sesion.Remove(clave);
                return null;
            }

            return borrador;
        }
        catch (JsonException)
        {
            // Si el contenido en sesión estuviera corrupto, lo limpiamos para no romper la pantalla.
            sesion.Remove(clave);
            return null;
        }
    }

    // Guarda el borrador en la sesión del usuario, serializado como JSON.
    public void Guardar(int presupuestoId, int usuarioId, BorradorPresupuestoDto borrador)
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null)
        {
            throw new InvalidOperationException("No hay una sesión HTTP disponible para conservar el borrador.");
        }

        string clave = ArmarClave(presupuestoId, usuarioId);
        string contenidoJson = JsonSerializer.Serialize(borrador);
        sesion.SetString(clave, contenidoJson);
    }

    public void Eliminar(int presupuestoId, int usuarioId)
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null)
        {
            return;
        }

        string clave = ArmarClave(presupuestoId, usuarioId);
        sesion.Remove(clave);
    }

    public bool TieneCambiosPendientes(int presupuestoId, int usuarioId)
    {
        BorradorPresupuestoDto? borrador = Obtener(presupuestoId, usuarioId);
        if (borrador == null)
        {
            return false;
        }

        return borrador.TieneCambiosPendientes;
    }

    public void RegistrarHuellaGuardada(int presupuestoId, int usuarioId, string huellaGuardada)
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null || string.IsNullOrWhiteSpace(huellaGuardada))
        {
            return;
        }

        string clave = ArmarClaveUltimaHuella(presupuestoId, usuarioId);
        sesion.SetString(clave, huellaGuardada);
    }

    public bool CoincideConUltimoGuardadoEnSesion(int presupuestoId, int usuarioId, string huellaActual)
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null || string.IsNullOrWhiteSpace(huellaActual))
        {
            return false;
        }

        string clave = ArmarClaveUltimaHuella(presupuestoId, usuarioId);
        string? huellaGuardada = sesion.GetString(clave);

        if (string.IsNullOrWhiteSpace(huellaGuardada))
        {
            return false;
        }

        return string.Equals(huellaGuardada, huellaActual, StringComparison.Ordinal);
    }

    public List<BorradorPresupuestoDto> ObtenerPendientesDelUsuario(int usuarioId)
    {
        var pendientes = new List<BorradorPresupuestoDto>();

        ISession? sesion = ObtenerSesion();
        if (sesion == null)
        {
            return pendientes;
        }

        // Todas las claves de borrador de este usuario empiezan igual: "BorradorPresupuesto:{usuarioId}:".
        // Filtrar por ese prefijo es lo que garantiza que nunca veamos borradores de otra persona,
        // aunque hayan quedado en la misma sesión del navegador.
        string prefijo = $"BorradorPresupuesto:{usuarioId}:";

        // Copiamos las claves a una lista aparte porque Obtener() puede borrar una clave corrupta,
        // y no se puede modificar una colección mientras se la está recorriendo.
        var claves = new List<string>(sesion.Keys);

        foreach (string clave in claves)
        {
            if (!clave.StartsWith(prefijo, StringComparison.Ordinal))
            {
                continue;
            }

            string textoPresupuestoId = clave.Substring(prefijo.Length);
            if (!int.TryParse(textoPresupuestoId, out int presupuestoId))
            {
                continue;
            }

            BorradorPresupuestoDto? borrador = Obtener(presupuestoId, usuarioId);
            if (borrador != null && borrador.TieneCambiosPendientes)
            {
                pendientes.Add(borrador);
            }
        }

        // Los ordenamos por presupuesto para que la advertencia los muestre siempre en el mismo orden.
        pendientes.Sort((primero, segundo) => primero.PresupuestoId.CompareTo(segundo.PresupuestoId));
        return pendientes;
    }

    public void RegistrarDescarte(int presupuestoId, int usuarioId, string borradorId)
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null || string.IsNullOrWhiteSpace(borradorId))
        {
            return;
        }

        sesion.SetString(ArmarClaveUltimoDescarte(presupuestoId, usuarioId), borradorId);
    }

    public bool FueDescartadoEnSesion(int presupuestoId, int usuarioId, string borradorId)
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null || string.IsNullOrWhiteSpace(borradorId))
        {
            return false;
        }

        string? borradorDescartado = sesion.GetString(ArmarClaveUltimoDescarte(presupuestoId, usuarioId));
        if (string.IsNullOrWhiteSpace(borradorDescartado))
        {
            return false;
        }

        return string.Equals(borradorDescartado, borradorId, StringComparison.Ordinal);
    }

    public void VaciarDatosTemporales()
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null)
        {
            return;
        }

        // Clear() borra todas las claves de la sesión: borradores, huellas y descartes.
        sesion.Clear();
    }

    private ISession? ObtenerSesion()
    {
        HttpContext? contexto = _accesoHttpContext.HttpContext;
        if (contexto == null)
        {
            return null;
        }

        return contexto.Session;
    }

    private static string ArmarClave(int presupuestoId, int usuarioId)
    {
        return $"BorradorPresupuesto:{usuarioId}:{presupuestoId}";
    }

    private static string ArmarClaveUltimaHuella(int presupuestoId, int usuarioId)
    {
        return $"BorradorPresupuestoUltimaHuella:{usuarioId}:{presupuestoId}";
    }

    private static string ArmarClaveUltimoDescarte(int presupuestoId, int usuarioId)
    {
        return $"BorradorPresupuestoUltimoDescarte:{usuarioId}:{presupuestoId}";
    }
}
