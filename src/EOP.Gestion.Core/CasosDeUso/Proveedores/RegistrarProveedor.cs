using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Interfaces.Proveedores;

namespace EOP.Gestion.Core.CasosDeUso.Proveedores;

// La acción completa de "registrar un proveedor" (US-009, RF-23): valida y, si está todo bien,
// guarda. Las reglas no están acá: viven en ValidarDatosProveedor y esta clase solo decide qué
// paso sigue.
public class RegistrarProveedor
{
    private readonly ValidarDatosProveedor _validarDatosProveedor;

    private readonly IRepositorioProveedores _repositorioProveedores;

    public RegistrarProveedor(ValidarDatosProveedor validarDatosProveedor, IRepositorioProveedores repositorioProveedores)
    {
        _validarDatosProveedor = validarDatosProveedor;
        _repositorioProveedores = repositorioProveedores;
    }

    // Devuelve la lista de errores para el usuario. Vacía quiere decir que el proveedor se guardó
    // de verdad: es lo que le permite al Controller avisar del éxito sin riesgo de mentir.
    //
    // El país y el número nacional vienen aparte porque son datos del formulario. Lo que queda en
    // el Proveedor es el número internacional que arma la validación con ellos.
    public async Task<List<string>> EjecutarAsync(Proveedor proveedor, string? paisTelefono, string? numeroNacionalTelefono, CancellationToken cancellationToken = default)
    {
        List<string> errores = await _validarDatosProveedor.ValidarAsync(proveedor, paisTelefono, numeroNacionalTelefono, cancellationToken);

        if (errores.Count > 0)
        {
            return errores;
        }

        ResultadoAgregarProveedor resultado = await _repositorioProveedores.AgregarAsync(proveedor, cancellationToken);

        // Si otro usuario guardó el mismo proveedor entre nuestra validación y nuestro guardado, la
        // validación no lo pudo ver y lo frenó la base. Para quien está cargando el formulario es
        // el mismo problema de siempre, así que le mostramos el mismo mensaje.
        if (resultado == ResultadoAgregarProveedor.NombreDuplicado)
        {
            errores.Add(ValidarDatosProveedor.MensajeNombreDuplicado);
        }
        else if (resultado == ResultadoAgregarProveedor.RutDuplicado)
        {
            errores.Add(ValidarDatosProveedor.MensajeRutDuplicado);
        }

        return errores;
    }
}
