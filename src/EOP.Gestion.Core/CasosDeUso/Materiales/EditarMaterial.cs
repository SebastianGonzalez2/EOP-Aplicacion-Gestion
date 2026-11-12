using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Materiales;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Interfaces.Materiales;

namespace EOP.Gestion.Core.CasosDeUso.Materiales
{
    // S2-T20 (US-071): modifica el nombre y la unidad de un material que ya existe y el precio y la
    // moneda de sus relaciones con proveedores. No crea ni elimina relaciones (eso es US-010B) y
    // tampoco toca los presupuestos: ellos guardan su propia copia del precio.
    public class EditarMaterial
    {
        private readonly IRepositorioMateriales _repositorioMateriales;

        public EditarMaterial(IRepositorioMateriales repositorioMateriales)
        {
            _repositorioMateriales = repositorioMateriales;
        }

        // Valida los datos y guarda los cambios. Devuelve la lista de errores:
        // si viene vacía, el material quedó actualizado.
        public async Task<List<string>> EjecutarAsync(int materialId,string? nombre,UnidadMedida? unidad,IReadOnlyList<CambioReferenciaPrecio> referencias,
                                                      CancellationToken cancellationToken = default)
        {
            List<string> errores = new List<string>();

            // Primero las validaciones que no necesitan la base de datos.
            if (materialId <= 0)
            {
                errores.Add("Seleccioná un material válido.");
            }

            // Igual que al registrar: medimos y guardamos el nombre sin espacios en los extremos.
            string nombreSinEspaciosEnLosExtremos = string.Empty;

            if (nombre != null)
            {
                nombreSinEspaciosEnLosExtremos = nombre.Trim();
            }

            if (nombreSinEspaciosEnLosExtremos.Length == 0)
            {
                errores.Add("El nombre del material es obligatorio.");
            }
            else if (nombreSinEspaciosEnLosExtremos.Length > RegistrarMaterial.LargoMaximoNombre)
            {
                errores.Add($"El nombre del material no puede superar los {RegistrarMaterial.LargoMaximoNombre} caracteres.");
            }

            // La unidad debe pertenecer a la lista fija.
            if (!unidad.HasValue || !Enum.IsDefined(unidad.Value))
            {
                errores.Add("Seleccioná una unidad de medida válida.");
            }

            ValidarReferencias(referencias,errores);

            // Si hay errores, no consultamos ni guardamos nada.
            if (errores.Count > 0)
            {
                return errores;
            }

            // Ahora las que consultan la base.
            Material? material = await _repositorioMateriales.ObtenerParaEdicionAsync(materialId,cancellationToken);

            if (material == null)
            {
                errores.Add("El material seleccionado no existe.");

                return errores;
            }

            // Calculamos el nombre normalizado igual que lo hace la entidad, para compararlo con el
            // de los demás materiales. Se excluye al propio material: si el usuario no cambia el
            // nombre (o solo cambia mayúsculas), no puede chocar consigo mismo.
            Material materialConElNombreNuevo = new Material { Nombre = nombreSinEspaciosEnLosExtremos };

            bool nombreLoUsaOtroMaterial = await _repositorioMateriales.ExisteOtroConNombreNormalizadoAsync(
                materialConElNombreNuevo.NombreNormalizado,materialId,cancellationToken);

            if (nombreLoUsaOtroMaterial)
            {
                errores.Add(RegistrarMaterial.MensajeNombreDuplicado);

                return errores;
            }

            // Cada referencia que llegó tiene que ser una relación de ESTE material. Sin esto,
            // alguien podría mandar a mano el Id de la relación de otro material y cambiarle el precio.
            List<MaterialProveedor> relacionesDelMaterial = await _repositorioMateriales.ObtenerRelacionesParaEdicionAsync(materialId,cancellationToken);

            foreach (CambioReferenciaPrecio referencia in referencias)
            {
                bool perteneceAlMaterial = relacionesDelMaterial.Any(relacion => relacion.Id == referencia.RelacionId);

                if (!perteneceAlMaterial)
                {
                    errores.Add("Una de las referencias de precio no pertenece a este material.");

                    return errores;
                }
            }

            // Recién ahora, con todo validado, modificamos las entidades. Así un error de validación
            // nunca deja cambios a medias en lo que EF Core tiene en memoria.
            // Se cambia el mismo Material y las mismas relaciones: no se borra ni se recrea nada,
            // por eso los Ids (y todo lo que apunta a ellos) se conservan.
            material.Nombre = nombreSinEspaciosEnLosExtremos;
            material.Unidad = unidad.Value;

            foreach (CambioReferenciaPrecio referencia in referencias)
            {
                MaterialProveedor relacion = relacionesDelMaterial.First(r => r.Id == referencia.RelacionId);

                bool cambioElPrecio = relacion.PrecioUnitario != referencia.PrecioUnitario.Value;
                bool cambioLaMoneda = relacion.Moneda != referencia.Moneda.Value;

                // Solo la relación que cambió se toca: las demás conservan precio, moneda y fecha.
                // La fecha la pone el sistema, y solo cuando hubo un cambio económico real.
                if (cambioElPrecio || cambioLaMoneda)
                {
                    relacion.PrecioUnitario = referencia.PrecioUnitario.Value;
                    relacion.Moneda = referencia.Moneda.Value;
                    relacion.FechaUltimaActualizacion = DateTime.UtcNow;
                }
            }

            // La consulta de nombre de arriba pudo quedar vieja si otra persona tomó ese nombre
            // justo ahora. La respuesta definitiva la da el índice único de la base.
            bool guardado = await _repositorioMateriales.GuardarEdicionSiNombreDisponibleAsync(cancellationToken);

            if (!guardado)
            {
                errores.Add(RegistrarMaterial.MensajeNombreDuplicado);
            }

            return errores;
        }

        // Valida el precio y la moneda de cada referencia, y que no venga repetida la misma relación.
        private static void ValidarReferencias(IReadOnlyList<CambioReferenciaPrecio> referencias,List<string> errores)
        {
            List<int> relacionesYaVistas = new List<int>();

            foreach (CambioReferenciaPrecio referencia in referencias)
            {
                if (referencia.RelacionId <= 0)
                {
                    AgregarErrorSinRepetir(errores,"Una de las referencias de precio no es válida.");
                }

                // Si la misma relación llega dos veces, no sabríamos con cuál de los dos valores quedarnos.
                if (relacionesYaVistas.Contains(referencia.RelacionId))
                {
                    AgregarErrorSinRepetir(errores,"Una referencia de precio llegó repetida.");
                }

                relacionesYaVistas.Add(referencia.RelacionId);

                // El precio debe ser estrictamente mayor que cero.
                if (!referencia.PrecioUnitario.HasValue || referencia.PrecioUnitario.Value <= 0)
                {
                    AgregarErrorSinRepetir(errores,"El precio unitario debe ser mayor que cero.");
                }

                // Solamente UYU o USD.
                if (!referencia.Moneda.HasValue || !Enum.IsDefined(referencia.Moneda.Value))
                {
                    AgregarErrorSinRepetir(errores,"La moneda debe ser UYU o USD.");
                }
            }
        }

        // Si hay varias referencias con el mismo problema, mostramos el mensaje una sola vez.
        private static void AgregarErrorSinRepetir(List<string> errores,string mensaje)
        {
            if (!errores.Contains(mensaje))
            {
                errores.Add(mensaje);
            }
        }
    }
}
