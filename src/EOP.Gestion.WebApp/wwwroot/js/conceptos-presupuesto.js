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
