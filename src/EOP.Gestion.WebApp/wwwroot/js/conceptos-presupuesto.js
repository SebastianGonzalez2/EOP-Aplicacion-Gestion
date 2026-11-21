const resumenConcepto = document.querySelector(".resumen-errores.validation-summary-errors");
if (resumenConcepto) {
    resumenConcepto.focus();
}

const formularioConcepto = document.getElementById("formulario-concepto");
formularioConcepto?.addEventListener("submit", (evento) => {
    const botonGuardar = formularioConcepto.querySelector("button[type=submit]");
    if (!botonGuardar || botonGuardar.disabled) {
        evento.preventDefault();
        return;
    }

    botonGuardar.disabled = true;
    botonGuardar.querySelector(".spinner-border").classList.remove("d-none");
    botonGuardar.querySelector("[data-texto-guardar]").textContent = "Guardando…";
});

// US-007: en la pantalla de selección de material hay un formulario por cada referencia.
// Al enviar uno se bloquean todos los botones para que no se pueda elegir dos veces (RNF-20).
const formulariosSeleccionMaterial = document.querySelectorAll(".form-seleccion-material");
formulariosSeleccionMaterial.forEach((formulario) => {
    formulario.addEventListener("submit", (evento) => {
        const botonEnviado = formulario.querySelector("button[type=submit]");
        if (botonEnviado.disabled) {
            evento.preventDefault();
            return;
        }

        formulariosSeleccionMaterial.forEach((otroFormulario) => {
            otroFormulario.querySelector("button[type=submit]").disabled = true;
        });
        botonEnviado.querySelector(".spinner-border").classList.remove("d-none");
        botonEnviado.querySelector("[data-texto-guardar]").textContent = "Guardando…";
    });
});
