# S3-T26 — Revisión del flujo de pantallas de presupuestos

## 1. Objetivo

Definir el recorrido de navegación entre la elaboración de un presupuesto, el guardado de avances y la consulta y confirmación del resumen.

Esta tarea corresponde al Sprint 3 y es de análisis. No implica implementar nuevas pantallas ni modificar las reglas de negocio aprobadas.

## 2. Historias de usuario relacionadas

- **US-002:** Crear un presupuesto.
- **US-003:** Guardar avances del presupuesto.
- **US-013:** Consultar y confirmar el resumen.

También se consideran las reglas de cálculo de US-012 y los permisos definidos en RF-73.

## 3. Flujo de navegación

### 3.1. Creación y elaboración

El usuario autorizado crea un presupuesto indicando los datos obligatorios.

El sistema registra el presupuesto con un código único, asigna automáticamente como Responsable al usuario que lo creó y establece el estado inicial Borrador.

Posteriormente, el usuario puede continuar su elaboración incorporando o modificando conceptos económicos.

### 3.2. Guardar borrador

Durante la elaboración, el usuario puede seleccionar **Guardar borrador**.

El sistema debe:

- Guardar los cambios realizados.
- Conservar los conceptos, cantidades, materiales, proveedores, precios y monedas históricos.
- Mantener el estado actual del presupuesto.
- No modificar materiales ni precios del catálogo.

El guardado es explícito. No existe guardado automático como sustituto de esta acción.

### 3.3. Cambios sin guardar

Cuando el usuario intenta abandonar la pantalla con cambios pendientes, debe aparecer una advertencia con tres opciones:

**Guardar:** guarda los cambios antes de abandonar la pantalla.

**Descartar cambios:** descarta las modificaciones posteriores al último guardado y permite abandonar.

**Continuar editando:** permanece en el presupuesto y mantiene los cambios realizados para continuar trabajando.

Si no existen cambios pendientes, no es necesaria esta advertencia.

### 3.4. Pantalla de resumen

El usuario puede consultar un resumen técnico y económico del presupuesto.

Debe mostrar:

- Conceptos y cantidades.
- Materiales y proveedores seleccionados.
- Precios e importes.
- Monedas originales.
- Margen calculado.
- Totales en UYU y USD.

La consulta del resumen no modifica los datos ni el estado del presupuesto.

### 3.5. Volver a edición

Desde el resumen, el usuario puede volver a modificar el presupuesto cuando su estado y permisos lo permiten.

Regresar a edición no cambia el estado por sí mismo.

### 3.6. Confirmar datos y continuar

El usuario puede seleccionar **Confirmar datos y continuar**.

El sistema debe:

- Validar los datos necesarios para confirmar.
- Comprobar que exista una cotización válida.
- Guardar los valores económicos confirmados.
- Conservar los totales en UYU y USD, la cotización aplicada y su fecha de publicación.
- Continuar hacia la sección Documentos.
- No modificar precios ni relaciones del catálogo.
- No cambiar el estado a Para revisión interna por el solo hecho de confirmar los datos.

Si no existe una cotización válida, se informa el problema y no se confirma la información económica. El presupuesto puede continuar guardándose como borrador.

## 4. Restricciones de edición

Los estados que permiten edición son:

- Borrador.
- Para revisión interna.
- Solicitud de modificación del cliente.

Los presupuestos en otros estados pueden consultarse, pero no modificarse mediante el proceso normal de elaboración.

Respecto a los permisos:

- **Operador:** puede modificar presupuestos de los que sea Responsable.
- **Supervisor:** puede modificar presupuestos dentro de las condiciones funcionales autorizadas.
- **Administrador:** posee acceso completo, respetando las restricciones funcionales del estado.

Los controles de autorización deben verificarse en el servidor.

## 5. Situación actual y dependencias

En la revisión inicial de `PresupuestosController` se identificaron acciones para:

- Listar presupuestos.
- Crear presupuestos.
- Consultar el detalle de un presupuesto.

Las pantallas y operaciones de guardado de avances, edición económica y resumen corresponden a funcionalidades previstas en las demás tareas del Sprint 3.

La revisión del flujo no implica que esas funcionalidades ya se encuentren implementadas.

## 6. Aspectos pendientes de implementación y verificación

- Implementación del guardado de borradores.
- Advertencia de cambios pendientes.
- Navegación hacia el resumen económico.
- Retorno desde el resumen a la edición.
- Confirmación de los datos económicos.
- Navegación hacia Documentos.
- Pruebas de permisos y estados editables.

## 7. Conclusión

El recorrido funcional esperado queda definido a partir de las historias de usuario aprobadas.

El flujo permite crear y elaborar presupuestos, guardar avances explícitamente, advertir sobre modificaciones no guardadas y revisar y confirmar los valores económicos antes de continuar a Documentos.

La implementación y las pruebas de estas operaciones quedan asignadas a las tareas correspondientes del Sprint 3.

**Referencias:** `AGENTS.md`, `docs/user-stories.md`, `docs/requerimientos-funcionales.md` y `docs/diseno-visual.md`.