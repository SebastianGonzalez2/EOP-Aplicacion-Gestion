const resumenConcepto = document.querySelector(".resumen-errores.validation-summary-errors");
if (resumenConcepto) {
    resumenConcepto.focus();
}

// US-003 (RNF-20): mientras se guarda el borrador en el servidor, deshabilitamos el botón y
// mostramos el indicador de progreso para evitar envíos duplicados.
const formularioGuardarBorrador = document.getElementById("formulario-guardar-borrador");
if (formularioGuardarBorrador) {
    formularioGuardarBorrador.addEventListener("submit", (evento) => {
        const botonGuardar = formularioGuardarBorrador.querySelector("button[type=submit]");
        if (!botonGuardar || botonGuardar.disabled) {
            evento.preventDefault();
            return;
        }

        botonGuardar.disabled = true;

        const icono = botonGuardar.querySelector("[data-icono-guardar]");
        if (icono) {
            icono.classList.add("d-none");
        }

        const indicador = botonGuardar.querySelector(".spinner-border");
        if (indicador) {
            indicador.classList.remove("d-none");
        }

        const texto = botonGuardar.querySelector("[data-texto-guardar]");
        if (texto) {
            texto.textContent = "Guardando…";
        }
    });
}

// S3-T15 (RNF-20): al confirmar el descarte, bloqueamos los botones del diálogo y mostramos el
// indicador de progreso para que no se pueda confirmar dos veces.
const formularioDescartarCambios = document.getElementById("formulario-descartar-cambios");
if (formularioDescartarCambios) {
    formularioDescartarCambios.addEventListener("submit", (evento) => {
        const botonDescartar = formularioDescartarCambios.querySelector("button[type=submit]");
        if (!botonDescartar || botonDescartar.disabled) {
            evento.preventDefault();
            return;
        }

        formularioDescartarCambios.querySelectorAll("button").forEach((boton) => {
            boton.disabled = true;
        });

        const indicador = botonDescartar.querySelector(".spinner-border");
        if (indicador) {
            indicador.classList.remove("d-none");
        }

        const texto = botonDescartar.querySelector("[data-texto-guardar]");
        if (texto) {
            texto.textContent = "Descartando…";
        }
    });
}

const formularioConcepto = document.getElementById("formulario-concepto");
if (formularioConcepto) {
    formularioConcepto.addEventListener("submit", (evento) => {
        const botonAgregar = formularioConcepto.querySelector("button[type=submit]");
        if (!botonAgregar || botonAgregar.disabled) {
            evento.preventDefault();
            return;
        }

        botonAgregar.disabled = true;

        const iconoAgregar = botonAgregar.querySelector("[data-icono-agregar]");
        if (iconoAgregar) {
            iconoAgregar.classList.add("d-none");
        }

        const indicador = botonAgregar.querySelector(".spinner-border");
        if (indicador) {
            indicador.classList.remove("d-none");
        }

        const texto = botonAgregar.querySelector("[data-texto-guardar]");
        if (texto) {
            texto.textContent = "Agregando…";
        }
    });
}

// US-007: en la pantalla de selección de material hay un formulario por cada referencia.
// Al enviar uno se bloquean todos los botones para que no se pueda elegir dos veces (RNF-20).
const formulariosSeleccionMaterial = document.querySelectorAll(".form-seleccion-material");
formulariosSeleccionMaterial.forEach((formulario) => {
    formulario.addEventListener("submit", (evento) => {
        const botonEnviado = formulario.querySelector("button[type=submit]");
        if (!botonEnviado || botonEnviado.disabled) {
            evento.preventDefault();
            return;
        }

        formulariosSeleccionMaterial.forEach((otroFormulario) => {
            const otroBoton = otroFormulario.querySelector("button[type=submit]");
            if (otroBoton) {
                otroBoton.disabled = true;
            }
        });

        const indicador = botonEnviado.querySelector(".spinner-border");
        if (indicador) {
            indicador.classList.remove("d-none");
        }

        const texto = botonEnviado.querySelector("[data-texto-guardar]");
        if (texto) {
            texto.textContent = "Aplicando…";
        }
    });
});

// UI-presupuestos (US-011): modal «Agregar concepto».
//
// El modal tiene una sección de campos por cada clase de tipo (Material, importe manual y
// Margen). Al elegir el tipo mostramos solo la sección que corresponde y deshabilitamos las
// demás: un campo deshabilitado no se envía ni se valida, así el servidor recibe solo los datos
// del tipo elegido. El servidor igual decide por el tipo y vuelve a validar todo (RNF-10).
//
// También se encarga de que no se pierdan datos escritos sin querer (S3-T15): si el usuario
// cierra el modal con datos escritos, primero le preguntamos, dentro del mismo modal.
// Va entre llaves para que sus nombres no choquen con los de otros scripts de la página.
{
    const modalAgregarConcepto = document.getElementById("modalAgregarConcepto");
    const formularioAgregar = document.getElementById("formulario-concepto");

    if (modalAgregarConcepto && formularioAgregar) {
        const selectorTipo = formularioAgregar.querySelector("[data-selector-tipo]");
        const secciones = formularioAgregar.querySelectorAll("[data-seccion-tipo]");
        const selectorMaterial = formularioAgregar.querySelector("[data-selector-material]");
        const unidadCantidad = formularioAgregar.querySelector("[data-unidad-cantidad]");
        const pasoFormulario = formularioAgregar.querySelector('[data-paso-modal="formulario"]');
        const pasoConfirmarSalida = formularioAgregar.querySelector('[data-paso-modal="confirmar-salida"]');
        const pieFormulario = formularioAgregar.querySelector('[data-pie-modal="formulario"]');
        const pieConfirmarSalida = formularioAgregar.querySelector('[data-pie-modal="confirmar-salida"]');

        // Se pone en true cuando el usuario ya decidió salir sin agregar: el próximo cierre
        // del modal no se frena.
        let cierrePermitido = false;

        // Qué sección corresponde al tipo elegido ("material", "importe", "margen" o "ninguna").
        // La vista lo anota en cada opción del desplegable (data-seccion).
        function seccionDelTipoElegido() {
            const opcion = selectorTipo.options[selectorTipo.selectedIndex];
            if (!opcion || !opcion.dataset.seccion) {
                return "ninguna";
            }

            return opcion.dataset.seccion;
        }

        function mostrarSeccionDelTipo() {
            const seccionElegida = seccionDelTipoElegido();

            secciones.forEach((seccion) => {
                const esLaElegida = seccion.dataset.seccionTipo === seccionElegida;
                seccion.classList.toggle("d-none", !esLaElegida);

                seccion.querySelectorAll("input, select, textarea, button").forEach((campo) => {
                    // Lo que el servidor dibujó deshabilitado a propósito queda así siempre.
                    if (campo.dataset.siempreDeshabilitado === "true") {
                        return;
                    }

                    campo.disabled = !esLaElegida;
                });
            });

            // El catálogo se consulta recién cuando hace falta.
            if (seccionElegida === "material" && selectorMaterial) {
                selectorMaterial.dispatchEvent(new CustomEvent("selector:activar"));
            }
        }

        function hayDatosEscritos() {
            // tieneDatosNoEnviados está en cambios-pendientes.js (se carga antes que este archivo)
            // y es la misma comprobación que usa la advertencia nativa del navegador.
            if (typeof tieneDatosNoEnviados === "function") {
                return tieneDatosNoEnviados(formularioAgregar);
            }

            return false;
        }

        function mostrarPasoConfirmarSalida() {
            pasoFormulario.classList.add("d-none");
            pieFormulario.classList.add("d-none");
            pasoConfirmarSalida.classList.remove("d-none");
            pieConfirmarSalida.classList.remove("d-none");

            // Foco inicial en la opción que no pierde nada (docs/diseno-visual.md, sección 20.2).
            pieConfirmarSalida.querySelector("[data-volver-al-formulario]").focus();
        }

        function mostrarPasoFormulario() {
            pasoConfirmarSalida.classList.add("d-none");
            pieConfirmarSalida.classList.add("d-none");
            pasoFormulario.classList.remove("d-none");
            pieFormulario.classList.remove("d-none");
        }

        // Deja el formulario como recién abierto. Cambiamos también los valores "por defecto"
        // (defaultValue, defaultChecked, defaultSelected) porque, si la página volvió del servidor
        // con errores, esos valores por defecto son los datos que el usuario había escrito.
        function limpiarFormulario() {
            for (const campo of formularioAgregar.elements) {
                if (campo.type === "hidden" || campo.type === "submit" || campo.type === "button") {
                    continue;
                }

                if (campo.type === "checkbox" || campo.type === "radio") {
                    campo.checked = false;
                    campo.defaultChecked = false;
                } else if (campo.tagName === "SELECT") {
                    for (const opcion of campo.options) {
                        opcion.defaultSelected = false;
                    }
                    campo.selectedIndex = 0;
                } else {
                    campo.value = "";
                    campo.defaultValue = "";
                }
            }

            formularioAgregar.dataset.datosNoEnviados = "false";

            // Sacamos los errores que había mostrado el servidor.
            const resumenErrores = formularioAgregar.querySelector(".resumen-errores");
            if (resumenErrores) {
                resumenErrores.innerHTML = "";
                resumenErrores.classList.remove("validation-summary-errors");
                resumenErrores.classList.add("validation-summary-valid");
            }

            formularioAgregar.querySelectorAll(".field-validation-error").forEach((error) => {
                error.textContent = "";
            });

            if (selectorMaterial) {
                selectorMaterial.dispatchEvent(new CustomEvent("selector:reiniciar"));
            }

            mostrarSeccionDelTipo();
        }

        selectorTipo.addEventListener("change", () => {
            mostrarSeccionDelTipo();
        });

        // La cantidad se expresa en la unidad del material: la mostramos al lado del campo.
        formularioAgregar.addEventListener("selector:referencia-elegida", (evento) => {
            if (!unidadCantidad) {
                return;
            }

            if (evento.detail && evento.detail.unidad) {
                unidadCantidad.textContent = evento.detail.unidad;
            } else {
                unidadCantidad.textContent = "Unidad";
            }
        });

        // «Cancelar», la X y Escape cierran el modal. Si hay datos escritos, frenamos el cierre y
        // preguntamos primero (S3-T15: esos datos no están en el borrador).
        modalAgregarConcepto.addEventListener("hide.bs.modal", (evento) => {
            if (cierrePermitido) {
                return;
            }

            if (!pasoConfirmarSalida.classList.contains("d-none")) {
                // Ya estamos preguntando: un segundo Escape equivale a «Volver al formulario».
                evento.preventDefault();
                mostrarPasoFormulario();
                selectorTipo.focus();
                return;
            }

            if (hayDatosEscritos()) {
                evento.preventDefault();
                mostrarPasoConfirmarSalida();
            }
        });

        pieConfirmarSalida.querySelector("[data-volver-al-formulario]").addEventListener("click", () => {
            mostrarPasoFormulario();
            selectorTipo.focus();
        });

        pieConfirmarSalida.querySelector("[data-salir-sin-agregar]").addEventListener("click", () => {
            limpiarFormulario();
            cierrePermitido = true;
            bootstrap.Modal.getOrCreateInstance(modalAgregarConcepto).hide();
        });

        // Cada vez que se cierra, la próxima apertura arranca en el formulario.
        modalAgregarConcepto.addEventListener("hidden.bs.modal", () => {
            cierrePermitido = false;
            mostrarPasoFormulario();
        });

        // Al abrir, llevamos el foco a los errores (si el servidor devolvió alguno) o al tipo.
        modalAgregarConcepto.addEventListener("shown.bs.modal", () => {
            const errores = formularioAgregar.querySelector(".resumen-errores.validation-summary-errors");
            if (errores) {
                errores.focus();
            } else {
                selectorTipo.focus();
            }
        });

        mostrarSeccionDelTipo();

        // Si el servidor rechazó el concepto, reabrimos el modal con lo que el usuario escribió.
        if (modalAgregarConcepto.dataset.abrirAlCargar === "true") {
            bootstrap.Modal.getOrCreateInstance(modalAgregarConcepto).show();
        }
    }
}
