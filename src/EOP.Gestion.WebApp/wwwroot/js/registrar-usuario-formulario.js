// Mientras se guarda el usuario, deshabilita el botón y avisa que se está registrando (RNF-19).
// Así, un doble clic no manda el formulario dos veces. Es solo comodidad: lo que
// realmente impide un usuario repetido es el índice único de la base.

document.addEventListener("DOMContentLoaded", iniciarFormularioRegistrarUsuario);

function iniciarFormularioRegistrarUsuario() {
    const formulario = document.getElementById("formulario-registrar-usuario");
    const boton = document.getElementById("boton-registrar-usuario");
    const textoOriginalDelBoton = boton.textContent;

    formulario.addEventListener("submit", function () {
        // Si hay errores, jQuery Validation frena el envío y el botón tiene que seguir disponible.
        // mostrarProcesando está en site.js: deshabilita el botón y le agrega el indicador de la guía.
        if ($(formulario).valid()) {
            mostrarProcesando(boton, "Registrando...");
        }
    });

    // Al volver con el botón "Atrás", el navegador puede restaurar la página tal como quedó,
    // con el botón deshabilitado. Por eso lo dejamos disponible cada vez que se muestra.
    window.addEventListener("pageshow", function () {
        quitarProcesando(boton, textoOriginalDelBoton);
    });
}
