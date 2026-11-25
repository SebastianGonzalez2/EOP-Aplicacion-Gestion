// UI-presupuestos (US-007, US-010A, US-011): selector de materiales del presupuesto.
//
// Se usa en el modal «Agregar concepto» y en la pantalla «Seleccionar material» de una fila. Lo
// arma la vista parcial _SelectorMaterial.cshtml y funciona en dos pasos, sin recargar la página:
//
//   1. Listado del catálogo: pide al servidor una página de materiales (con o sin búsqueda) y la
//      muestra. El usuario toca «Elegir» en el material que quiere.
//   2. Referencias: pide al servidor las referencias de proveedor de ese material y las muestra
//      como botones de opción. El usuario tiene que marcar una; ninguna viene marcada.
//
// El servidor devuelve pedazos de HTML ya armados (vistas parciales), así los textos, los formatos
// de números y fechas y el escapado de caracteres se resuelven en un solo lugar: Razor.
//
// Esto es solo comodidad para elegir. Lo que realmente se agrega o asigna lo decide el servidor al
// recibir el formulario, y ahí se vuelven a comprobar todas las reglas (RNF-10).
//
// Para que otra parte de la pantalla pueda manejar el selector, escucha dos eventos propios:
//   - "selector:activar": arranca (carga el listado o restaura lo elegido). Lo usa el modal, que
//     no quiere consultar el catálogo hasta que el usuario elige el tipo Material.
//   - "selector:reiniciar": vuelve todo a cero (por ejemplo, cuando se descartan los datos).
// Y avisa con "selector:referencia-elegida" cuando cambia la referencia marcada.

(function () {
    // Pide un pedazo de HTML al servidor. Si la respuesta no es la esperada (error, falta de
    // permisos, sesión vencida que redirige al inicio de sesión), lanza un error.
    async function traerHtml(url) {
        const respuesta = await fetch(url, {
            headers: { "X-Requested-With": "XMLHttpRequest" },
            credentials: "same-origin"
        });

        if (!respuesta.ok || respuesta.redirected) {
            throw new Error("No se pudo obtener la información del catálogo.");
        }

        return await respuesta.text();
    }

    // Muestra el bloque de procesamiento (RNF-20) mientras esperamos la respuesta.
    function mostrarCargando(zona, texto) {
        zona.innerHTML = "";

        const bloque = document.createElement("div");
        bloque.className = "procesando";
        bloque.setAttribute("role", "status");

        const indicador = document.createElement("span");
        indicador.className = "spinner-border";
        indicador.setAttribute("aria-hidden", "true");

        const mensaje = document.createElement("div");
        mensaje.textContent = texto;

        bloque.appendChild(indicador);
        bloque.appendChild(mensaje);
        zona.appendChild(bloque);
    }

    // Si algo falla, lo decimos claramente y el resto de la pantalla sigue funcionando (RNF-13).
    function mostrarError(zona, texto) {
        zona.innerHTML = "";

        const bloque = document.createElement("div");
        bloque.className = "alert mensaje mensaje-error mb-0";
        bloque.setAttribute("role", "alert");

        const icono = document.createElement("i");
        icono.className = "bi bi-x-octagon";
        icono.setAttribute("aria-hidden", "true");

        const mensaje = document.createElement("div");
        mensaje.className = "mensaje-cuerpo";
        mensaje.textContent = texto;

        bloque.appendChild(icono);
        bloque.appendChild(mensaje);
        zona.appendChild(bloque);
    }

    function prepararSelector(contenedor) {
        const urlListado = contenedor.dataset.urlListado;
        const urlReferencias = contenedor.dataset.urlReferencias;
        const paraFila = contenedor.dataset.paraFila === "true";
        const referenciaActualId = contenedor.dataset.referenciaActualId || "";

        // Si el servidor devolvió la pantalla con errores, esta es la referencia que el usuario
        // había marcado. La usamos una sola vez, para restaurar la selección.
        let referenciaParaRestaurar = contenedor.dataset.referenciaElegidaId || "";

        const campoMaterial = contenedor.querySelector("[data-campo-material]");
        const buscador = contenedor.querySelector("[data-buscador-material]");
        const botonBuscar = contenedor.querySelector("[data-buscar-material]");
        const pasoListado = contenedor.querySelector('[data-paso-selector="listado"]');
        const pasoReferencias = contenedor.querySelector('[data-paso-selector="referencias"]');
        const zonaListado = contenedor.querySelector("[data-zona-listado]");
        const zonaReferencias = contenedor.querySelector("[data-zona-referencias]");
        const resumen = contenedor.querySelector("[data-resumen-referencia]");
        const botonElegirOtro = contenedor.querySelector("[data-elegir-otro-material]");

        let listadoCargado = false;

        // Cada pedido lleva un número. Si el usuario pide otra página antes de que llegue la
        // anterior, ignoramos la respuesta vieja para no pisar la nueva.
        let ultimoPedidoListado = 0;
        let ultimoPedidoReferencias = 0;

        async function cargarListado(pagina) {
            ultimoPedidoListado = ultimoPedidoListado + 1;
            const numeroPedido = ultimoPedidoListado;

            const parametros = new URLSearchParams();
            parametros.set("busqueda", buscador.value.trim());
            parametros.set("pagina", String(pagina));

            mostrarCargando(zonaListado, "Cargando materiales del catálogo…");

            try {
                const html = await traerHtml(urlListado + "?" + parametros.toString());
                if (numeroPedido !== ultimoPedidoListado) {
                    return;
                }

                zonaListado.innerHTML = html;
                listadoCargado = true;
            } catch {
                if (numeroPedido !== ultimoPedidoListado) {
                    return;
                }

                mostrarError(zonaListado,
                    "No se pudo cargar el catálogo de materiales. Probá de nuevo; si sigue fallando, recargá la página.");
            }
        }

        function ocultarResumen() {
            resumen.classList.add("d-none");
        }

        // Muestra la unidad, el precio y la moneda de la referencia marcada. Usamos textContent
        // (no innerHTML) para que nada de lo que venga en los datos se interprete como HTML.
        function actualizarResumen() {
            const marcada = zonaReferencias.querySelector("input[type=radio]:checked");

            if (!marcada) {
                ocultarResumen();
                avisarReferenciaElegida(null);
                return;
            }

            resumen.querySelector("[data-resumen-proveedor]").textContent = marcada.dataset.proveedor;
            resumen.querySelector("[data-resumen-unidad]").textContent = marcada.dataset.unidad;
            resumen.querySelector("[data-resumen-precio]").textContent = marcada.dataset.precio;
            resumen.querySelector("[data-resumen-moneda]").textContent = marcada.dataset.moneda;
            resumen.classList.remove("d-none");

            avisarReferenciaElegida(marcada);
        }

        function avisarReferenciaElegida(radioMarcado) {
            let unidad = null;
            if (radioMarcado) {
                unidad = radioMarcado.dataset.unidad;
            }

            contenedor.dispatchEvent(new CustomEvent("selector:referencia-elegida", {
                bubbles: true,
                detail: { unidad: unidad }
            }));
        }

        async function elegirMaterial(materialId) {
            campoMaterial.value = materialId;
            ocultarResumen();
            avisarReferenciaElegida(null);

            pasoListado.classList.add("d-none");
            pasoReferencias.classList.remove("d-none");

            ultimoPedidoReferencias = ultimoPedidoReferencias + 1;
            const numeroPedido = ultimoPedidoReferencias;

            const parametros = new URLSearchParams();
            parametros.set("materialId", String(materialId));
            parametros.set("paraFila", paraFila ? "true" : "false");
            if (referenciaParaRestaurar) {
                parametros.set("referenciaSeleccionadaId", referenciaParaRestaurar);
                referenciaParaRestaurar = "";
            }
            if (referenciaActualId) {
                parametros.set("referenciaActualId", referenciaActualId);
            }

            mostrarCargando(zonaReferencias, "Cargando las referencias del material…");

            try {
                const html = await traerHtml(urlReferencias + "?" + parametros.toString());
                if (numeroPedido !== ultimoPedidoReferencias) {
                    return;
                }

                zonaReferencias.innerHTML = html;

                // Si el campo quedó deshabilitado (por ejemplo, el modal ocultó esta sección
                // mientras esperábamos), los botones nuevos también tienen que estarlo.
                if (campoMaterial.disabled) {
                    zonaReferencias.querySelectorAll("input").forEach((campo) => {
                        campo.disabled = true;
                    });
                }

                actualizarResumen();

                // Llevamos el foco al nombre del material para que el lector de pantalla anuncie
                // el cambio de paso.
                const nombreMaterial = zonaReferencias.querySelector("[data-nombre-material-elegido]");
                if (nombreMaterial && contenedor.offsetParent !== null) {
                    nombreMaterial.focus();
                }
            } catch {
                if (numeroPedido !== ultimoPedidoReferencias) {
                    return;
                }

                mostrarError(zonaReferencias,
                    "No se pudieron cargar las referencias del material. Probá elegirlo de nuevo.");
            }
        }

        function volverAlListado() {
            campoMaterial.value = "";

            // Sacamos los botones de opción: si no, se enviaría la referencia de otro material.
            zonaReferencias.innerHTML = "";
            ocultarResumen();
            avisarReferenciaElegida(null);

            pasoReferencias.classList.add("d-none");
            pasoListado.classList.remove("d-none");
        }

        function activar() {
            if (campoMaterial.value) {
                elegirMaterial(campoMaterial.value);
                return;
            }

            if (!listadoCargado) {
                cargarListado(1);
            }
        }

        function reiniciar() {
            referenciaParaRestaurar = "";
            buscador.value = "";
            buscador.defaultValue = "";
            zonaListado.innerHTML = "";
            listadoCargado = false;
            volverAlListado();
        }

        // --- Eventos ---

        // Enter en el buscador busca, en lugar de enviar el formulario que lo contiene.
        buscador.addEventListener("keydown", (evento) => {
            if (evento.key === "Enter") {
                evento.preventDefault();
                cargarListado(1);
            }
        });

        botonBuscar.addEventListener("click", () => {
            cargarListado(1);
        });

        // Los botones del listado llegan después (desde el servidor), así que escuchamos en la
        // zona que los contiene en vez de en cada botón.
        zonaListado.addEventListener("click", (evento) => {
            const botonPagina = evento.target.closest("[data-pagina]");
            if (botonPagina && !botonPagina.disabled) {
                cargarListado(Number(botonPagina.dataset.pagina));
                return;
            }

            const botonElegir = evento.target.closest("[data-elegir-material]");
            if (botonElegir) {
                elegirMaterial(botonElegir.dataset.elegirMaterial);
            }
        });

        zonaReferencias.addEventListener("change", () => {
            actualizarResumen();
        });

        botonElegirOtro.addEventListener("click", () => {
            volverAlListado();
            if (!listadoCargado) {
                cargarListado(1);
            }
            buscador.focus();
        });

        contenedor.addEventListener("selector:activar", () => {
            activar();
        });

        contenedor.addEventListener("selector:reiniciar", () => {
            reiniciar();
        });

        if (contenedor.dataset.cargaDiferida !== "true") {
            activar();
        }
    }

    document.querySelectorAll("[data-selector-material]").forEach((contenedor) => {
        prepararSelector(contenedor);
    });
})();
