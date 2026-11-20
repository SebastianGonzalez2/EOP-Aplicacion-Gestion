# S3-T04 — Evaluación preliminar de integración con inteligencia artificial

**Proyecto:** EOP — Sistema de Gestión  
**Fecha de las pruebas:** 20/11/2026  
**Rama de trabajo:** `feat/S3-T04-evaluar-integracion-ia`  
**Estado de la evaluación:** pruebas preliminares realizadas; pendiente incorporar los archivos a Git y revisar el Pull Request.

## 1. ¿Qué queríamos comprobar?

La tarea S3-T04 consiste en hacer una **prueba técnica preliminar** para saber si podemos utilizar la API de OpenAI para analizar los relevamientos de obra de EOP.

Un *relevamiento* es el conjunto de observaciones que se toman antes de preparar un presupuesto: qué materiales hacen falta, qué trabajos deben realizarse, si se necesitan equipos alquilados o traslados, etc.

El objetivo de esta tarea **no era incorporar la IA a las pantallas de EOP**. Eso se desarrollará en una etapa posterior. Queríamos comprobar si la tecnología funciona y conocer sus posibles problemas antes de integrarla.

## 2. ¿Cómo hicimos la prueba?

Creamos una aplicación pequeña de consola, separada de la aplicación web, pero dentro del mismo repositorio:

- **Ubicación:** `pruebas-concepto/ia-relevamientos/`.
- **Lenguaje y versión:** C# con .NET 10.
- **Biblioteca utilizada:** paquete `OpenAI`, versión `2.15.0`.
- **Modelo configurado en la prueba:** `gpt-6-luna`.
- **Acceso:** una clave privada proporcionada mediante la variable de entorno `OPENAI_API_KEY`. La clave **no se escribió en el código**.

El programa envía a OpenAI un relevamiento ficticio y recibe una propuesta con estos datos: tipo de concepto, nombre detectado, cantidad y unidad. Se utilizó un formato de respuesta **JSON estructurado**: una manera de recibir datos ordenados que C# puede leer sin tener que interpretar una tabla de texto.

También agregamos una validación básica en C#: comprobamos que la respuesta contenga una lista, que los tipos pertenezcan al conjunto permitido, que los conceptos tengan nombre y que cantidad y unidad tengan el formato esperado.

**Importante:** esta prueba **no asigna precios, no calcula subtotales, no accede a la base de datos y no selecciona materiales del catálogo**. Esas responsabilidades no se delegan automáticamente a la IA.

## 3. Pruebas realizadas y resultados

Los resultados siguientes se copiaron de las salidas de terminal obtenidas durante la evaluación. Son pruebas exploratorias ejecutadas manualmente, no una medición estadística de precisión.

### Prueba 1 — Relevamiento sencillo

**Texto utilizado:**

> Se necesita instalar 20 metros de cable eléctrico, realizar trabajos de mano de obra y alquilar un elevador para la instalación.

**Resultado observado:**

| Tipo | Concepto identificado | Cantidad | Unidad |
|---|---|---:|---|
| Material | Cable eléctrico | 20 | metros |
| Mano de obra | Trabajos de mano de obra | No especificada | No especificada |
| Alquiler | Elevador | No especificada | No especificada |

**Evaluación:** identificó los **3 conceptos esperados**. No inventó cantidades que no aparecían en el texto.

### Prueba 2 — Más conceptos y datos incompletos

**Texto utilizado:**

> En el galpón se deben instalar 8 luminarias LED y tender cable eléctrico, pero todavía no se midieron los metros de cable. Se necesita mano de obra para la instalación, alquilar una plataforma elevadora durante 2 días y trasladar materiales desde el depósito. No se informaron precios.

**Resultado observado:**

| Tipo | Concepto identificado | Cantidad | Unidad |
|---|---|---:|---|
| Material | Luminarias LED | 8 | luminarias |
| Material | Cable eléctrico | No especificada | metros |
| Mano de obra | Mano de obra para la instalación | No especificada | No especificada |
| Alquiler | Plataforma elevadora | 2 | días |
| Traslado | Traslado de materiales desde el depósito | No especificada | No especificada |

**Evaluación:** identificó los **5 conceptos esperados**. Reconoció los dos días de alquiler y no inventó los metros de cable ni precios.

**Observación:** la IA puede devolver nombres de unidad distintos, por ejemplo «luminarias», «luminaria» o «unidades». La aplicación definitiva necesitará revisar o normalizar esos valores.

### Prueba 3 — Información dudosa o no confirmada

**Texto utilizado:**

> En el edificio hay que reemplazar algunas luminarias LED, pero todavía no se determinó cuántas. También se debe revisar el tablero eléctrico existente para evaluar su estado. Es posible que haya que reemplazarlo, pero eso todavía no está confirmado. La revisión requiere 4 horas de mano de obra. No se necesita alquilar equipos ni trasladar materiales. Todavía no se definieron precios.

**Primer resultado:** la IA propuso tres filas:

1. Material: luminarias LED para reemplazo.
2. **Material: tablero eléctrico existente (reemplazo posible, aún no confirmado).**
3. Mano de obra: revisión del tablero eléctrico, 4 horas.

**Problema encontrado:** incluyó el tablero como material presupuestable aunque su reemplazo todavía era una posibilidad. Si se aceptara automáticamente esa propuesta, podría agregarse un costo que el relevamiento no confirmó.

**Corrección ensayada:** agregamos instrucciones explícitas para incluir solamente necesidades confirmadas; distinguir revisión de compra; excluir trabajos expresamente descartados; y no inventar datos.

**Resultado al repetir el mismo texto:**

| Tipo | Concepto identificado | Cantidad | Unidad |
|---|---|---:|---|
| Material | Reemplazo de luminarias LED | No especificada | luminaria |
| Mano de obra | Revisión del tablero eléctrico existente | 4 | horas |

**Evaluación:** en la repetición identificó únicamente los **2 conceptos esperados**. Ya no incluyó la compra del tablero ni alquileres o traslados innecesarios.

**Conclusión de esta prueba:** las instrucciones ayudan a reducir errores, pero una ejecución correcta **no garantiza** que la IA nunca vuelva a equivocarse. El usuario siempre debe poder revisar lo propuesto.

### Prueba 4 — Datos estructurados y lectura desde C#

Pedimos a OpenAI que devolviera los conceptos como **JSON**, con un conjunto fijo de tipos (`Material`, `ManoDeObra`, `Alquiler`, `Traslado`, `CostosIndirectos`, `Margen`) y con los campos `conceptoDetectado`, `cantidad` y `unidad`.

**Evidencia resumida de la respuesta:**

`{"conceptos":[{"tipo":"Material","conceptoDetectado":"Cable eléctrico","cantidad":20,"unidad":"metros"},{"tipo":"ManoDeObra","conceptoDetectado":"Trabajos de mano de obra","cantidad":null,"unidad":null},{"tipo":"Alquiler","conceptoDetectado":"Alquiler de un elevador","cantidad":null,"unidad":null}]}`

El programa pudo leer esa respuesta, recorrer los conceptos, comprobar los tipos y mostrar:

> Total de conceptos: 3

**Evaluación:** el formato estructurado funcionó y la validación básica no detectó errores para ese caso. **Esto valida el formato, no la exactitud de todas las decisiones de la IA.**

### Prueba 5 — Error de autenticación

Sustituimos **temporalmente** la clave válida por una clave ficticia para simular un fallo. Después la restauramos desde PowerShell.

**Resultado observado:**

> La API rechazó la solicitud. Código HTTP: 401  
> Revisá si la API key es válida.  
> Clave original restaurada.

**Evaluación:** el programa detectó y manejó el error sin una excepción sin controlar. Posteriormente quitamos del código la impresión del detalle completo devuelto por OpenAI para evitar exponer información sensible.

**Límite de esta prueba:** no comprobamos todavía qué ocurre ante cortes de red, tiempos de espera ni qué muestra la aplicación web, porque todavía no existe esa integración.

### Prueba 6 — Compilación

Se ejecutó:

`dotnet build pruebas-concepto/ia-relevamientos/EOP.PruebaConcepto.IA.csproj`

La terminal confirmó una compilación correcta al instalar el SDK. Después del último cambio de manejo de errores, el desarrollador volvió a ejecutar la compilación y confirmó que terminó correctamente.

**No consta en esta evaluación la ejecución de las pruebas automatizadas de toda la solución EOP**, ni una revisión de Pull Request.

## 4. Qué aprendimos

- **Es técnicamente posible** conectar un programa C# de EOP con OpenAI para analizar relevamientos escritos.
- La IA puede detectar materiales, mano de obra, alquileres y traslados, incluso cuando faltan cantidades.
- El formato JSON estructurado facilita que C# interprete la respuesta y compruebe su forma.
- La IA puede equivocarse cuando una necesidad es solamente posible o está pendiente de confirmación. Las instrucciones mejoran el resultado observado, pero no reemplazan la revisión humana.
- Los valores faltantes deben permanecer sin completar; no deben inventarse precios ni cantidades.
- Las claves de acceso deben mantenerse fuera del repositorio y los errores no deben mostrar datos sensibles.

## 5. Relación con los requerimientos de EOP

La evaluación aporta evidencia preliminar para **RF-011** y **US-006**, que prevén analizar el relevamiento y obtener conceptos propuestos para un presupuesto. También respeta el criterio de **RF-012 / US-007** de no vincular automáticamente los materiales detectados con el catálogo.

**No se implementaron ni verificaron todavía** la tabla editable de la aplicación, las autorizaciones de usuarios, el bloqueo por audio pendiente, la selección manual de catálogo, los precios, las monedas ni los cálculos del presupuesto. Esas funciones corresponden a la implementación posterior y deberán cumplir sus propios criterios de aceptación.

En cuanto a fallas externas, comprobamos un caso de clave incorrecta, **pero no podemos afirmar todavía que la aplicación web cumpla por completo el requisito de continuar funcionando cuando OpenAI no esté disponible**.

## 6. Aspectos pendientes antes de la integración definitiva

1. Repetir las pruebas con más relevamientos distintos, incluyendo casos reales autorizados y tratados con las precauciones de privacidad que correspondan.
2. Definir y probar reglas claras para revisar conceptos dudosos y unidades de medida.
3. Incorporar las comprobaciones necesarias en la aplicación, manteniendo la revisión y confirmación del usuario.
4. Probar otros fallos de la API, los tiempos de respuesta, el costo por uso y el registro seguro de errores.
5. Integrar la funcionalidad en las capas existentes de EOP cuando corresponda, sin llamar a OpenAI directamente desde los controladores ni alterar los cálculos económicos del sistema.

**Estos puntos no significan que la prueba preliminar S3-T04 deba convertirse en la implementación completa de US-006.** Son consideraciones para el trabajo posterior.

## 7. Conclusión

**Resultado de la evaluación preliminar: favorable, con limitaciones identificadas.**

Las pruebas permiten considerar **viable** el análisis inicial de relevamientos mediante OpenAI desde C# y .NET 10. Se identificaron correctamente los conceptos esperados en los ejemplos sencillos y complejos. En el caso ambiguo apareció un error, se corrigieron las instrucciones y se obtuvo un resultado adecuado al repetir la prueba.

La IA debe utilizarse para **proponer conceptos**, nunca para aprobar presupuestos, seleccionar automáticamente materiales del catálogo o definir precios sin intervención humana.

**Cierre administrativo de S3-T04:** corresponde guardar este documento y el proyecto de consola en la rama S3-T04, comprobar los cambios y abrir un Pull Request para revisión. La tarea puede pasar a «Done» una vez completado el proceso de revisión y aceptación del equipo, conforme a las reglas del repositorio.

## 8. Cómo reproducir la prueba

Desde la raíz del repositorio, con una API key válida configurada en la variable de entorno `OPENAI_API_KEY`:

- **Compilar:** `dotnet build pruebas-concepto/ia-relevamientos/EOP.PruebaConcepto.IA.csproj`
- **Ejecutar:** `dotnet run --project pruebas-concepto/ia-relevamientos/EOP.PruebaConcepto.IA.csproj`

Las pruebas con distintos relevamientos se hicieron cambiando el texto ficticio enviado desde `Program.cs`. Los resultados presentados arriba proceden de esas ejecuciones manuales; **no se implementó aún una batería automatizada que repita todos estos casos**.

**Seguridad:** nunca subir la clave privada a GitHub ni incorporarla a capturas o registros de prueba.
