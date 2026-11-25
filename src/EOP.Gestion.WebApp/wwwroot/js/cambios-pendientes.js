// S3-T15 (US-003, US-042, RF-05): advertencias por cambios sin guardar.
//
// Se carga en todas las pantallas con sesión (desde el _Layout) y hace tres cosas:
//
// 1. Cierre de sesión: si el usuario tiene cambios sin guardar en algún presupuesto, al tocar
//    "Cerrar sesión" abre el diálogo con Guardar, Descartar cambios y Continuar editando.
//
// 2. Datos escritos y no enviados: en un formulario marcado con data-advertir-datos-no-enviados
//    (por ejemplo «Agregar concepto»), si el usuario escribió algo y quiere irse sin enviarlo, le
//    avisamos antes. Esos datos NO están en el borrador, así que se perderían.
//
// 3. Advertencia nativa del navegador (beforeunload): al cerrar la pestaña o el navegador,
//    recargar o salir a otro sitio con cambios pendientes, le pedimos al navegador que avise.
//    No se pide en las navegaciones dentro de EOP (enlaces y formularios propios).
//
// Todo esto es comodidad: lo que realmente impide cerrar sesión con cambios pendientes es el
// control del servidor (CuentaController.CerrarSesion). Y nada de este archivo guarda ni
// descarta cambios por su cuenta.

const modalCierreSesion = document.getElementById("modalCambiosPendientesCierre");
const formularioCerrarSesion = document.getElementById("formulario-cerrar-sesion");
const modalDatosNoEnviados = document.getElementById("modalDatosNoEnviados");

// Se pone en true cuando el usuario sale por un enlace o formulario de EOP: en ese caso no
// queremos la advertencia nativa, porque los cambios quedan en la sesión y no se pierden.
let navegacionInternaEnCurso = false;

// Se pone en true un instante, cuando el usuario ya eligió "Seguir sin agregarlos" y volvemos a
// enviar el formulario que había quedado frenado. Así el aviso no aparece dos veces.
let saltearAvisoDatosNoEnviados = false;

// Lo que el usuario quería hacer cuando lo frenamos con el aviso de datos no enviados.
let accionFrenadaPorDatosNoEnviados = null;

// ---------------------------------------------------------------------------------------------
// Funciones de apoyo
// ---------------------------------------------------------------------------------------------

// Indica si una dirección pertenece a EOP (mismo origen: protocolo, dominio y puerto).
function esDireccionDeEop(direccion) {
    try {
        const url = new URL(direccion, window.location.href);
        return url.origin === window.location.origin;
    } catch {
        return false;
    }
}

// Indica si un clic en un enlace va a reemplazar esta página. Si se abre en otra pestaña
// (target="_blank", Ctrl/Cmd + clic, clic del medio) o es una descarga, esta página sigue igual.
function elClicReemplazaLaPagina(evento, enlace) {
    if (evento.button !== 0) {
        return false;
    }

    if (evento.ctrlKey || evento.metaKey || evento.shiftKey || evento.altKey) {
        return false;
    }

    if (enlace.hasAttribute("download")) {
        return false;
    }

    const destino = enlace.getAttribute("target");
    if (destino && destino !== "_self") {
        return false;
    }

    return true;
}

// Índice de la opción que el select tenía al cargarse la página. Si ninguna opción tenía el
// atributo "selected", el navegador muestra la primera.
function indiceInicialDelSelect(select) {
    for (let indice = 0; indice < select.options.length; indice++) {
        if (select.options[indice].defaultSelected) {
            return indice;
        }
    }

    return 0;
}

// Compara cada campo con el valor que tenía cuando se cargó la página.
function tieneDatosNoEnviados(formulario) {
    // El servidor marca el formulario cuando lo devolvió con errores de validación: en ese caso
    // los datos ya vienen escritos desde el principio, pero todavía no se agregaron.
    if (formulario.dataset.datosNoEnviados === "true") {
        return true;
    }

    for (const campo of formulario.elements) {
        if (campo.disabled || campo.type === "hidden" || campo.type === "submit" || campo.type === "button") {
            continue;
        }

        if (campo.type === "checkbox" || campo.type === "radio") {
            if (campo.checked !== campo.defaultChecked) {
                return true;
            }
        } else if (campo.tagName === "SELECT") {
            if (campo.selectedIndex !== indiceInicialDelSelect(campo)) {
                return true;
            }
        } else if (campo.tagName === "INPUT" || campo.tagName === "TEXTAREA") {
            if (campo.value !== campo.defaultValue) {
                return true;
            }
        }
    }

    return false;
}

// Devuelve el primer formulario vigilado que tenga datos sin enviar, o null si no hay ninguno.
function buscarFormularioConDatosNoEnviados() {
    const formulariosVigilados = document.querySelectorAll("form[data-advertir-datos-no-enviados]");

    for (const formulario of formulariosVigilados) {
        if (tieneDatosNoEnviados(formulario)) {
            return formulario;
        }
    }

    return null;
}

// Indica si el usuario tiene cambios sin guardar en algún presupuesto. Lo sabemos porque el
// servidor dibuja el diálogo de cierre de sesión solo cuando hay cambios pendientes, y la
// pantalla de elaboración marca su contenedor con data-cambios-pendientes="true".
function hayCambiosPendientesEnLaSesion() {
    if (document.querySelector("[data-cambios-pendientes-usuario]")) {
        return true;
    }

    if (document.querySelector('[data-borrador-presupuesto][data-cambios-pendientes="true"]')) {
        return true;
    }

    return false;
}

// Muestra un diálogo de Bootstrap. Si ya había otro abierto (por ejemplo, la confirmación de
// descarte), primero lo cierra: Bootstrap no está pensado para dos diálogos encimados.
function mostrarDialogo(dialogo) {
    const dialogoAbierto = document.querySelector(".modal.show");

    if (dialogoAbierto && dialogoAbierto !== dialogo) {
        dialogoAbierto.addEventListener("hidden.bs.modal", () => {
            bootstrap.Modal.getOrCreateInstance(dialogo).show();
        }, { once: true });

        bootstrap.Modal.getOrCreateInstance(dialogoAbierto).hide();
        return;
    }

    bootstrap.Modal.getOrCreateInstance(dialogo).show();
}

// ---------------------------------------------------------------------------------------------
// 2. Datos escritos y no enviados
// ---------------------------------------------------------------------------------------------
//
// Escuchamos en fase de captura (el "true" del final): así nos enteramos del clic o del envío
// ANTES que los demás scripts de la pantalla, y podemos frenarlo si hace falta.

if (modalDatosNoEnviados) {
    document.addEventListener("click", (evento) => {
        const enlace = evento.target.closest("a[href]");
        if (!enlace || !elClicReemplazaLaPagina(evento, enlace)) {
            return;
        }

        const formulario = buscarFormularioConDatosNoEnviados();
        if (!formulario) {
            return;
        }

        // Los enlaces del propio formulario (por ejemplo "Cancelar") son una decisión explícita
        // del usuario de dejar lo que escribió: no lo frenamos.
        if (formulario.contains(enlace)) {
            return;
        }

        evento.preventDefault();
        evento.stopPropagation();

        accionFrenadaPorDatosNoEnviados = () => {
            if (esDireccionDeEop(enlace.href)) {
                navegacionInternaEnCurso = true;
            }

            window.location.assign(enlace.href);
        };

        mostrarDialogo(modalDatosNoEnviados);
    }, true);

    document.addEventListener("submit", (evento) => {
        if (saltearAvisoDatosNoEnviados) {
            return;
        }

        const formularioEnviado = evento.target;
        const formulario = buscarFormularioConDatosNoEnviados();

        // Enviar el propio formulario es justamente "agregarlos": no hay nada que avisar.
        if (!formulario || formulario === formularioEnviado) {
            return;
        }

        evento.preventDefault();
        evento.stopPropagation();

        const botonUsado = evento.submitter;

        accionFrenadaPorDatosNoEnviados = () => {
            // requestSubmit vuelve a disparar el envío (con sus validaciones y scripts), pero esta
            // vez salteando este aviso porque el usuario ya decidió seguir.
            saltearAvisoDatosNoEnviados = true;
            try {
                if (botonUsado) {
                    formularioEnviado.requestSubmit(botonUsado);
                } else {
                    formularioEnviado.requestSubmit();
                }
            } finally {
                saltearAvisoDatosNoEnviados = false;
            }
        };

        mostrarDialogo(modalDatosNoEnviados);
    }, true);

    const botonSeguirSinAgregar = modalDatosNoEnviados.querySelector("[data-seguir-sin-agregar]");
    if (botonSeguirSinAgregar) {
        botonSeguirSinAgregar.addEventListener("click", () => {
            const accion = accionFrenadaPorDatosNoEnviados;
            accionFrenadaPorDatosNoEnviados = null;

            if (!accion) {
                bootstrap.Modal.getOrCreateInstance(modalDatosNoEnviados).hide();
                return;
            }

            // Esperamos a que el diálogo termine de cerrarse antes de seguir: si lo que sigue abre
            // otro diálogo (por ejemplo, el de cierre de sesión), así no se encima con este.
            modalDatosNoEnviados.addEventListener("hidden.bs.modal", () => {
                accion();
            }, { once: true });

            bootstrap.Modal.getOrCreateInstance(modalDatosNoEnviados).hide();
        });
    }

    // "Volver al formulario": cerramos el aviso y dejamos el cursor en el primer campo.
    modalDatosNoEnviados.addEventListener("hidden.bs.modal", () => {
        if (accionFrenadaPorDatosNoEnviados === null) {
            return;
        }

        accionFrenadaPorDatosNoEnviados = null;

        const formulario = buscarFormularioConDatosNoEnviados();
        if (formulario) {
            const primerCampo = formulario.querySelector("input:not([type=hidden]), select, textarea");
            if (primerCampo) {
                primerCampo.focus();
            }
        }
    });
}

// ---------------------------------------------------------------------------------------------
// 1. Cierre de sesión con cambios pendientes
// ---------------------------------------------------------------------------------------------

if (formularioCerrarSesion && modalCierreSesion) {
    formularioCerrarSesion.addEventListener("submit", (evento) => {
        // En lugar de cerrar sesión directamente, mostramos la advertencia. Si el JavaScript no
        // anduviera, el servidor igual frenaría el cierre y mostraría la pantalla de decisión.
        evento.preventDefault();
        mostrarDialogo(modalCierreSesion);
    });
}

if (modalCierreSesion) {
    const pasoDecision = modalCierreSesion.querySelector('[data-paso="decision"]');
    const pasoConfirmarDescarte = modalCierreSesion.querySelector('[data-paso="confirmar-descarte"]');

    const mostrarPaso = (pasoVisible, pasoOculto) => {
        pasoOculto.classList.add("d-none");
        pasoVisible.classList.remove("d-none");

        // Llevamos el foco al título del paso para que el lector de pantalla lo anuncie.
        const titulo = pasoVisible.querySelector("h2");
        if (titulo) {
            modalCierreSesion.setAttribute("aria-labelledby", titulo.id);
            titulo.focus();
        }
    };

    const botonIrADescarte = modalCierreSesion.querySelector("[data-ir-a-confirmar-descarte]");
    if (botonIrADescarte) {
        botonIrADescarte.addEventListener("click", () => {
            mostrarPaso(pasoConfirmarDescarte, pasoDecision);
        });
    }

    const botonVolver = modalCierreSesion.querySelector("[data-volver-a-decision]");
    if (botonVolver) {
        botonVolver.addEventListener("click", () => {
            mostrarPaso(pasoDecision, pasoConfirmarDescarte);
        });
    }

    // Si el usuario cierra el diálogo ("Continuar editando"), la próxima vez arranca de nuevo
    // en el primer paso. No se cambió nada: los cambios siguen en la sesión.
    modalCierreSesion.addEventListener("hidden.bs.modal", () => {
        pasoConfirmarDescarte.classList.add("d-none");
        pasoDecision.classList.remove("d-none");
    });
}

// Indicador de procesamiento (RNF-20) para el formulario de la advertencia, tanto en el diálogo
// como en la pantalla /Cuenta/CambiosPendientes. Evita que se mande dos veces.
document.querySelectorAll("form[data-formulario-decision-cierre]").forEach((formulario) => {
    formulario.addEventListener("submit", (evento) => {
        if (formulario.dataset.enviando === "true") {
            evento.preventDefault();
            return;
        }

        formulario.dataset.enviando = "true";

        const botonUsado = evento.submitter;

        // Un botón deshabilitado no manda su valor. Por eso copiamos la decisión elegida
        // (guardar o descartar) a un campo oculto antes de deshabilitar los botones.
        if (botonUsado && botonUsado.name) {
            const campoDecision = document.createElement("input");
            campoDecision.type = "hidden";
            campoDecision.name = botonUsado.name;
            campoDecision.value = botonUsado.value;
            formulario.appendChild(campoDecision);
        }

        formulario.querySelectorAll("button").forEach((boton) => {
            boton.disabled = true;
        });

        if (botonUsado) {
            const indicador = botonUsado.querySelector(".spinner-border");
            if (indicador) {
                indicador.classList.remove("d-none");
            }

            const texto = botonUsado.querySelector("[data-texto-guardar]");
            if (texto) {
                if (botonUsado.value === "descartar") {
                    texto.textContent = "Descartando…";
                } else {
                    texto.textContent = "Guardando…";
                }
            }
        }
    });
});

// ---------------------------------------------------------------------------------------------
// 3. Advertencia nativa del navegador
// ---------------------------------------------------------------------------------------------
//
// Escuchamos en fase de burbujeo (sin el "true"): así llegamos DESPUÉS que los scripts de la
// pantalla. Si alguno frenó el clic o el envío (por ejemplo, para mostrar un diálogo), no hubo
// navegación y no marcamos nada.

document.addEventListener("click", (evento) => {
    if (evento.defaultPrevented) {
        return;
    }

    const enlace = evento.target.closest("a[href]");
    if (!enlace || !elClicReemplazaLaPagina(evento, enlace)) {
        return;
    }

    if (esDireccionDeEop(enlace.href)) {
        navegacionInternaEnCurso = true;
    }
});

document.addEventListener("submit", (evento) => {
    if (evento.defaultPrevented) {
        return;
    }

    if (esDireccionDeEop(evento.target.action)) {
        navegacionInternaEnCurso = true;
    }
});

window.addEventListener("beforeunload", (evento) => {
    if (navegacionInternaEnCurso) {
        return;
    }

    if (!hayCambiosPendientesEnLaSesion() && !buscarFormularioConDatosNoEnviados()) {
        return;
    }

    // Así se le pide al navegador su advertencia. El texto y los botones los pone el navegador:
    // no se pueden personalizar, y algunos navegadores (sobre todo en celulares) no la muestran.
    evento.preventDefault();
    evento.returnValue = "";
});

// Si el navegador vuelve a mostrar esta página desde su memoria (botón Atrás), la navegación
// anterior ya terminó: volvemos a vigilar.
window.addEventListener("pageshow", () => {
    navegacionInternaEnCurso = false;
});
