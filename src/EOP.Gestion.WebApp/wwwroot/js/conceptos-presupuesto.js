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
