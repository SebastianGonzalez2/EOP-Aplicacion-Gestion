# Cómo usar esta guía

Esta guía es un **curso práctico**. Está pensada para que, dentro de unos meses, puedas agregar inicio de sesión con Microsoft Entra ID a **cualquier** aplicación ASP.NET Core MVC nueva siguiendo solamente este documento, y entendiendo por qué se hace cada cosa.

Para eso usa como ejemplo real el proyecto **EOP**, donde ya implementamos:

| Tarea | Qué resolvió | Pull request |
|---|---|---|
| **S1-T08** | Conectar la aplicación con Microsoft Entra ID (configuración, paquete NuGet, `Program.cs`) | #10 |
| **S1-T09** | Iniciar sesión, cerrar sesión, proteger todas las pantallas y manejar los fallos del login | #13 |

## Dos miradas en cada capítulo

Cada tema se explica desde dos perspectivas. Para que no se mezclen, están marcadas con recuadros de color:

> [!EOP]
> Lo que **efectivamente está hecho** en el repositorio de EOP: archivos, clases, métodos y valores reales (sin secretos). Todo lo que aparece en estos recuadros fue verificado contra el código que está en `main`.

> [!GENERAL]
> Cómo hacerlo en **un proyecto nuevo**, con otros nombres y otra estructura de carpetas. Es el procedimiento que tenés que aprender, más allá de cómo quedó organizado EOP.

Además vas a encontrar estos otros recuadros:

> [!ADVERTENCIA]
> Errores habituales o decisiones de seguridad que no conviene saltear.

> [!CONSEJO]
> Atajos y recomendaciones prácticas.

> [!PRUEBA]
> Cómo comprobar, en ese momento, que lo que hiciste funciona antes de seguir.

> [!RECONSTRUIDO]
> Pasos que **no quedaron registrados** en el repositorio (por ejemplo, clics en el portal de Azure durante la T08). Se reconstruyeron a partir de la configuración que sí existe y de la documentación oficial de Microsoft. Son correctos y reproducibles, pero no son un registro histórico exacto.

> [!PENDIENTE]
> Funcionalidades que corresponden a tareas posteriores (T10, T11, T13…). Se mencionan para que entiendas dónde encajan, pero **no están implementadas**.

## Valores de ejemplo y valores reales

En la guía nunca aparecen secretos. Cuando un valor depende de tu cuenta, se escribe en mayúsculas entre signos `<` y `>`:

| En la guía | Qué tenés que poner vos |
|---|---|
| `<TENANT-ID>` | El **Directory (tenant) ID** de tu organización en Entra ID |
| `<CLIENT-ID>` | El **Application (client) ID** de tu registro de aplicación |
| `<CLIENT-SECRET>` | El **Value** del secreto que generaste (nunca va en el repositorio) |
| `<PUERTO>` | El puerto HTTPS de tu proyecto (en EOP es `7125`) |

Los identificadores con forma de GUID que veas en ejemplos (`00000000-0000-0000-0000-000000000000`) son **ficticios**.

## Mapa de la guía

| Capítulo | Para qué te sirve |
|---|---|
| 1. Introducción y conceptos | Entender las piezas antes de tocar nada |
| 2. Requisitos previos | Preparar cuenta, herramientas y HTTPS |
| 3. Configuración de Entra ID | Registrar la aplicación en el portal, paso a paso |
| 4. Integración con ASP.NET Core | Conectar el proyecto con Entra ID (T08) |
| 5. Inicio de sesión | Construir el circuito de login (T09) |
| 6. Protección y autorización | Cerrar todas las pantallas por defecto (T09) |
| 7. Cierre de sesión | Salir de la app y de Microsoft de forma segura (T09) |
| 8. Errores y seguridad | Qué pasa cuando el login falla y qué protege cada configuración |
| 9. Pruebas y verificación | Pruebas manuales y automáticas |
| 10. Errores frecuentes | Diagnóstico de problemas con mensajes reales |
| 11. Checklist de reproducción | La receta corta para seguir mientras programás |

> [!CONSEJO]
> Si es tu primera vez, leé los capítulos 1 a 8 en orden. Cuando ya entiendas los conceptos, el capítulo 11 alcanza como referencia rápida.
