using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos;

public static class PermisosConceptosPresupuesto
{
    public static bool PuedeEditar(Presupuesto presupuesto, Usuario usuario)
    {
        if (!usuario.Activo || presupuesto.Estado is not (
            EstadoPresupuesto.Borrador or EstadoPresupuesto.ParaRevisionInterna
            or EstadoPresupuesto.SolicitudDeModificacionDelCliente))
        {
            return false;
        }

        if (usuario.Rol == RolUsuario.Operador)
        {
            return usuario.Id == presupuesto.ResponsableId;
        }

        return usuario.Rol is RolUsuario.Supervisor or RolUsuario.Administrador;
    }
}
