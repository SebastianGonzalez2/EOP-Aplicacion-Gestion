// Maneja las cuentas bancarias del formulario de alta de proveedores: agregar y quitar bloques
// de campos sin recargar la página. Es solo comodidad para el usuario: los permisos y las
// validaciones de verdad siempre se hacen en el servidor (RNF-4 y RNF-10).

document.addEventListener("DOMContentLoaded", iniciarCuentasBancarias);

function iniciarCuentasBancarias() {
    const listaDeCuentas = document.getElementById("lista-cuentas-bancarias");
    const botonAgregar = document.getElementById("boton-agregar-cuenta");

    botonAgregar.addEventListener("click", function () {
        agregarCuenta(listaDeCuentas);
    });

    // Escuchamos los clics en la lista completa y no en cada botón "Quitar", porque así también
    // funciona con las cuentas que se agreguen después. closest() busca el botón aunque
    // el clic haya caído en algo que está adentro de él.
    listaDeCuentas.addEventListener("click", function (evento) {
        const botonQuitar = evento.target.closest(".boton-quitar-cuenta");

        if (botonQuitar !== null) {
            const cuenta = botonQuitar.closest(".cuenta-bancaria");
            quitarCuenta(cuenta, listaDeCuentas, botonAgregar);
        }
    });

    actualizarCuentas(listaDeCuentas);
    iniciarConfirmacionDelAlta(listaDeCuentas.closest("form"));
}

// RF-23: el alta necesita la confirmación expresa del usuario. Para eso abrimos la ventana de
// confirmación que está en la vista (un modal de Bootstrap).
function iniciarConfirmacionDelAlta(formulario) {
    const botonRegistrar = formulario.querySelector("button[type='submit']");
    const textoOriginalDelBoton = botonRegistrar.textContent;
    const ventanaDeConfirmacion = document.getElementById("modal-confirmar-alta");
    const botonConfirmar = document.getElementById("boton-confirmar-alta");
    const modal = bootstrap.Modal.getOrCreateInstance(ventanaDeConfirmacion);

    // A diferencia del cuadro del navegador, el modal no deja el código esperando la respuesta.
    // Por eso frenamos siempre este envío, y el formulario sale recién si el usuario confirma.
    // Si cancela no hay nada más que hacer: queda como estaba y no se guarda nada.
    formulario.addEventListener("submit", function (evento) {
        evento.preventDefault();
        modal.show();
    });

    // El foco acompaña a quien usa el teclado o un lector de pantalla: entra a la
    // ventana cuando se abre y vuelve al formulario cuando se cierra.
    ventanaDeConfirmacion.addEventListener("shown.bs.modal", function () {
        botonConfirmar.focus();
    });

    ventanaDeConfirmacion.addEventListener("hidden.bs.modal", function () {
        botonRegistrar.focus();
    });

    botonConfirmar.addEventListener("click", function () {
        // Le mostramos que se está procesando (RNF-19 y RNF-20) y evitamos un segundo envío por
        // un doble clic. Ese segundo envío no podría duplicar nada, pero le mostraría
        // "ya existe" a alguien cuyo proveedor se acaba de guardar bien.
        botonConfirmar.disabled = true;
        botonRegistrar.disabled = true;
        botonRegistrar.textContent = "Registrando...";
        modal.hide();

        // submit() envía sin volver a disparar el evento "submit" de arriba.
        // Si lo disparara, se abriría de nuevo la ventana en vez de enviar.
        formulario.submit();
    });

    // Si el usuario vuelve con el botón Atrás, el navegador puede restaurar la página tal como
    // quedó, con los botones deshabilitados. "pageshow" también corre en ese caso, así que los reponemos.
    window.addEventListener("pageshow", function () {
        botonConfirmar.disabled = false;
        botonRegistrar.disabled = false;
        botonRegistrar.textContent = textoOriginalDelBoton;
    });
}

function agregarCuenta(listaDeCuentas) {
    // Copiamos la primera cuenta en vez de escribir el HTML acá otra vez. Así la lista de
    // bancos y monedas sigue viniendo únicamente del servidor.
    const primeraCuenta = listaDeCuentas.querySelector(".cuenta-bancaria");
    const cuentaNueva = primeraCuenta.cloneNode(true);

    limpiarCuenta(cuentaNueva);
    listaDeCuentas.appendChild(cuentaNueva);
    actualizarCuentas(listaDeCuentas);

    // Llevamos el foco a la cuenta nueva, para que quien usa el teclado o un lector de
    // pantalla siga directamente desde ahí.
    cuentaNueva.querySelector("select").focus();
}

function quitarCuenta(cuenta, listaDeCuentas, botonAgregar) {
    const cantidadDeCuentas = listaDeCuentas.querySelectorAll(".cuenta-bancaria").length;

    // RF-23: un proveedor necesita al menos una cuenta. El botón ya está oculto en ese caso,
    // pero lo controlamos igual para que nunca nos quedemos sin un bloque para copiar.
    if (cantidadDeCuentas === 1) {
        return;
    }

    cuenta.remove();
    actualizarCuentas(listaDeCuentas);

    // El botón que tenía el foco acaba de desaparecer, así que lo llevamos a otro lado.
    botonAgregar.focus();
}

// La copia trae todo lo que tenía la primera cuenta: los valores escritos y, si el servidor
// devolvió el formulario con errores, también sus mensajes. La cuenta nueva arranca limpia.
function limpiarCuenta(cuenta) {
    const campos = cuenta.querySelectorAll("input, select");

    for (const campo of campos) {
        campo.value = "";
        campo.classList.remove("input-validation-error");
    }

    const mensajesDeError = cuenta.querySelectorAll("[data-valmsg-for]");

    for (const mensaje of mensajesDeError) {
        mensaje.textContent = "";
        mensaje.classList.remove("field-validation-error");
        mensaje.classList.add("field-validation-valid");
    }
}

// Deja todas las cuentas numeradas según su posición actual. Se llama cada vez que cambia la lista.
function actualizarCuentas(listaDeCuentas) {
    const cuentas = listaDeCuentas.querySelectorAll(".cuenta-bancaria");

    for (let indice = 0; indice < cuentas.length; indice++) {
        const cuenta = cuentas[indice];
        const numeroVisible = indice + 1;

        cuenta.querySelector(".titulo-cuenta").textContent = "Cuenta bancaria " + numeroVisible;
        renumerarCampos(cuenta, indice);

        const botonQuitar = cuenta.querySelector(".boton-quitar-cuenta");
        botonQuitar.textContent = "Quitar cuenta " + numeroVisible;

        // Con una sola cuenta no se ofrece quitarla.
        if (cuentas.length === 1) {
            botonQuitar.hidden = true;
        } else {
            botonQuitar.hidden = false;
        }
    }
}

// Este paso es el que hace que el servidor reciba todas las cuentas. ASP.NET Core lee
// CuentasBancarias[0], [1], [2]... y se detiene en el primer número que falta: si se quita la
// cuenta del medio y quedan [0] y [2], la [2] se pierde sin ningún error. Por eso, después de
// cada cambio volvemos a numerar desde 0 y sin huecos.
//
// El mismo índice aparece escrito de dos formas, porque un id no lleva corchetes:
//   name y data-valmsg-for  ->  CuentasBancarias[0].Banco
//   id y for                ->  CuentasBancarias_0__Banco
function renumerarCampos(cuenta, indice) {
    const nombreConCorchetes = "CuentasBancarias[" + indice + "]";
    const nombreConGuiones = "CuentasBancarias_" + indice + "__";

    const elementos = cuenta.querySelectorAll("[name], [data-valmsg-for], [id], [for]");

    for (const elemento of elementos) {
        reemplazarIndice(elemento, "name", /CuentasBancarias\[\d+\]/, nombreConCorchetes);
        reemplazarIndice(elemento, "data-valmsg-for", /CuentasBancarias\[\d+\]/, nombreConCorchetes);
        reemplazarIndice(elemento, "id", /CuentasBancarias_\d+__/, nombreConGuiones);
        reemplazarIndice(elemento, "for", /CuentasBancarias_\d+__/, nombreConGuiones);
    }
}

function reemplazarIndice(elemento, atributo, patron, reemplazo) {
    const valorActual = elemento.getAttribute(atributo);

    if (valorActual !== null) {
        elemento.setAttribute(atributo, valorActual.replace(patron, reemplazo));
    }
}
