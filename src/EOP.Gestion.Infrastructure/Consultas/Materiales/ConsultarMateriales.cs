
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



        // Busca un material existente por su identificador.
        // Lo usamos para mostrar qué material seleccionó el usuario
        // antes de agregarle un nuevo proveedor.
        public async Task<MaterialListadoDto?> ObtenerPorIdAsync(int materialId,CancellationToken cancellationToken = default)
        {
            // No tiene sentido consultar la base con un Id inválido.
            if (materialId <= 0)
            {
                return null;
            }

            // Consultamos solamente el material solicitado.
            // AsNoTracking evita que EF Core prepare la entidad
            // para modificaciones, porque solamente queremos leerla.
            MaterialListadoDto? material = await _context.Materiales.AsNoTracking().Where(material => material.Id == materialId).Select(material => new MaterialListadoDto
                {
                    Id = material.Id,
                    Nombre = material.Nombre,
                    Unidad = material.Unidad
                }).FirstOrDefaultAsync(cancellationToken);

            return material;
        }

        // S2-T20 (US-071): trae el material con sus relaciones (incluido el Id de cada una) para
        // mostrar el formulario de edición con los datos actuales. Devuelve null si no existe.
        public async Task<MaterialEdicionDto?> ObtenerParaEdicionAsync(int materialId,CancellationToken cancellationToken = default)
        {
            if (materialId <= 0)
            {
                return null;
            }

            MaterialEdicionDto? material = await _context.Materiales.AsNoTracking().Where(material => material.Id == materialId).Select(material => new MaterialEdicionDto
                {
                    Id = material.Id,
                    Nombre = material.Nombre,
                    Unidad = material.Unidad
                }).FirstOrDefaultAsync(cancellationToken);

            if (material == null)
            {
                return null;
            }

            // Las referencias, ordenadas por proveedor como en el listado.
            material.Referencias = await (
                from relacion in _context.MaterialProveedores.AsNoTracking()
                join proveedor in _context.Proveedores.AsNoTracking()
                    on relacion.ProveedorId equals proveedor.Id
                where relacion.MaterialId == materialId
                orderby proveedor.Nombre
                select new ReferenciaEdicionDto
                {
                    RelacionId = relacion.Id,
                    ProveedorNombre = proveedor.Nombre,
                    PrecioUnitario = relacion.PrecioUnitario,
                    Moneda = relacion.Moneda,
                    FechaUltimaActualizacion = relacion.FechaUltimaActualizacion
                }
            ).ToListAsync(cancellationToken);

            return material;
        }


    }
}
