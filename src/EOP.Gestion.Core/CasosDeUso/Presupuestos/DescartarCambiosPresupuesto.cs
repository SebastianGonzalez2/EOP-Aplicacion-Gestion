using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// S3-T15 (US-003, RF-05): descarte voluntario de los cambios sin guardar de un presupuesto.
//
// La idea es simple: el usuario tiene cambios guardados "en borrador" en su sesión y decide
// tirarlos. Lo único que hacemos es borrar ESE borrador temporal (de ese usuario y de ese
// presupuesto). No tocamos SQL Server, así que lo último que se guardó queda exactamente igual,
// y tampoco tocamos los borradores de otros presupuestos ni de otros usuarios.
//
// ¿Por qué no pedimos permiso de edición sobre el presupuesto? Porque descartar no modifica el
// presupuesto: solo borra datos temporales que son del propio usuario (la clave de la sesión
// lleva su id). Así, si mientras tanto el presupuesto dejó de ser editable, el usuario igual
// puede limpiar sus cambios, por ejemplo para poder cerrar sesión.
public class DescartarCambiosPresupuesto
{
    public const string MensajeDescartado =
        "Se descartaron los cambios sin guardar. Se muestra la información del último guardado.";

    public const string MensajeSinCambios =
        "No había cambios sin guardar para descartar. Se muestra la información del último guardado.";

    public const string MensajeYaGuardadosEnOtraPestana =
        "Los cambios ya se habían guardado desde otra pestaña, así que no se descartó nada.";

    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;
    private readonly ISincronizadorBorradorPresupuesto _sincronizador;

    public DescartarCambiosPresupuesto(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        ISincronizadorBorradorPresupuesto sincronizador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
        _sincronizador = sincronizador;
    }

    // borradorIdEsperado y versionEsperada son los datos del borrador que la pantalla le mostró
    // al usuario. Si no coinciden con lo que hay en la sesión, no descartamos nada.
    public Task<ResultadoAccionDescartarCambios> EjecutarAsync(
        int presupuestoId,
        string? borradorIdEsperado,
        int versionEsperada,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        // Igual que al guardar, hacemos todo el recorrido (leer la sesión, decidir y confirmar)
        // dentro del bloqueo del usuario, para que otra pestaña no cambie el borrador a la mitad.
        return _sincronizador.EjecutarConBloqueoAsync(
            usuario.Id,
            () => EjecutarDentroDelBloqueoAsync(
                presupuestoId, borradorIdEsperado, versionEsperada, usuario, cancellationToken),
            cancellationToken);
    }

    private async Task<ResultadoAccionDescartarCambios> EjecutarDentroDelBloqueoAsync(
        int presupuestoId,
        string? borradorIdEsperado,
        int versionEsperada,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        await _almacenBorrador.CargarAsync(cancellationToken);

        BorradorPresupuestoDto? borrador = _almacenBorrador.Obtener(presupuestoId, usuario.Id);

        if (borrador == null || !borrador.TieneCambiosPendientes)
        {
            return await ResolverSinCambiosPendientesAsync(
                presupuestoId, borradorIdEsperado, borrador, usuario, cancellationToken);
        }

        // Hay cambios pendientes. Antes de borrarlos, comprobamos que sean exactamente los que el
        // usuario estaba viendo. Si otra pestaña agregó algo después, descartarlo sería borrar
        // trabajo que el usuario ni sabe que existe.
        bool coincideIdentificador = !string.IsNullOrWhiteSpace(borradorIdEsperado)
            && string.Equals(borradorIdEsperado, borrador.BorradorId, StringComparison.Ordinal);

        bool coincideVersion = versionEsperada == borrador.Version;

        if (!coincideIdentificador || !coincideVersion)
        {
            return new ResultadoAccionDescartarCambios(
                ResultadoDescartarCambios.BorradorDesactualizado,
                MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana);
        }

        _almacenBorrador.Eliminar(presupuestoId, usuario.Id);

        // Dejamos anotado qué borrador se descartó, para que una pestaña vieja que siga mostrándolo
        // reciba un mensaje claro ("se descartaron desde otra pestaña") y no uno de pérdida.
        _almacenBorrador.RegistrarDescarte(presupuestoId, usuario.Id, borrador.BorradorId);

        await _almacenBorrador.ConfirmarAsync(cancellationToken);

        return new ResultadoAccionDescartarCambios(
            ResultadoDescartarCambios.Descartado,
            MensajeDescartado);
    }

    // No hay nada que descartar. Igual le explicamos al usuario por qué, para que no piense que
    // descartó algo que en realidad ya estaba guardado.
    private async Task<ResultadoAccionDescartarCambios> ResolverSinCambiosPendientesAsync(
        int presupuestoId,
        string? borradorIdEsperado,
        BorradorPresupuestoDto? borradorSinCambios,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        // Si quedó un borrador vacío (sin cambios), lo limpiamos para no dejar basura en la sesión.
        if (borradorSinCambios != null)
        {
            _almacenBorrador.Eliminar(presupuestoId, usuario.Id);
            await _almacenBorrador.ConfirmarAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(borradorIdEsperado)
            && _almacenBorrador.FueDescartadoEnSesion(presupuestoId, usuario.Id, borradorIdEsperado))
        {
            return new ResultadoAccionDescartarCambios(
                ResultadoDescartarCambios.SinCambiosPendientes,
                MensajesBorradorPresupuesto.CambiosDescartadosEnOtraPestana);
        }

        string? huellaPersistida = await _repositorio.ObtenerHuellaPersistidaAsync(
            presupuestoId, cancellationToken);

        bool fueGuardadoEnOtraPestana = !string.IsNullOrWhiteSpace(huellaPersistida)
            && _almacenBorrador.CoincideConUltimoGuardadoEnSesion(presupuestoId, usuario.Id, huellaPersistida);

        if (fueGuardadoEnOtraPestana)
        {
            return new ResultadoAccionDescartarCambios(
                ResultadoDescartarCambios.SinCambiosPendientes,
                MensajeYaGuardadosEnOtraPestana);
        }

        return new ResultadoAccionDescartarCambios(
            ResultadoDescartarCambios.SinCambiosPendientes,
            MensajeSinCambios);
    }
}
