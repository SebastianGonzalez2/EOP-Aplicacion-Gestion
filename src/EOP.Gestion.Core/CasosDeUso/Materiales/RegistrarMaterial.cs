
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Interfaces.Materiales;
using EOP.Gestion.Core.Interfaces.Proveedores;

namespace EOP.Gestion.Core.CasosDeUso.Materiales
{
    public class RegistrarMaterial
    {
        // El mismo largo que tiene la columna en MaterialConfiguracion. Si lo dejáramos pasar,
        // SQL Server rechazaría el INSERT y el usuario vería un error genérico en vez de este mensaje.
        public const int LargoMaximoNombre = 200;

        private readonly IRepositorioMateriales _repositorioMateriales;
        private readonly IRepositorioProveedores _repositorioProveedores;

        // Recibimos los repositorios que necesitamos
        // para consultar y guardar los datos.
        public RegistrarMaterial(IRepositorioMateriales repositorioMateriales,IRepositorioProveedores repositorioProveedores)
        {
            _repositorioMateriales = repositorioMateriales;
            _repositorioProveedores = repositorioProveedores;
        }

        // Valida los datos y registra el material con
        // su primera relación con un proveedor.
        public async Task<List<string>> EjecutarAsync(string? nombre,UnidadMedida? unidad,int? proveedorId,decimal? precioUnitario,Moneda? moneda,
                                                      CancellationToken cancellationToken = default)
        {
            List<string> errores = new List<string>();

            // S1-T27: medimos y guardamos el nombre sin los espacios del principio y del final.
            // Si no, "  Caño  " ocuparía más lugar del que se ve y quedaría guardado con esos
            // espacios. (Para detectar duplicados no cambia nada: NombreNormalizado ya los ignoraba.)
            string nombreSinEspaciosEnLosExtremos = string.Empty;

            if (nombre != null)
            {
                nombreSinEspaciosEnLosExtremos = nombre.Trim();
            }

            // El nombre del material es obligatorio.
            if (nombreSinEspaciosEnLosExtremos.Length == 0)
            {
                errores.Add("El nombre del material es obligatorio.");
            }
            else if (nombreSinEspaciosEnLosExtremos.Length > LargoMaximoNombre)
            {
                errores.Add($"El nombre del material no puede superar los {LargoMaximoNombre} caracteres.");
            }

            // La unidad debe pertenecer a la lista fija.
            if (!unidad.HasValue || !Enum.IsDefined(unidad.Value))
            {
                errores.Add("Seleccioná una unidad de medida válida.");
            }

            // Debemos recibir un identificador de proveedor válido.
            if (!proveedorId.HasValue || proveedorId.Value <= 0)
            {
                errores.Add("Seleccioná un proveedor válido.");
            }

            // El precio debe ser estrictamente mayor que cero.
            if (!precioUnitario.HasValue || precioUnitario.Value <= 0)
            {
                errores.Add("El precio unitario debe ser mayor que cero.");
            }

            // Solamente aceptamos las monedas definidas en el sistema.
            if (!moneda.HasValue || !Enum.IsDefined(moneda.Value))
            {
                errores.Add("La moneda debe ser UYU o USD.");
            }

            // Si hay errores, no consultamos ni guardamos nada.
            if (errores.Count > 0)
            {
                return errores;
            }

            // Creamos el material con los datos validados.
            // Al asignar Nombre se calcula NombreNormalizado.
            Material material = new Material
            {
                Nombre = nombreSinEspaciosEnLosExtremos,
                Unidad = unidad.Value
            };

            // Comprobamos si ya existe otro material
            // con el mismo nombre normalizado.
            bool nombreExiste = await _repositorioMateriales.ExistePorNombreNormalizadoAsync(material.NombreNormalizado,cancellationToken);

            if (nombreExiste)
            {
                errores.Add("Ya existe un material con ese nombre.");
            }

            // Verificamos que el proveedor realmente
            // esté registrado en la base de datos.
            bool proveedorExiste = await _repositorioProveedores.ExistePorIdAsync(proveedorId.Value,cancellationToken);

            if (!proveedorExiste)
            {
                errores.Add("El proveedor seleccionado no existe.");
            }

            // Si alguna consulta detectó un error,
            // no continuamos con el registro.
            if (errores.Count > 0)
            {
                return errores;
            }

            // Preparamos la primera relación del material
            // con el proveedor, su precio y moneda.
            MaterialProveedor primeraRelacion = new MaterialProveedor
            {
                ProveedorId = proveedorId.Value,
                PrecioUnitario = precioUnitario.Value,
                Moneda = moneda.Value,
                FechaUltimaActualizacion = DateTime.UtcNow
            };

            // Guardamos ambos registros mediante el repositorio.
            await _repositorioMateriales.AgregarConPrimeraRelacionAsync(material,primeraRelacion,cancellationToken);

            // Si todo salió bien, devolvemos la lista vacía.
            return errores;
        }
    }
}
