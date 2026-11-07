// Funciones de JavaScript compartidas por todas las pantallas. Se carga en el _Layout,
// antes de los scripts propios de cada vista, así que estas funciones ya están disponibles ahí.

// Indicador de procesamiento de la guía (sección 18.2 de docs/diseno-visual.md, RNF-20):
// deshabilita el botón y le pone un circulito girando delante del texto, por ejemplo
// "Registrando...". Así el usuario ve que la operación está en curso y no puede apretar dos veces.
//
// Es solo comodidad visual: lo que impide un registro repetido sigue estando en el servidor.
function mostrarProcesando(boton, texto) {
    boton.disabled = true;
    boton.textContent = texto;

    const indicador = document.createElement("span");
    indicador.className = "spinner-border spinner-border-sm me-1";
    // El texto del botón ya dice lo que pasa; el circulito es solo decorativo para un lector de pantalla.
    indicador.setAttribute("aria-hidden", "true");
    boton.prepend(indicador);
}

// Deja el botón como estaba. Al asignar textContent también se va el circulito.
function quitarProcesando(boton, textoOriginal) {
    boton.disabled = false;
    boton.textContent = textoOriginal;
}
