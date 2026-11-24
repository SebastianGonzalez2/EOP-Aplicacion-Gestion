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

    public BorradorPresupuestoDto? Obtener(int presupuestoId, int usuarioId)
    {
        ISession? sesion = ObtenerSesion();
        if (sesion == null)
        {
            return null;
        }

        string clave = ArmarClave(presupuestoId, usuarioId);
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
}
