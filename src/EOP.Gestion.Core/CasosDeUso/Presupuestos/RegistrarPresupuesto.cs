
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.Interfaces.Usuarios;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos
{
    public class RegistrarPresupuesto
    {
        private readonly IRepositorioPresupuestos _repositorioPresupuestos;
        private readonly IConsultaReferenciasPresupuesto _consultaReferencias;
        private readonly IRepositorioUsuarios _repositorioUsuarios;

        // Recibimos los servicios que necesitamos para validar
        // los datos y guardar el presupuesto.
        public RegistrarPresupuesto(IRepositorioPresupuestos repositorioPresupuestos,IConsultaReferenciasPresupuesto consultaReferencias,IRepositorioUsuarios repositorioUsuarios)
        {
            _repositorioPresupuestos = repositorioPresupuestos;
            _consultaReferencias = consultaReferencias;
            _repositorioUsuarios = repositorioUsuarios;
        }

        // Valida los datos del presupuesto y, si son correctos,
        // lo registra en la base de datos.
        public async Task<List<string>> EjecutarAsync(Presupuesto presupuesto,string? entraObjectId,CancellationToken cancellationToken = default)
        {
            List<string> errores = new List<string>();

            // Quitamos los espacios sobrantes de los extremos.
            string nombreProyecto = string.Empty;

            if (presupuesto.NombreProyecto != null)
            {
                nombreProyecto = presupuesto.NombreProyecto.Trim();
            }

            // Comprobamos los datos obligatorios.
            if (string.IsNullOrWhiteSpace(nombreProyecto))
            {
                errores.Add("El nombre del proyecto es obligatorio.");
            }
            else if (nombreProyecto.Length > 200)
            {
                errores.Add("El nombre no puede superar los 200 caracteres.");
            }

            if (presupuesto.ClienteId <= 0)
            {
                errores.Add("Seleccioná un cliente válido.");
            }

            if (presupuesto.TipoProyectoId <= 0)
            {
                errores.Add("Seleccioná un tipo de proyecto válido.");
            }

            if (presupuesto.PlazoVigenciaDias <= 0)
            {
                errores.Add("El plazo de vigencia debe ser mayor que cero.");
            }

            // El Responsable nunca viene del formulario.
            // Necesitamos identificar al usuario que inició sesión.
            if (string.IsNullOrWhiteSpace(entraObjectId))
            {
                errores.Add("No se pudo identificar al usuario autenticado.");
            }

            // Si faltan datos, no continuamos con el guardado.
            if (errores.Count > 0)
            {
                return errores;
            }

            // Buscamos al usuario de EOP asociado a su cuenta Microsoft.
            Usuario? usuario = await _repositorioUsuarios.ObtenerPorIdentidadExternaAsync(entraObjectId!,cancellationToken);

            if (usuario == null || !usuario.Activo)
            {
                errores.Add("El usuario no está habilitado para crear presupuestos.");
                return errores;
            }

            // Comprobamos que el cliente realmente exista y esté activo.
            bool clienteActivo = await _consultaReferencias.ClienteEstaActivoAsync(presupuesto.ClienteId,cancellationToken);

            if (!clienteActivo)
            {
                errores.Add("El cliente seleccionado no existe o está inactivo.");
            }

            // Comprobamos que el tipo de proyecto exista.
            bool tipoProyectoExiste = await _consultaReferencias.TipoProyectoExisteAsync(presupuesto.TipoProyectoId,cancellationToken);

            if (!tipoProyectoExiste)
            {
                errores.Add("El tipo de proyecto seleccionado no existe.");
            }

            if (errores.Count > 0)
            {
                return errores;
            }

            // Completamos los datos que debe establecer el sistema.
            presupuesto.NombreProyecto = nombreProyecto;
            presupuesto.ResponsableId = usuario.Id;
            presupuesto.Estado = EstadoPresupuesto.Borrador;
            presupuesto.FechaCreacion = DateTime.UtcNow;

            // El repositorio guarda el presupuesto y genera
            // automáticamente su código interno único.
            await _repositorioPresupuestos.AgregarConCodigoAsignadoAsync(presupuesto,cancellationToken);

            // Si no hubo errores, devolvemos la lista vacía.
            return errores;
        }
    }
}
