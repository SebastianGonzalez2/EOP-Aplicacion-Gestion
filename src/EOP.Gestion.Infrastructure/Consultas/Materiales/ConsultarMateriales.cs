
using EOP.Gestion.Core.DTOs.Materiales;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Materiales
{
    public class ConsultarMateriales
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto para consultar la base de datos.
        public ConsultarMateriales(EopDbContext context)
        {
            _context = context;
        }

        // Obtiene los materiales aplicando los filtros seleccionados.
        public async Task<List<MaterialListadoDto>> EjecutarAsync(string? busqueda,UnidadMedida? unidad,int? proveedorId,CancellationToken cancellationToken = default)
        {
            // Preparamos la consulta sin modificar los registros.
            var consulta = _context.Materiales.AsNoTracking();

            // Buscamos por nombre completo o parcial.
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string[] palabras = busqueda.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);

                string nombreBuscado = string.Join(" ", palabras).ToUpperInvariant();

                consulta = consulta.Where(material =>material.NombreNormalizado.Contains(nombreBuscado));
            }

            // Filtramos por unidad de medida.
            if (unidad.HasValue)
            {
                consulta = consulta.Where(material =>material.Unidad == unidad.Value);
            }

            // Mostramos materiales relacionados con el proveedor elegido.
            if (proveedorId.HasValue)
            {
                consulta = consulta.Where(material =>_context.MaterialProveedores.Any(relacion =>relacion.MaterialId == material.Id && relacion.ProveedorId == proveedorId.Value));
            }

            // Obtenemos los materiales que cumplen los filtros.
            var materiales = await consulta.OrderBy(material => material.Nombre).Select(material => new MaterialListadoDto
                {
                    Id = material.Id,
                    Nombre = material.Nombre,
                    Unidad = material.Unidad
                }).ToListAsync(cancellationToken);

            if (materiales.Count == 0)
            {
                return materiales;
            }

            // Buscamos los precios de los proveedores asociados
            // solamente a los materiales que aparecen en el listado.
            var materialesIds = materiales.Select(material => material.Id).ToList();

            var referencias = await (
                from relacion in _context.MaterialProveedores.AsNoTracking()
                join proveedor in _context.Proveedores.AsNoTracking()
                    on relacion.ProveedorId equals proveedor.Id
                where materialesIds.Contains(relacion.MaterialId)
                orderby proveedor.Nombre
                select new
                {
                    relacion.MaterialId,
                    ProveedorNombre = proveedor.Nombre,
                    relacion.PrecioUnitario,
                    relacion.Moneda,
                    relacion.FechaUltimaActualizacion
                }
            ).ToListAsync(cancellationToken);

            // Organizamos los materiales por Id para encontrar
            // rápidamente a cuál pertenece cada precio.
            var materialesPorId = materiales.ToDictionary(material => material.Id);

            // Agregamos cada referencia al material correspondiente.
            foreach (var referencia in referencias)
            {
                var material = materialesPorId[referencia.MaterialId];

                material.Referencias.Add(new ReferenciaPrecioMaterialDto
                {
                    ProveedorNombre = referencia.ProveedorNombre,
                    PrecioUnitario = referencia.PrecioUnitario,
                    Moneda = referencia.Moneda,
                    FechaUltimaActualizacion = referencia.FechaUltimaActualizacion
                });
            }

            return materiales;
        }
    }
}
