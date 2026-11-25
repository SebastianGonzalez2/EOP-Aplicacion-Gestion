using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// S3-T15 (US-003, US-042, RF-05, RF-69): decide qué pasa con los cambios sin guardar cuando el
// usuario quiere cerrar sesión.
//
// Hay tres caminos, que elige el usuario en la advertencia:
// - Guardar: guarda cada presupuesto con el MISMO caso de uso del botón «Guardar borrador».
// - Descartar: descarta cada presupuesto con el mismo caso de uso del botón «Descartar cambios».
// - Continuar editando: no llega acá, simplemente se cierra la advertencia.
//
// Reglas importantes:
// 1. Antes de tocar nada comprobamos que la lista que vio el usuario sea la misma que hay ahora en
//    la sesión. Si otra pestaña cambió algo, no guardamos ni descartamos nada a ciegas.
// 2. Cada presupuesto se guarda en su propia transacción: no es "todo o nada". Si uno falla, los
//    que ya se guardaron quedan guardados y el que falló conserva sus cambios temporales.
// 3. La sesión solo se puede cerrar si al final no queda NINGÚN cambio pendiente. Ese último
//    control y el vaciado de la sesión se hacen juntos dentro del bloqueo, para que otra pestaña
//    no meta un cambio nuevo justo en el medio y se pierda sin que nadie se entere.
public class ResolverCambiosPendientesAlCerrarSesion
{
    public const string MensajeListaDesactualizada =
        "Los cambios sin guardar cambiaron desde que se abrió la advertencia. Revisá la lista actualizada antes de cerrar sesión.";

    public const string MensajeGuardado = "Cambios guardados.";

    public const string MensajeDescartado = "Cambios descartados.";

    private readonly GuardarBorradorPresupuesto _guardarBorrador;
    private readonly DescartarCambiosPresupuesto _descartarCambios;
    private readonly ConsultarCambiosPendientesDelUsuario _consultarPendientes;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;
    private readonly ISincronizadorBorradorPresupuesto _sincronizador;

    public ResolverCambiosPendientesAlCerrarSesion(
        GuardarBorradorPresupuesto guardarBorrador,
        DescartarCambiosPresupuesto descartarCambios,
        ConsultarCambiosPendientesDelUsuario consultarPendientes,
        IAlmacenBorradorPresupuesto almacenBorrador,
        ISincronizadorBorradorPresupuesto sincronizador)
    {
        _guardarBorrador = guardarBorrador;
        _descartarCambios = descartarCambios;
        _consultarPendientes = consultarPendientes;
        _almacenBorrador = almacenBorrador;
        _sincronizador = sincronizador;
    }

    // Último paso antes de cerrar sesión. Si no queda ningún cambio pendiente, vacía la sesión
    // temporal y devuelve true. Si queda alguno, no toca nada y devuelve false.
    // También se usa en el cierre de sesión "normal", cuando el usuario no tenía nada pendiente.
    public Task<bool> PrepararCierreSinPendientesAsync(int usuarioId, CancellationToken cancellationToken)
    {
        return _sincronizador.EjecutarConBloqueoAsync(
            usuarioId,
            async () =>
            {
                await _almacenBorrador.CargarAsync(cancellationToken);

                List<BorradorPresupuestoDto> pendientes = _almacenBorrador.ObtenerPendientesDelUsuario(usuarioId);
                if (pendientes.Count > 0)
                {
                    return false;
                }

                // Vaciamos todo para que ningún dato temporal quede disponible para la próxima
                // persona que inicie sesión en este mismo navegador.
                _almacenBorrador.VaciarDatosTemporales();
                await _almacenBorrador.ConfirmarAsync(cancellationToken);
                return true;
            },
            cancellationToken);
    }

    public async Task<ResultadoCierreSesionConPendientes> GuardarTodoAsync(
        Usuario usuario,
        IReadOnlyList<BorradorEsperadoDto> borradoresEsperados,
        CancellationToken cancellationToken)
    {
        List<CambioPendienteDto> pendientesActuales = await _consultarPendientes.EjecutarAsync(
            usuario.Id, cancellationToken);

        if (!CoincidenConLaAdvertencia(pendientesActuales, borradoresEsperados))
        {
            return new ResultadoCierreSesionConPendientes
            {
                PuedeCerrarSesion = false,
                MensajeGeneral = MensajeListaDesactualizada
            };
        }

        var resultados = new List<ResultadoPresupuestoAlCerrarSesion>();
        bool seGuardaronTodos = true;

        foreach (CambioPendienteDto pendiente in pendientesActuales)
        {
            // Le pasamos al guardado la misma "foto" del borrador que vio el usuario. Si otra
            // pestaña lo cambió en el medio, GuardarBorradorPresupuesto lo rechaza por su cuenta.
            var contextoEdicion = new ContextoEdicionBorrador(
                EsperabaCambiosPendientes: true,
                VersionBorradorEsperada: pendiente.Version,
                HuellaEstadoBaseEsperada: null,
                BorradorIdEsperado: pendiente.BorradorId);

            ResultadoAccionGuardarBorrador resultadoGuardado = await _guardarBorrador.EjecutarAsync(
                pendiente.PresupuestoId, contextoEdicion, usuario, cancellationToken);

            if (resultadoGuardado.GuardadoCorrectamente)
            {
                resultados.Add(new ResultadoPresupuestoAlCerrarSesion(
                    pendiente.PresupuestoId, pendiente.Codigo, true, MensajeGuardado));
            }
            else
            {
                seGuardaronTodos = false;

                string mensajeError = "No se pudieron guardar los cambios.";
                if (!string.IsNullOrWhiteSpace(resultadoGuardado.MensajeError))
                {
                    mensajeError = resultadoGuardado.MensajeError;
                }

                resultados.Add(new ResultadoPresupuestoAlCerrarSesion(
                    pendiente.PresupuestoId, pendiente.Codigo, false, mensajeError));
            }
        }

        // Si algún guardado falló, la sesión sigue abierta: el usuario tiene que ver qué pasó y
        // decidir qué hacer con ese presupuesto.
        if (!seGuardaronTodos)
        {
            return new ResultadoCierreSesionConPendientes
            {
                PuedeCerrarSesion = false,
                Resultados = resultados
            };
        }

        return await TerminarAsync(usuario.Id, resultados, cancellationToken);
    }

    public async Task<ResultadoCierreSesionConPendientes> DescartarTodoAsync(
        Usuario usuario,
        IReadOnlyList<BorradorEsperadoDto> borradoresEsperados,
        CancellationToken cancellationToken)
    {
        List<CambioPendienteDto> pendientesActuales = await _consultarPendientes.EjecutarAsync(
            usuario.Id, cancellationToken);

        // Nunca descartamos borradores que el usuario no vio en la advertencia.
        if (!CoincidenConLaAdvertencia(pendientesActuales, borradoresEsperados))
        {
            return new ResultadoCierreSesionConPendientes
            {
                PuedeCerrarSesion = false,
                MensajeGeneral = MensajeListaDesactualizada
            };
        }

        var resultados = new List<ResultadoPresupuestoAlCerrarSesion>();
        bool seDescartaronTodos = true;

        foreach (CambioPendienteDto pendiente in pendientesActuales)
        {
            ResultadoAccionDescartarCambios resultadoDescarte = await _descartarCambios.EjecutarAsync(
                pendiente.PresupuestoId, pendiente.BorradorId, pendiente.Version, usuario, cancellationToken);

            if (resultadoDescarte.Estado == ResultadoDescartarCambios.BorradorDesactualizado)
            {
                seDescartaronTodos = false;
                resultados.Add(new ResultadoPresupuestoAlCerrarSesion(
                    pendiente.PresupuestoId, pendiente.Codigo, false, resultadoDescarte.Mensaje));
            }
            else
            {
                // Descartado, o ya no había nada pendiente: en los dos casos ese presupuesto ya no
                // impide cerrar sesión.
                resultados.Add(new ResultadoPresupuestoAlCerrarSesion(
                    pendiente.PresupuestoId, pendiente.Codigo, true, MensajeDescartado));
            }
        }

        if (!seDescartaronTodos)
        {
            return new ResultadoCierreSesionConPendientes
            {
                PuedeCerrarSesion = false,
                Resultados = resultados
            };
        }

        return await TerminarAsync(usuario.Id, resultados, cancellationToken);
    }

    private async Task<ResultadoCierreSesionConPendientes> TerminarAsync(
        int usuarioId,
        List<ResultadoPresupuestoAlCerrarSesion> resultados,
        CancellationToken cancellationToken)
    {
        bool puedeCerrar = await PrepararCierreSinPendientesAsync(usuarioId, cancellationToken);

        if (!puedeCerrar)
        {
            // Mientras guardábamos o descartábamos, otra pestaña dejó un cambio nuevo.
            return new ResultadoCierreSesionConPendientes
            {
                PuedeCerrarSesion = false,
                MensajeGeneral = MensajeListaDesactualizada,
                Resultados = resultados
            };
        }

        return new ResultadoCierreSesionConPendientes
        {
            PuedeCerrarSesion = true,
            Resultados = resultados
        };
    }

    // Compara presupuesto por presupuesto lo que vio el usuario con lo que hay ahora.
    // Tienen que ser exactamente los mismos borradores, con el mismo identificador y versión.
    private static bool CoincidenConLaAdvertencia(
        List<CambioPendienteDto> pendientesActuales,
        IReadOnlyList<BorradorEsperadoDto> borradoresEsperados)
    {
        if (pendientesActuales.Count != borradoresEsperados.Count)
        {
            return false;
        }

        foreach (CambioPendienteDto pendiente in pendientesActuales)
        {
            BorradorEsperadoDto? esperado = null;
            foreach (BorradorEsperadoDto candidato in borradoresEsperados)
            {
                if (candidato.PresupuestoId == pendiente.PresupuestoId)
                {
                    esperado = candidato;
                    break;
                }
            }

            if (esperado == null)
            {
                return false;
            }

            bool mismoBorrador = string.Equals(esperado.BorradorId, pendiente.BorradorId, StringComparison.Ordinal);
            bool mismaVersion = esperado.Version == pendiente.Version;

            if (!mismoBorrador || !mismaVersion)
            {
                return false;
            }
        }

        return true;
    }
}
