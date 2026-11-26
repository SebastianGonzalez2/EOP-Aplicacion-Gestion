# Guía: Autenticación con Microsoft Entra ID en ASP.NET Core MVC

Manual de capacitación paso a paso, basado en lo implementado en EOP (S1-T08 y S1-T09).

- **PDF para leer:** `salida/Guia-Autenticacion-Entra-ID.pdf`
- **Fuente editable:** los archivos Markdown de `capitulos/`

## Estructura

```text
docs/guias/autenticacion-entra-id/
├── guia.json              ← título, versión, tareas documentadas y ORDEN de los capítulos
├── capitulos/             ← un archivo Markdown por capítulo (fuente editable)
├── imagenes/
│   ├── app/               ← capturas reales de EOP (se regeneran con `npm run capturas`)
│   └── portal/            ← capturas del portal de Azure (se agregan a mano)
├── herramientas/
│   ├── construir.mjs      ← Markdown → HTML → PDF
│   ├── capturar.mjs       ← capturas automáticas de la app en local
│   ├── vista-previa.mjs   ← convierte páginas del PDF en PNG para revisarlo
│   └── estilos.css        ← diseño del PDF
└── salida/                ← PDF y HTML generados
```

## Cómo regenerar el PDF

Requisitos: Node.js 20 o superior y Google Chrome o Microsoft Edge instalados.

```bash
cd docs/guias/autenticacion-entra-id/herramientas
npm install          # solo la primera vez
npm run construir
```

Si Chrome o Edge están en otra ruta, definí la variable `CHROME_PATH`.

Para regenerar las capturas de la app, levantá EOP con el perfil `https` y ejecutá `npm run capturas`. El script nunca escribe credenciales: solo captura pantallas que se ven sin sesión.

## Recuadros disponibles en el Markdown

Se escriben como citas que empiezan con una etiqueta. El texto opcional después de la etiqueta reemplaza el título:

```markdown
> [!EOP]
> Lo que está hecho en el repositorio.

> [!ADVERTENCIA] Título propio
> Texto del aviso.
```

Etiquetas: `EOP`, `GENERAL`, `ADVERTENCIA`, `CONSEJO`, `NOTA`, `RECONSTRUIDO`, `PENDIENTE`, `PRUEBA`.

Los diagramas se escriben en bloques de código ` ```mermaid `, que también se ven en GitHub y en VS Code.

## Cómo agregar capturas del portal

Los capítulos ya referencian estas imágenes. Mientras no existan, el PDF muestra un recuadro "📷 Captura pendiente":

| Archivo | Qué capturar |
|---|---|
| `imagenes/portal/01-app-registrations.png` | Entra ID › App registrations, con el botón New registration |
| `imagenes/portal/02-new-registration.png` | El formulario Register an application |
| `imagenes/portal/03-overview.png` | Overview del registro (Client ID y Tenant ID) |
| `imagenes/portal/04-authentication-web.png` | Authentication con la plataforma Web y las dos Redirect URIs |
| `imagenes/portal/05-client-secret.png` | Certificates & secrets con un secreto |
| `imagenes/portal/06-security-defaults.png` | Overview › Properties › Manage security defaults |

**Antes de guardar una captura, difuminá** el valor del client secret y, si la guía se va a compartir fuera del equipo, también los identificadores del tenant y de la aplicación.

## Cómo actualizar la guía cuando se completa una tarea nueva

1. **Analizar los cambios reales**: PR de la tarea, `git diff` contra `main`, archivos tocados y pruebas.
2. **Agregar un capítulo** en `capitulos/` con el número siguiente (por ejemplo `12-vinculacion-de-identidades.md`) y sumarlo a `guia.json` **antes** de `90-glosario.md`.
3. Mantener las **dos miradas**: recuadros `[!EOP]` para lo implementado y `[!GENERAL]` para el procedimiento en un proyecto nuevo.
4. Revisar los recuadros `[!PENDIENTE]` de los capítulos anteriores que la tarea resolvió: cambiarlos por la explicación real y una referencia al capítulo nuevo. **No borrar** explicaciones anteriores salvo que estuvieran mal.
5. Actualizar los **diagramas** afectados (por ejemplo, el circuito completo del capítulo 1).
6. Actualizar el **checklist** (capítulo 11), el **glosario** y las **referencias**.
7. Actualizar `guia.json` (`version`, `fecha`, `tareasDocumentadas`) y el **historial** (`99-historial-de-la-guia.md`).
8. Regenerar el PDF y revisarlo con `node vista-previa.mjs`.
