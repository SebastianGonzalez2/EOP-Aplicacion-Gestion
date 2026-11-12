using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Interfaces.Materiales;
using EOP.Gestion.Core.Interfaces.Proveedores;

namespace EOP.Gestion.Core.CasosDeUso.Materiales
{
    // S2-T19 (US-010B): agrega una nueva relación material-proveedor a un material que ya
    // está registrado en el catálogo. El proveedor tiene que existir de antes: acá nunca se crea.
    public class AgregarProveedorAMaterial
    {
        // Lo usamos en dos lugares: cuando la consulta previa encuentra la relación y cuando el
        // índice único la frena al guardar. Así el usuario ve siempre el mismo mensaje.
        public const string MensajeRelacionDuplicada = "Este material ya tiene una relación con ese proveedor.";

        private readonly IRepositorioMateriales _repositorioMateriales;
        private readonly IRepositorioProveedores _repositorioProveedores;

        public AgregarProveedorAMaterial(IRepositorioMateriales repositorioMateriales,IRepositorioProveedores repositorioProveedores)
        {
            _repositorioMateriales = repositorioMateriales;
            _repositorioProveedores = repositorioProveedores;
        }

        // Valida los datos y guarda la relación. Devuelve la lista de errores:
        // si viene vacía, la relación quedó registrada.
        public async Task<List<string>> EjecutarAsync(int materialId,int? proveedorId,decimal? precioUnitario,Moneda? moneda,
                                                      CancellationToken cancellationToken = default)
        {
            List<string> errores = new List<string>();

            // Primero las validaciones que no necesitan la base de datos.
            if (materialId <= 0)
            {
                errores.Add("Seleccioná un material válido.");
            }

            if (!proveedorId.HasValue || proveedorId.Value <= 0)
            {
                errores.Add("Seleccioná un proveedor válido.");
            }

            // El precio debe ser mayor que cero y entrar en la columna (S2-T22).
            string? errorDePrecio = ValidadorPrecioUnitario.ObtenerError(precioUnitario);

            if (errorDePrecio != null)
            {
                errores.Add(errorDePrecio);
            }

            // Solamente UYU o USD.
            if (!moneda.HasValue || !Enum.IsDefined(moneda.Value))
            {
                errores.Add("La moneda debe ser UYU o USD.");
            }

            // Si hay errores, no consultamos ni guardamos nada.
            if (errores.Count > 0)
            {
                return errores;
            }

            // Ahora las que consultan la base. Ya sabemos que los valores tienen datos.
            bool materialExiste = await _repositorioMateriales.ExistePorIdAsync(materialId,cancellationToken);

            if (!materialExiste)
            {
                errores.Add("El material seleccionado no existe.");
            }

            // El proveedor tiene que estar registrado. Alguien podría mandar un Id inventado.
            bool proveedorExiste = await _repositorioProveedores.ExistePorIdAsync(proveedorId.Value,cancellationToken);

            if (!proveedorExiste)
            {
                errores.Add("El proveedor seleccionado no existe.");
            }

            if (errores.Count > 0)
            {
                return errores;
            }

            // Un material no puede tener dos relaciones con el mismo proveedor.
            bool relacionExiste = await _repositorioMateriales.ExisteRelacionConProveedorAsync(materialId,proveedorId.Value,cancellationToken);

            if (relacionExiste)
            {
                errores.Add(MensajeRelacionDuplicada);

                return errores;
            }

            // La fecha de actualización la pone el sistema, no el usuario.
            MaterialProveedor relacion = new MaterialProveedor
            {
                MaterialId = materialId,
                ProveedorId = proveedorId.Value,
                PrecioUnitario = precioUnitario.Value,
                Moneda = moneda.Value,
                FechaUltimaActualizacion = DateTime.UtcNow
            };

            // La consulta de arriba pudo quedar vieja si otra persona guardó la misma relación
            // justo ahora. La respuesta definitiva la da el índice único de la base.
            bool relacionGuardada = await _repositorioMateriales.AgregarRelacionSiNoExisteAsync(relacion,cancellationToken);

            if (!relacionGuardada)
            {
                errores.Add(MensajeRelacionDuplicada);
            }

            return errores;
        }
    }
}
