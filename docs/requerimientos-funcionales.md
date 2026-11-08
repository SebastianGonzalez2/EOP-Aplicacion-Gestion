# Requerimientos funcionales

> Generado desde el anteproyecto corregido. **El documento Word es el original**;
> este archivo es la copia de trabajo del repositorio. Si corregís el Word,
> hay que regenerar este archivo.


109 requerimientos funcionales.

---

## RF-001 — Crear Presupuesto


**Descripción**


Por sus permisos base, Operador, Supervisor y Administrador podrán crear Presupuestos; el usuario que los cree quedará automáticamente como Responsable, sin selección manual. Los permisos serán exclusivamente los definidos para cada rol en RF-73, sin excepciones individuales. El sistema deberá permitir a los usuarios autorizados iniciar un nuevo presupuesto, registrando la información básica necesaria para identificar el trabajo a presupuestar.

Al momento de crear el presupuesto, el sistema deberá asignar automáticamente como Responsable al usuario autenticado que realiza su creación. El Responsable no deberá ser seleccionado manualmente por el usuario durante el alta.


**Reglas de negocio**


Todo presupuesto deberá indicar y estar asociado a un cliente.

Todo presupuesto deberá indicar un Tipo de proyecto seleccionado entre los tipos de proyecto fijos definidos por la aplicación.

Todo presupuesto deberá contar con un nombre del proyecto al cual se va a asociar.

Cuando el cliente para el cual se desea crear el presupuesto no se encuentre registrado, el usuario deberá poder iniciar el registro de un nuevo cliente de acuerdo con RF-89. Una vez registrado, deberá quedar disponible para ser seleccionado en el presupuesto actual.

Todo presupuesto deberá contar con un plazo de vigencia expresado en días corridos.

El sistema deberá asignar automáticamente un código interno único al presupuesto al momento de su creación, utilizando la estructura PRES-AAAAMMDD-NOMBRE DEL PROYECTO-####. El usuario no deberá ingresar manualmente este código.

AAAAMMDD corresponderá a la fecha de creación del presupuesto; NOMBRE DEL PROYECTO, al nombre registrado en el presupuesto; y ####, al correlativo global de presupuestos expresado mediante cuatro cifras. El correlativo no se reiniciará por fecha, cliente, Tipo de proyecto ni nombre del proyecto y será independiente del correlativo de proyectos. El código permanecerá asociado al mismo presupuesto durante todo su ciclo de vida; modificar sus datos posteriormente no generará uno nuevo.

Todo presupuesto deberá contar con un Responsable.

Al crear un presupuesto, el sistema deberá asignar automáticamente como Responsable al usuario autenticado que realiza su creación.

El usuario no deberá seleccionar manualmente al Responsable durante el alta del presupuesto.

El Responsable asignado deberá permanecer asociado al presupuesto para permitir identificar posteriormente quién estuvo a cargo de su elaboración y seguimiento.

Los tipos de proyecto se ofrecerán como opciones fijas; su creación, administración y eliminación no forman parte de esta versión. PENDIENTE DE VALIDAR CON EOP: lista definitiva de tipos de proyecto.

Proceso de negocio al que responde: Elaboración de presupuestos.

Dependencias: RF-89; gestión de clientes; selección de tipo de proyecto fijo.

Necesidad asociada: N-01 / N-07

Prioridad: Alta.

## RF-002 — Gestionar estado del presupuesto


**Descripción**


El sistema deberá mantener identificado el estado de cada presupuesto de acuerdo con la etapa en la que se encuentre dentro del proceso de elaboración, revisión, envío y respuesta del cliente.

Los estados definidos para los presupuestos serán: Borrador, Para revisión interna, Enviado / pendiente de respuesta del cliente, Aprobado, Rechazado, Solicitud de modificación del cliente, Vencido y Cancelado.


**Reglas de negocio**


Todo presupuesto recién creado deberá encontrarse inicialmente en estado Borrador.

Todo presupuesto deberá poseer un único estado actual identificable.

Los presupuestos en estado Borrador, Para revisión interna y Solicitud de modificación del cliente deberán poder ser editados.

Los presupuestos en estado Enviado / pendiente de respuesta del cliente, Aprobado, Rechazado, Vencido y Cancelado no deberán poder modificarse mediante el proceso normal de edición.

Los cambios de estado deberán representar la situación actual del presupuesto dentro del proceso.

Un presupuesto en estado Vencido podrá salir de dicho estado mediante el proceso de reapertura definido en RF-36.

Al completarse correctamente la reapertura de un presupuesto Vencido, su estado deberá cambiar a Enviado / pendiente de respuesta del cliente.

Las acciones Aprobar, Rechazar y Solicitud de modificación no deberán ejecutarse directamente mientras el presupuesto se encuentre en estado Vencido. Para registrar una respuesta del cliente deberá realizarse previamente la reapertura definida en RF-36.

Proceso de negocio al que responde: Elaboración y seguimiento de presupuestos.

Dependencias: RF-01.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-003 — Consultar presupuestos registrados


**Descripción**


Por sus permisos base, Operador, Supervisor y Administrador podrán ejecutar esta acción; no se admitirán excepciones individuales; se aplicarán los permisos fijos de RF-73. El sistema deberá permitir al usuario consultar los presupuestos registrados y acceder a la información correspondiente a cada uno.

La consulta deberá permitir identificar información básica de cada presupuesto, incluyendo su código, nombre del proyecto, cliente, fecha, estado actual y Responsable.


**Reglas de negocio**


Los presupuestos registrados deberán permanecer disponibles para su posterior consulta.

Desde el listado de presupuestos deberá ser posible seleccionar un presupuesto para consultar su información o, cuando su estado lo permita, continuar su elaboración o modificación de acuerdo con RF-05.

Los presupuestos ya elaborados podrán consultarse como antecedentes o referencia, pero el sistema no deberá permitir duplicarlos automáticamente ni utilizarlos como base para crear un nuevo presupuesto.

El listado general de presupuestos deberá incluir una columna Responsable.

Para cada presupuesto, la columna Responsable deberá mostrar el usuario que se encuentra registrado como Responsable de dicho presupuesto. Cuando un Presupuesto esté Rechazado y tenga un Motivo de rechazo registrado, este deberá poder consultarse al acceder a su información; no se exige columna ni filtro adicional.

Proceso de negocio al que responde: Gestión y seguimiento de presupuestos.

Dependencias: RF-01; RF-02.

Necesidad asociada: N-01 / N-03.

Prioridad: Alta.

## RF-004 — Filtrar presupuestos


**Descripción**


Por sus permisos base, Operador, Supervisor y Administrador podrán ejecutar esta acción; no se admitirán excepciones individuales; se aplicarán los permisos fijos de RF-73. El sistema deberá permitir al usuario aplicar filtros sobre los presupuestos registrados para facilitar su localización y seguimiento.

Los presupuestos deberán poder filtrarse por código, estado, cliente, fecha y Responsable.


**Reglas de negocio**


La aplicación de filtros no deberá modificar la información de los presupuestos registrados.

El usuario deberá poder combinar criterios de filtrado cuando resulte necesario.

El usuario deberá poder seleccionar un Responsable como criterio de filtrado.

Al aplicar el filtro por Responsable, el listado deberá mostrar únicamente los presupuestos asociados al usuario seleccionado como Responsable.

El filtro por Responsable deberá poder combinarse con los demás criterios de búsqueda y filtrado disponibles.

Los criterios definitivos de filtrado son exclusivamente Código, Estado, Cliente, Fecha y Responsable; no se requieren criterios adicionales.

Proceso de negocio al que responde: Gestión y seguimiento de presupuestos.

Dependencias: RF-03.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-005 — Continuar la elaboración o modificación de un presupuesto editable


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario acceder nuevamente a un presupuesto que se encuentre en un estado que permita edición y continuar trabajando sobre la información previamente registrada.

Al volver a acceder al Presupuesto deberán conservarse los avances efectivamente guardados: relevamiento, filas y conceptos, materiales y Proveedores seleccionados, precios y monedas históricos copiados desde el catálogo, cantidades, mano de obra y demás información registrada. Guardar el Presupuesto no creará ni modificará relaciones o precios del catálogo.


**Reglas de negocio**


Los presupuestos en estado Borrador, Para revisión interna y Solicitud de modificación del cliente deberán poder continuar siendo modificados.

La información registrada deberá conservarse unicamente si el usuario aprieta Guardar, aunque el usuario abandone la pantalla de elaboración y acceda nuevamente en otro momento.

La continuación de la elaboración deberá realizarse sobre el mismo presupuesto y no deberá generar automáticamente un nuevo presupuesto.

Los presupuestos cuyo estado no permita edición deberán poder consultarse, pero no modificarse mediante el proceso normal de elaboración.

Si el usuario intenta cerrar la aplicación o acceder a otra sección sin haber guardado los cambios, se mostrará una ventana advirtiendo de esto con las opciones: Guardar, Descartar Cambios y Continuar editando.

Proceso de negocio al que responde: Elaboración y seguimiento de presupuestos.

Dependencias: RF-02; RF-03.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-006 — Registrar observaciones de relevamiento mediante texto


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario registrar y editar observaciones escritas correspondientes al relevamiento de un presupuesto.

Las observaciones deberán mantenerse reunidas en un mismo contenido textual editable asociado al presupuesto.


**Reglas de negocio**


No se definen reglas adicionales a las indicadas en la descripción.

Proceso de negocio al que responde: Relevamiento previo a la elaboración del presupuesto.

Dependencias: RF-01.

Necesidad asociada: N-01

Prioridad: Alta.

## RF-007 — Registrar observaciones de relevamiento mediante audio.


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario incorporar observaciones de audio al relevamiento de un presupuesto mediante una grabación realizada directamente desde la aplicación o mediante la carga de un archivo de audio previamente grabado con un dispositivo móvil.

Cada audio incorporado deberá poder utilizarse para agregar información al relevamiento que ya se encuentre registrado.


**Reglas de negocio**


La carga de un archivo de audio deberá permitir incorporar grabaciones realizadas previamente fuera del sistema, por ejemplo, cuando el usuario haya realizado el relevamiento en un lugar sin conexión a Internet.

Los archivos cargados deberán ser compatibles con los formatos de audio admitidos por el sistema: MP3, M4A, WAV y WEBM.

El archivo de audio utilizado para la transcripción tendrá carácter temporal y no deberá conservarse como documentación permanente del presupuesto una vez completada correctamente su transcripción.

Cuando el usuario grabe o cargue un audio válido, el sistema deberá mostrar claramente que existe un audio pendiente de transcripción.

El usuario deberá poder quitar un audio pendiente antes de iniciar su transcripción. Al quitarlo, el audio no deberá incorporarse al relevamiento.

El sistema permitirá mantener como máximo un audio pendiente de transcripción o en procesamiento a la vez.

Mientras exista un audio pendiente de transcripción o en procesamiento, no deberá permitirse grabar ni cargar un nuevo audio.

Proceso de negocio al que responde: Relevamiento previo a la elaboración del presupuesto.

Dependencias: RF-01.

Necesidad asociada: N-01

Prioridad: Alta.

## RF-008 — Transcribir observaciones de audio


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario solicitar la transcripción mediante inteligencia artificial del audio pendiente incorporado al relevamiento, ya sea mediante una grabación realizada directamente desde la aplicación o mediante la carga de un archivo previamente grabado.

Al seleccionar la acción Transcribir, el sistema deberá procesar el audio y convertir su contenido a texto.

Durante el procesamiento, la aplicación deberá indicar claramente al usuario que la transcripción se encuentra en curso.

Cuando la transcripción finalice correctamente, el texto obtenido deberá agregarse al final del contenido textual existente del relevamiento.


**Reglas de negocio**


La transcripción deberá iniciarse mediante una acción explícita del usuario.

La transcripción de un nuevo audio deberá agregarse al final del relevamiento textual existente, sin eliminar ni reemplazar observaciones anteriores.

Tanto los audios grabados desde la aplicación como los archivos cargados deberán procesarse mediante el mismo mecanismo de transcripción.

Mientras la transcripción se encuentre en procesamiento, el sistema deberá indicar visualmente que la operación está en curso y no deberá permitir iniciar nuevamente la transcripción del mismo audio.

Cuando la transcripción finalice correctamente, el texto resultante deberá incorporarse al relevamiento y el audio dejará de encontrarse pendiente de procesamiento.

Una vez completada correctamente la transcripción, el archivo de audio no deberá conservarse como documentación permanente del presupuesto.

Si la transcripción no puede completarse correctamente, el contenido textual existente del relevamiento no deberá modificarse ni perderse.

Si la transcripción falla, el audio deberá mantenerse temporalmente disponible para que el usuario pueda volver a intentar la transcripción o quitarlo.

Un intento fallido de transcripción no deberá agregar contenido parcial o incompleto al relevamiento.

Proceso de negocio al que responde: Relevamiento previo a la elaboración del presupuesto.

Dependencias: RF-07.

Necesidad asociada: N-01

Prioridad: Alta.

## RF-009 — Consultar y editar el relevamiento registrado


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir visualizar en un mismo contenido textual todas las observaciones registradas para el relevamiento, independientemente de que hayan sido ingresadas manualmente o mediante la transcripción de audios.

El usuario deberá poder editar dicho contenido para agregar, corregir o eliminar información antes de realizar el análisis del relevamiento.


**Reglas de negocio**


El contenido editado por el usuario deberá conservarse como la información actual del relevamiento.

Proceso de negocio al que responde: Relevamiento previo a la elaboración del presupuesto.

Dependencias: RF-06; RF-08

Necesidad asociada: N-01

Prioridad: Alta.

## RF-010 — Incorporar nuevas observaciones a un relevamiento existente.


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario continuar incorporando observaciones mediante texto, grabación directa o carga de un archivo de audio.

Cuando se incorpore una nueva observación mediante audio, su transcripción deberá agregarse al contenido textual existente para que el usuario pueda continuar revisándolo y editándolo.


**Reglas de negocio**


La incorporación de nuevas observaciones no deberá reemplazar la información registrada anteriormente.

Proceso de negocio al que responde: Relevamiento previo a la elaboración del presupuesto.

Dependencias: RF-09.

Necesidad asociada: N-01

Prioridad: Alta.

## RF-011 — Analizar relevamiento mediante inteligencia artificial


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario solicitar el análisis mediante inteligencia artificial del contenido textual completo del relevamiento.

La inteligencia artificial deberá procesar la información registrada e identificar los conceptos que resulten relevantes para la elaboración del presupuesto.

El resultado deberá presentarse al usuario mediante una tabla editable.


**La tabla deberá permitir representar, para cada fila**


tipo de concepto;

concepto detectado;

concepto;

cantidad;

unidad;

precio o valor y, cuando sea monetario, su moneda (USD o UYU);

Subtotal y, cuando sea monetario, su moneda.

Al final de la tabla deberá mostrarse una fila Total con resultados en USD y UYU calculados conforme a RF-29, sin sumar directamente importes de distintas monedas. Cada concepto conservará su precio o importe y moneda correspondientes; los conceptos porcentuales, como Margen, seguirán sus reglas específicas de cálculo.


**Reglas de negocio**


El análisis deberá realizarse utilizando el contenido completo del relevamiento disponible al momento de solicitarlo.

Los conceptos identificados mediante inteligencia artificial deberán considerarse propuestas y podrán ser revisados, modificados o eliminados por el usuario.

Para los materiales, el precio o valor corresponderá al precio unitario del material seleccionado.

Para los conceptos cuyo costo sea ingresado directamente por el usuario, el valor registrado podrá utilizarse directamente como subtotal.

Para los conceptos cuya forma de cálculo sea porcentual, como el margen, el valor corresponderá al porcentaje registrado y el subtotal deberá ser calculado de acuerdo con las reglas definidas para dicho concepto.

Cuando la inteligencia artificial identifique un elemento como material, el tipo de concepto correspondiente deberá establecerse como Material.

El sistema no deberá permitir iniciar el análisis del relevamiento mientras exista un audio pendiente de transcripción o una transcripción en procesamiento. Para continuar, el usuario deberá completar correctamente la transcripción del audio pendiente o quitar dicho audio.

El campo Concepto detectado deberá conservar la denominación identificada originalmente por la inteligencia artificial durante el análisis del relevamiento.

El campo Concepto deberá representar el concepto que finalmente se utilizará en el presupuesto.

Cuando el tipo de concepto sea Material, el campo Concepto deberá corresponder al material seleccionado del catálogo. Mientras no exista un material seleccionado, dicho campo podrá permanecer sin completar.

Para los conceptos identificados por inteligencia artificial cuyo tipo sea distinto de Material, el campo Concepto deberá tomar inicialmente el mismo contenido que Concepto detectado y podrá posteriormente ser modificado por el usuario.

El campo Concepto detectado deberá conservar la información originalmente obtenida mediante inteligencia artificial y no deberá modificarse como consecuencia de las correcciones realizadas posteriormente sobre el campo Concepto.

Proceso de negocio al que responde: Análisis del relevamiento y elaboración del presupuesto.

Dependencias: RF-09

Necesidad asociada: N-01

Prioridad: Alta

## RF-012 — Buscar y seleccionar manualmente materiales del catálogo


**Descripción**


El sistema deberá permitir al usuario buscar manualmente materiales ya registrados en el catálogo de EOP y seleccionar cuál corresponde a una fila de tipo Material generada por el análisis del relevamiento.

La IA podrá identificar el tipo de concepto y conservar su denominación en Concepto detectado, pero no deberá buscar, preseleccionar ni vincular automáticamente registros del catálogo.


**Reglas de negocio**


El campo Concepto detectado conservará la denominación identificada originalmente por la IA; no implicará ninguna relación con un material del catálogo.

El campo Concepto permanecerá sin material vinculado hasta que el usuario realice una búsqueda y seleccione explícitamente uno de los materiales existentes.

La búsqueda deberá permitir localizar materiales registrados, sin asociarlos automáticamente según similitud, coincidencias parciales ni igualdad del texto detectado.

La selección de un material existente no creará, duplicará ni modificará registros del catálogo.

Si el material buscado no existe, la fila podrá continuar sin selección; los materiales nuevos solo se registrarán desde el catálogo mediante RF-68, nunca desde el presupuesto.

La vinculación deberá verificarse conforme a los permisos fijos del rol y, en el caso del Operador, a su responsabilidad sobre el presupuesto.

Proceso de negocio al que responde: Análisis del relevamiento y gestión de materiales de referencia.

Dependencias: RF-11; RF-67; RF-73.

Necesidad asociada: N-01

Prioridad: Alta

## RF-013 — Revisar y confirmar la selección manual de materiales


**Descripción**


El sistema deberá permitir al usuario revisar y confirmar manualmente el material del catálogo asociado a cada fila de tipo Material del presupuesto.

El usuario utilizará el selector/buscador del campo Concepto para seleccionar un material existente o sustituir su selección por otro; no existirán propuestas ni selecciones automáticas del catálogo.


**Reglas de negocio**


Cada asociación material del catálogo–fila de presupuesto deberá originarse en una selección explícita del usuario.

El campo Concepto detectado será informativo y conservará la denominación original obtenida del relevamiento.

El usuario podrá corregir la selección mientras el presupuesto sea editable, respetando las validaciones aplicables.

La confirmación de una selección no creará un material nuevo ni actualizará datos del catálogo.

Si el material requerido no existe, se deberá registrar previamente desde el módulo Catálogo de Materiales conforme a RF-68 y luego seleccionarlo; no se abrirá un alta de material dentro del presupuesto.

Se conservarán las restricciones sobre combinaciones material–proveedor repetidas dentro de un presupuesto previstas en RF-20.

Proceso de negocio al que responde: Validación del relevamiento y gestión de materiales de referencia.

Dependencias: RF-12; RF-67; RF-68; RF-73.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-014 — Agregar manualmente un material al relevamiento analizado.


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario agregar manualmente un material a la tabla de elementos obtenida luego del análisis del relevamiento.

Al incorporar un material, el usuario deberá poder buscar entre los materiales previamente registrados en el catálogo de EOP y seleccionar el que corresponda.

La búsqueda deberá facilitar la localización de materiales existentes a partir del texto ingresado por el usuario.


**Reglas de negocio**


Cuando el usuario seleccione un material existente, deberá utilizarse el registro correspondiente del catálogo y no crearse un nuevo material.

El material agregado deberá quedar asociado al presupuesto en elaboración.

La incorporación de un material existente al presupuesto no deberá modificar la información general almacenada sobre dicho material en el catálogo.

Cuando una fila de tipo Material sea incorporada manualmente por el usuario y no provenga del análisis mediante inteligencia artificial, el campo Concepto detectado no aplicará y el material seleccionado del catálogo deberá registrarse en el campo Concepto

Proceso de negocio al que responde: Validación y complementación del relevamiento.

Dependencias: RF-11; catálogo de materiales.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-015 — Registrar un nuevo material durante la elaboración del presupuesto


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-008, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

Por sus permisos base, el Operador podrá buscar, consultar y seleccionar materiales existentes, pero no registrar uno nuevo; el Supervisor y el Administrador sí podrán registrarlo. Un Operador podrá registrarlo solamente con el permiso individual específico concedido conforme a RF-73 y acceso efectivo para elaborar el Presupuesto correspondiente, sin omitir las reglas del material. El sistema deberá permitir al usuario registrar un nuevo material cuando no encuentre dentro del catálogo un material correspondiente al elemento identificado o ingresado durante la elaboración del presupuesto.

Cuando el material provenga del análisis del relevamiento y no se encuentre ninguna coincidencia adecuada, deberá conservarse como base la denominación identificada por la inteligencia artificial. El usuario deberá poder corregir dicha denominación antes de registrar el nuevo material.

Cuando el material provenga del análisis del relevamiento y no exista un material correspondiente en el catálogo, al iniciar su creación el sistema deberá utilizar como nombre inicial la denominación registrada en el campo Concepto detectado. El usuario deberá poder modificar dicha denominación antes de confirmar el registro.

Cuando la creación de un nuevo material se origine a partir de una incorporación manual, el nombre inicial podrá utilizar como base el texto ingresado por el usuario durante la búsqueda del material.

Para registrar el material deberán completarse obligatoriamente:

- nombre;

- unidad de medida;

- proveedor;

- precio unitario estrictamente mayor que cero, ingresado manualmente salvo que se obtenga automáticamente de una vinculación validada; y moneda USD o UYU seleccionada obligatoriamente por el usuario en ambos casos y conservada con el precio, sin extraerla automáticamente de Fivisa.

Una vez registrado, el nuevo material deberá quedar seleccionado en el campo Concepto de la fila correspondiente y quedar disponible en el catálogo para futuros presupuestos.

Cuando el proveedor seleccionado corresponda a Fivisa, el sistema deberá permitir iniciar el proceso de vinculación del material con un producto específico de dicho proveedor según lo establecido en RF-92.

Cuando la vinculación con Fivisa sea validada correctamente, el precio unitario inicial de la relación material-proveedor deberá ser el precio normal publicado recuperado desde Fivisa de acuerdo con RF-92, sin que el usuario deba ingresarlo manualmente.

El nombre y la unidad de medida del material deberán corresponder a los datos registrados por el usuario en EOP y no deberán ser reemplazados por la información obtenida desde Fivisa.


**Reglas de negocio**


El material no deberá registrarse automáticamente por el solo hecho de no haberse encontrado una coincidencia.

La creación deberá iniciarse mediante una acción explícita del usuario.

Antes de crear el material, el usuario deberá haber podido consultar o buscar materiales existentes. Al confirmar su creación desde el presupuesto, se impedirá registrar un nombre cuya denominación completa, ignorando mayúsculas, minúsculas y espacios innecesarios, coincida con la de otro material del catálogo, y se informará al usuario; nombres distintos no se equipararán necesariamente.

El usuario podrá modificar el nombre propuesto antes de crear el material.

Todo nuevo material deberá registrar nombre y seleccionar obligatoriamente su Unidad mediante un selector cargado con las Unidades registradas en la aplicación. Las nuevas Unidades registradas mediante RF-99 quedarán inmediatamente disponibles; no se permitirá registrar el material con una Unidad inexistente.

Al crear un material deberá seleccionarse un proveedor, una moneda válida (USD o UYU) y disponerse de un precio unitario estrictamente mayor que cero. Para proveedores sin integración automática, el precio deberá ser ingresado por el usuario. Para una relación correctamente vinculada con Fivisa, el precio normal podrá obtenerse automáticamente mediante RF-92, pero la moneda deberá seleccionarla obligatoriamente el usuario; no podrá inferirse de la página de Fivisa. El Material y su primera relación MaterialProveedor válida deberán registrarse conjuntamente: si esta última falla o queda incompleta, el Material no quedará registrado definitivamente.

El precio deberá quedar asociado a la combinación material-proveedor y deberá registrarse automáticamente su fecha de actualización.

Cuando el proveedor necesario no exista, deberá poder registrarse mediante el proceso definido en RF-23.

Cuando el proveedor seleccionado corresponda a Fivisa, el usuario deberá poder vincular la relación material-proveedor con un producto específico de Fivisa mediante RF-92.

La información recuperada desde Fivisa no deberá modificar el nombre ni la unidad de medida definidos para el material dentro del catálogo de EOP.

Una vez confirmado el registro de un nuevo material, el material y su relación inicial material-proveedor válida deberán quedar disponibles para su utilización en el presupuesto actual y en presupuestos futuros. El flujo de creación desde el presupuesto conservará las confirmaciones diferidas ya definidas para modificar relaciones de materiales existentes; en ningún caso quedará definitivamente registrado un Material nuevo sin su primera relación válida.

Proceso de negocio al que responde: Elaboración de presupuestos y mantenimiento del catálogo de materiales.

Dependencias: RF-12; RF-13; RF-23; RF-99.

Necesidad asociada: N-01 / N-02.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-016 — Agregar manualmente conceptos distintos de materiales al presupuesto


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario agregar manualmente a la tabla del presupuesto conceptos distintos de materiales que resulten necesarios para determinar el costo o precio del trabajo.

Para incorporar uno de estos conceptos, el usuario deberá seleccionar el tipo de concepto correspondiente entre los tipos disponibles.

Los tipos podrán representar conceptos como Material, Mano de obra, Alquiler, Traslado, Costos indirectos, Margen u otros definidos por EOP.

Para los tipos de concepto cuyo importe no se calcule mediante una regla específica, el usuario deberá poder ingresar manualmente el importe correspondiente y seleccionar su moneda, USD o UYU. Cada concepto con precio o importe monetario ingresado conservará su valor original y su moneda; no se almacenará una cotización histórica individual por concepto.


**Reglas de negocio**


Todo concepto deberá poseer un tipo de concepto.

El usuario podrá ingresar una descripción que permita identificar el concepto cuando corresponda.

Para los conceptos que utilicen un importe manual, dicho importe deberá utilizarse directamente como subtotal de la fila.

Los campos cantidad y unidad no serán obligatorios para conceptos cuyo costo se registre directamente mediante un importe total.

Los conceptos agregados deberán incorporarse al cálculo total del presupuesto.

El margen se regirá por las reglas específicas definidas en RF-27.

Los tipos de concepto disponibles deberán encontrarse previamente definidos y disponibles para su selección durante la elaboración del presupuesto.

Para los conceptos agregados manualmente que no provengan del análisis mediante inteligencia artificial, el campo Concepto detectado no aplicará y la descripción ingresada por el usuario deberá registrarse en el campo Concepto.

Cuando el usuario seleccione el tipo de concepto Margen, no deberá ingresar directamente su importe. El porcentaje, la selección de los tipos de concepto que constituirán su base y el cálculo del subtotal deberán gestionarse de acuerdo con RF-27.

Un presupuesto podrá contener como máximo un único concepto de tipo Margen. Si el presupuesto ya posee un margen registrado, el sistema no deberá permitir agregar otro concepto de dicho tipo.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-11.

Necesidad asociada: N-01 / N-07.

Prioridad: Alta.

## RF-017 — Modificar elementos del relevamiento analizado


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario modificar individualmente los elementos incluidos en la tabla obtenida luego del análisis del relevamiento.

El usuario deberá poder corregir, cuando corresponda, datos como el Concepto, cantidad, unidad u otra información editable asociada.

El campo Concepto detectado deberá conservar la denominación obtenida originalmente durante el análisis mediante inteligencia artificial y no deberá modificarse al corregir el campo Concepto.


**Reglas de negocio**


No se definen reglas adicionales a las indicadas en la descripción.

Proceso de negocio al que responde: Validación del relevamiento.

Dependencias: RF-11; RF-12; RF-13.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-018 — Eliminar elementos del relevamiento analizado


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario eliminar de la tabla aquellos materiales, equipos, trabajos u otros elementos que hayan sido identificados durante el análisis pero que no correspondan al trabajo a presupuestar.


**Reglas de negocio**


La eliminación de un elemento de la tabla durante la elaboración del presupuesto deberá afectar únicamente a dicha fila y no requerirá una confirmación adicional.

Proceso de negocio al que responde: Validación del relevamiento.

Dependencias: RF-11.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-019 — Consultar referencias de precios de los materiales por proveedor.


**Descripción**


El sistema deberá mostrar para un material seleccionado las referencias material-proveedor registradas en el catálogo de EOP, indicando proveedor, precio, moneda (UYU o USD) y fecha de última actualización.

La consulta será de solo lectura durante la presupuestación y permitirá elegir una referencia existente sin registrar o actualizar precios ni proveedores desde esta pantalla.


**Reglas de negocio**


Cada relación material-proveedor tendrá un precio actual estrictamente mayor que cero, moneda USD o UYU y fecha de última actualización.

Un material podrá tener varias referencias correspondientes a distintos proveedores registrados.

Los valores consultados serán los que existan en el catálogo al realizar la consulta; las referencias previamente copiadas a presupuestos mantendrán sus valores históricos.

Las modificaciones a relaciones existentes se harán desde la edición del material conforme a RF-21 y RF-108; las nuevas asociaciones se darán de alta desde catálogo mediante RF-22.
No habrá actualización automática de precios ni vinculación con Fivisa.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-67; RF-68; RF-22.

Necesidad asociada: N-01 / N-02.

Prioridad: Alta.

## RF-020 — Seleccionar referencia de proveedor y conservar precio histórico


**Descripción**


El sistema deberá permitir seleccionar, para un material de catálogo ya elegido, una referencia de proveedor previamente registrada y copiar su precio actual y moneda al concepto del presupuesto.

El precio y la moneda copiados serán valores históricos propios de ese concepto y no podrán editarse desde la tabla del presupuesto. Para actualizarlos se modificará el catálogo y, si se desea aplicar la referencia actualizada, el usuario deberá realizar una nueva selección.


**Reglas de negocio**


Antes de seleccionar una referencia deberán mostrarse proveedor, precio, moneda y fecha de última actualización.

La referencia de precio será seleccionada expresamente por el usuario, incluso cuando exista una única opción; no se seleccionará automáticamente por cantidad de proveedores.

Una vez seleccionada, el precio y la moneda se copiarán al concepto del presupuesto y deberán conservarse aunque el catálogo cambie posteriormente.

No se permitirá modificar precio ni moneda del concepto provenientes de la referencia de catálogo directamente desde la tabla del presupuesto.

La selección de una referencia no actualizará el catálogo ni alterará las demás referencias del mismo material.
Dentro de un presupuesto no podrá registrarse más de una fila de tipo Material con la misma combinación de material y proveedor. La restricción se verificará al seleccionar o modificar el material o el proveedor de una fila, independientemente de si la fila fue generada mediante IA o agregada manualmente; se admitirá el mismo material con proveedores diferentes.
Si la combinación ya existe, el sistema informará que se debe modificar la cantidad de la fila correspondiente o seleccionar otra referencia válida.
El usuario con permisos de consulta del presupuesto podrá revisar los datos históricos copiados, conforme al estado y autorizaciones aplicables.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-12; RF-13; RF-19; RF-73.

Necesidad asociada: N-01 / N-02.

Prioridad: Alta.

## RF-021 — Actualizar precio y moneda de una relación material-proveedor desde el catálogo


**Descripción**


El sistema deberá permitir al Supervisor o Administrador actualizar expresamente, desde la edición de un material en el Catálogo de Materiales, el precio y/o la moneda de una relación material-proveedor ya registrada.

Cada cambio realizado y confirmado actualizará la fecha de última actualización de dicha referencia. No se realizarán modificaciones desde la tabla del presupuesto ni sincronizaciones automáticas con Fivisa.


**Reglas de negocio**


La modificación corresponderá exclusivamente a la relación material-proveedor seleccionada, sin afectar relaciones con otros proveedores.

El precio actual deberá ser estrictamente mayor que cero y la moneda únicamente UYU o USD.

Si se modifica un solo campo, el otro conservará su valor válido anterior.

La fecha de última actualización se actualizará al guardar correctamente un cambio de precio y/o moneda; no se requiere mantener historial de precios anteriores en el catálogo.

No se podrán registrar cambios de precios del catálogo como pendientes dentro de un presupuesto ni aplicarlos al confirmar su resumen.

Los conceptos de presupuestos ya guardados conservarán precios y monedas históricos sin modificación retroactiva.

El mantenimiento de una relación existente corresponde a RF-108; la creación de una nueva relación corresponde a RF-22.

Solamente los roles Supervisor y Administrador podrán editar referencias; los Operadores podrán consultarlas y seleccionarlas conforme a RF-73.

Proceso de negocio al que responde: Elaboración de presupuestos y actualización de precios de materiales.

Dependencias: RF-67; RF-73; RF-108.

Necesidad asociada: N-01 / N-02.

Prioridad: Alta.

## RF-022 — Registrar una nueva relación material-proveedor desde el catálogo


**Descripción**


El sistema deberá permitir al Supervisor o Administrador registrar una nueva relación entre un material existente y un proveedor previamente registrado, exclusivamente desde el Catálogo de Materiales mediante la acción Agregar proveedor.

El usuario deberá elegir un proveedor existente e ingresar manualmente precio actual, estrictamente mayor que cero, y moneda UYU o USD; la fecha de actualización se registrará al confirmar el alta.


**Reglas de negocio**


La operación solo podrá iniciarse para un material previamente registrado; la primera relación del alta de un nuevo material se gestiona por RF-68.

La combinación material-proveedor será única en el catálogo; se rechazará una duplicación de relación.

No se permitirán altas de proveedores desde este formulario; el proveedor debe existir en el módulo Proveedores conforme a RF-23.

El precio inicial deberá ingresarse manualmente y ser estrictamente mayor que cero; moneda obligatoria UYU o USD.

Al guardar correctamente se registrará la relación junto con su fecha de última actualización; un error no deberá dejar una asociación parcial.

No habrá vinculación, importación o sincronización automática con precios de Fivisa.

No se crearán relaciones pendientes desde un presupuesto ni se registrarán al confirmar el resumen del presupuesto.

La nueva relación deberá quedar disponible para selección posterior conforme a RF-19 y RF-20.
El mantenimiento posterior de precio y moneda de una relación existente se realizará por RF-21 y RF-108.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-23; RF-67; RF-68; RF-73.

Necesidad asociada: N-01 / N-02.

Prioridad: Alta.

## RF-023 — Seleccionar proveedores existentes y registrar proveedores desde su módulo


**Descripción**


El sistema deberá permitir consultar y seleccionar Proveedores ya registrados durante presupuestación, materiales o registro de facturas de compra. Las nuevas altas de Proveedores se realizarán exclusivamente desde la sección independiente Proveedores y solo por Supervisor o Administrador, conforme a RF-73.

En la sección Proveedores, el alta deberá permitir registrar Nombre, RUT, Teléfono, Correo de contacto, Correo de facturación y una o más Cuentas bancarias, con las validaciones descritas en las reglas siguientes.

Fuera de la sección Proveedores únicamente se ofrecerá la selección de un proveedor existente; no se podrá abrir ni completar su alta dentro de un presupuesto, material o factura.


**Reglas de negocio**


La selección de un proveedor existente reutilizará su registro y no creará nuevos proveedores.

Únicamente Supervisor y Administrador podrán registrar nuevos proveedores desde el módulo Proveedores. Operador podrá consultar o seleccionar los existentes según el contexto y sus permisos del rol.

No se permitirán proveedores duplicados por nombre completo normalizado ni por RUT normalizado; una coincidencia deberá impedir el alta e informar el motivo.

El alta se realizará mediante acción explícita y confirmación del usuario, no automáticamente por una búsqueda sin resultados.

El proveedor dado de alta quedará inmediatamente disponible en el catálogo para futuras selecciones desde los demás módulos.

Los nombres deberán compararse por denominación completa ignorando mayúsculas, minúsculas, tildes y espacios innecesarios. La ñ y la ü no se consideran tildes: se distinguen de la n y la u.

El RUT y el Nombre deberán validarse de manera independiente contra Proveedores existentes y cualquier duplicado impedirá confirmar el alta.

El Nombre y el RUT del Proveedor serán obligatorios y únicos cada uno; el Teléfono será obligatorio. El RUT se almacenará solo con dígitos, sin puntos, espacios ni guiones, tras validar la estructura uruguaya y su dígito verificador; se podrán normalizar separadores habituales antes de validarlo y la unicidad se comprobará sobre su valor normalizado. El Teléfono se almacenará como texto, nunca como número, en formato internacional compatible con E.164; podrán normalizarse espacios y separadores antes de validarlo.

El Proveedor dispondrá de Correo de contacto y Correo de facturación; al menos uno deberá estar informado y tener formato válido. Si ambos están vacíos, se impedirá el registro. Si ambos se informan, ambos deberán conservarse y tener formato válido.

Todo Proveedor deberá poseer al menos una Cuenta bancaria completa y podrá poseer múltiples Cuentas asociadas al mismo Proveedor.

Cada Cuenta bancaria requerirá Banco, Número de cuenta, Titular y Moneda. No se podrá guardar una Cuenta incompleta ni registrar el Proveedor sin una Cuenta válida.

Banco se seleccionará de la siguiente lista fija hardcodeada; no podrá escribirse libremente ni administrarse desde la aplicación: Banco de la República Oriental del Uruguay (BROU); Banco Hipotecario del Uruguay (BHU); Banco Bandes Uruguay S.A.; Banco Bilbao Vizcaya Argentaria Uruguay S.A. (BBVA); Banco BTG Pactual Uruguay S.A.; Banco de la Nación Argentina — Sucursal Uruguay; Banco Itaú Uruguay S.A.; Banco Santander S.A.; Banque Heritage (Uruguay) S.A.; Citibank N.A. — Sucursal Uruguay; Scotiabank Uruguay S.A.

La Moneda deberá seleccionarse exclusivamente entre UYU — Pesos uruguayos y USD — Dólares estadounidenses.

Durante el alta se podrá utilizar Agregar otra cuenta bancaria. Las Cuentas adicionales quedarán asociadas al mismo Proveedor, sin reemplazar las anteriores ni crear otro Proveedor.

La sección Proveedores mostrará a Operador, Supervisor y Administrador un listado sencillo de los Proveedores registrados, sin buscador ni filtros, desde el cual se accederá al alta, a la modificación (RF-109) y a la incorporación de Cuentas bancarias (RF-102), según los permisos de RF-73 (US-074).

Proceso de negocio al que responde: Gestión centralizada de Proveedores y selección desde presupuestos, catálogo de materiales y facturas de compra.

Dependencias: RF-73; módulo Proveedores.

Necesidad asociada: N-01 / N-02.

Prioridad: Alta.

## RF-024 — Registrar costos de mano de obra.


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario incorporar al presupuesto conceptos correspondientes a mano de obra.

Para cada concepto de mano de obra, el usuario deberá poder ingresar manualmente el importe total que EOP estime para dicho concepto.


**Reglas de negocio**


La mano de obra deberá registrarse como un concepto de tipo Mano de obra.

El importe será ingresado manualmente por el usuario.

El importe ingresado deberá utilizarse directamente como subtotal del concepto.

No será obligatorio registrar cantidad ni unidad para determinar el costo de mano de obra en esta primera versión.

Los subtotales correspondientes a mano de obra deberán incorporarse al cálculo total del presupuesto.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-16.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-025 — Registrar costos adicionales del presupuesto


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir incorporar conceptos económicos adicionales que no correspondan directamente a materiales, tales como alquileres, traslados, servicios de terceros u otros conceptos definidos por EOP.

El usuario deberá seleccionar el tipo de concepto correspondiente e ingresar manualmente su importe total.


**Reglas de negocio**


El usuario deberá seleccionar el tipo de concepto correspondiente.

Podrá registrar una descripción que permita identificar el costo.

El importe deberá ser ingresado manualmente.

El importe ingresado deberá utilizarse directamente como subtotal.

El subtotal deberá incorporarse al cálculo total del presupuesto.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-16.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-026 — Incorporar costos indirectos al presupuesto


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario incorporar al presupuesto uno o más conceptos correspondientes a costos indirectos.

En esta primera versión, el usuario deberá ingresar manualmente el importe total correspondiente a cada concepto de costo indirecto.


**Reglas de negocio**


Los costos indirectos deberán registrarse como conceptos del presupuesto.

El importe será ingresado manualmente por el usuario.

El importe ingresado deberá utilizarse directamente como subtotal.

Los subtotales correspondientes a costos indirectos deberán incorporarse al cálculo total del presupuesto.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-16.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-027 — Aplicar margen por tipo de concepto al presupuesto


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales.

El sistema deberá permitir incorporar al presupuesto un único margen, registrado como un tipo de concepto dentro de la tabla del presupuesto.

Cuando el usuario seleccione Agregar concepto → Margen, deberá ingresar el porcentaje de margen correspondiente. El sistema deberá identificar los distintos tipos de conceptos presentes en el presupuesto y mostrar, para cada uno, la suma de sus subtotales por moneda, sin sumar nominalmente USD y UYU.

El usuario deberá poder seleccionar uno o más de los tipos de conceptos mostrados para utilizarlos como base de cálculo del margen.

La base de cálculo en UYU se obtendrá de los subtotales correspondientes a los tipos de conceptos seleccionados: se convertirán primero los importes en USD a UYU con la única cotización actual prevista en RF-29, aunque la base esté compuesta exclusivamente por conceptos en USD, y luego se sumarán los importes en UYU. El sistema deberá aplicar el porcentaje de margen ingresado sobre esa base y mostrar el importe resultante como subtotal del concepto Margen en UYU, sin sumar nominalmente importes de distintas monedas.


**Reglas de negocio**


Un presupuesto podrá contener como máximo un único concepto de tipo Margen.

El margen deberá registrarse como un tipo de concepto del presupuesto.

Para incorporar el margen, el usuario deberá ingresar el porcentaje correspondiente.

El sistema deberá identificar los tipos de conceptos actualmente presentes en el presupuesto y calcular, para cada tipo, las sumas de sus subtotales separadas por moneda, sin sumar nominalmente USD y UYU.

El usuario deberá seleccionar uno o más tipos de conceptos como base para aplicar el margen.

La base de cálculo del margen deberá expresarse en UYU: para los tipos de conceptos seleccionados, los importes en USD se convertirán a UYU utilizando la única cotización actual prevista en RF-29, aun cuando la base contenga exclusivamente USD; posteriormente se sumarán los importes expresados en UYU. No se sumarán nominalmente importes de distintas monedas.

El subtotal del concepto Margen deberá calcularse de la siguiente forma:
Subtotal Margen = Base de cálculo × Porcentaje de margen / 100.

El importe calculado deberá mostrarse como subtotal del concepto Margen.

El subtotal del Margen, expresado en UYU, deberá incorporarse al Subtotal UYU y a los totales generales del presupuesto de acuerdo con RF-29.

Si posteriormente se modifica cualquier valor que intervenga en los subtotales de uno de los tipos seleccionados como base, el sistema deberá recalcular automáticamente la suma correspondiente a dicho tipo, la base de cálculo y el subtotal del margen.

Las modificaciones realizadas sobre conceptos pertenecientes a tipos que no hayan sido seleccionados como base no deberán modificar el subtotal del margen, aunque podrán modificar el importe total del presupuesto.

Si el presupuesto ya posee un concepto de tipo Margen, el sistema no deberá permitir registrar un segundo margen.

El concepto Margen no deberá utilizarse como base de cálculo de otro margen.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-16; RF-24; RF-25; RF-26; RF-28; RF-29.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-028 — Calcular costos de materiales


**Descripción**


El sistema deberá calcular automáticamente el costo correspondiente a cada material incluido en el presupuesto utilizando la cantidad requerida y el precio seleccionado para la combinación de material y proveedor correspondiente.

El sistema deberá calcular también el costo total correspondiente al conjunto de materiales incluidos en el presupuesto, respetando en cada fila el precio y la moneda propios del concepto, aunque cambie posteriormente el catálogo; para consolidar monedas distintas se aplicará RF-29.


**Reglas de negocio**


El costo de cada material deberá calcularse utilizando la cantidad registrada y el precio utilizado en el presupuesto.

Una modificación en la cantidad o en el precio utilizado deberá actualizar el costo correspondiente.

Para cada fila correspondiente a un material, el subtotal deberá calcularse como:
Cantidad × Precio unitario seleccionado.

El subtotal calculado deberá mostrarse en la columna correspondiente de la tabla.

Una modificación en la cantidad, proveedor o precio utilizado deberá provocar el recálculo del subtotal.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-17; RF-20.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-029 — Calcular importe del presupuesto


**Descripción**


El sistema deberá calcular automáticamente el importe total del presupuesto utilizando los subtotales correspondientes a los conceptos registrados.


**Reglas de negocio**


Cada concepto registrado deberá aportar al total mediante su subtotal correspondiente.

Para los materiales, el subtotal deberá obtenerse según RF-28.

Para los conceptos de importe manual, el subtotal deberá corresponder al importe ingresado.

Para el margen, el subtotal deberá calcularse según RF-27.

El sistema calculará dos subtotales internos a partir de los importes de los conceptos, respetando cantidades, porcentajes y margen: Subtotal USD, suma de importes expresados en USD; y Subtotal UYU, suma de importes expresados en UYU, incluido el Margen calculado siempre en UYU conforme a RF-27. No se sumarán nominalmente importes de distintas monedas ni se utilizarán cotizaciones históricas individuales de los conceptos. Con una única cotización UYU/USD aplicable a la fecha de consulta, obtenida mediante datosUruguay API según las reglas 8 y 9 de este requerimiento, Total UYU = Subtotal UYU + (Subtotal USD × Cotización) y Total USD = Subtotal USD + (Subtotal UYU ÷ Cotización); ambos totales se mostrarán simultáneamente.

Al final de la tabla deberá mostrarse una fila Total con los totales actualizados en USD y UYU.

El cálculo durante la elaboración utilizará los conceptos actualmente registrados y la única cotización actual consultada. Al confirmar los datos económicos mediante Confirmar datos y continuar (RF-31) se conservarán los Totales USD y UYU y la cotización utilizados, con su fecha de publicación, y la propuesta comercial elaborada externamente deberá reflejarlos; al registrar correctamente su envío se conservarán esos mismos valores sin sustituirlos por una cotización nueva por el mero tiempo transcurrido, ni se modificarán posteriormente por fluctuaciones de cotización. Si se modifican los datos económicos y vuelven a confirmarse, se conservarán los nuevos valores confirmados y el PDF comercial elaborado externamente deberá actualizarse y cargarse de nuevo mediante RF-44, manteniendo las reglas de vigencia y revisión.

Para la conversión entre USD y UYU durante la elaboración del presupuesto, el sistema deberá utilizar la cotización diaria del dólar publicada por el Banco Central del Uruguay, obtenida mediante datosUruguay API y expresada en pesos uruguayos por dólar estadounidense (UYU/USD). La cotización utilizada deberá ser estrictamente mayor que cero.

Cuando no exista una cotización publicada para la fecha actual, por ejemplo durante fines de semana o feriados, el sistema deberá utilizar la última cotización válida publicada con anterioridad. No deberá utilizar cotizaciones posteriores, valores estimados ni promedios. Una falla temporal de datosUruguay no deberá interpretarse automáticamente como ausencia de publicación. Si no se puede obtener una cotización válida, el sistema deberá informar la situación, permitir guardar y continuar editando el borrador, pero no presentar importes convertidos como definitivos ni permitir confirmar los datos económicos (RF-31) sin una cotización válida.

Los cálculos económicos deberán realizarse conservando la precisión de los precios unitarios, cantidades, porcentajes y cotización, sin aplicar redondeos intermedios que alteren los resultados. Los totales finales del presupuesto deberán expresarse en USD y UYU con dos decimales, aplicando redondeo matemático habitual: cuando el tercer decimal sea 5 o superior, el segundo decimal se incrementará. El redondeo no deberá modificar los valores originales registrados para los conceptos.

El sistema deberá permitir identificar el valor y la fecha de publicación de la cotización utilizada para calcular los totales. Al confirmar los datos económicos conforme a RF-31 deberán conservarse los totales y la cotización aplicada, conforme a la regla 7, sin modificarlos posteriormente por nuevas publicaciones del tipo de cambio.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-24; RF-25; RF-26; RF-28.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-030 — Recalcular automáticamente el presupuesto


**Descripción**


El sistema deberá actualizar automáticamente los resultados económicos del presupuesto cuando se modifique alguno de los valores que intervienen en sus cálculos.

Esto deberá contemplar cambios en cantidades, precios de materiales, costos de mano de obra, alquileres, traslados, costos indirectos, porcentaje de margen, tipos seleccionados como base del margen u otros valores utilizados para calcular los subtotales y el importe total.


**Reglas de negocio**


Los resultados económicos mostrados deberán corresponder a los valores actualmente registrados en el presupuesto.

El subtotal de un concepto deberá recalcularse cuando se modifique cualquier dato que intervenga en su cálculo.

Cuando se modifique el subtotal de un concepto perteneciente a un tipo seleccionado como base del margen, el sistema deberá recalcular automáticamente la suma de subtotales correspondiente a dicho tipo, la base de cálculo del margen y el subtotal del concepto Margen.

Cuando el usuario modifique el porcentaje del margen o los tipos de conceptos seleccionados como su base, el subtotal del margen deberá recalcularse automáticamente de acuerdo con RF-27. Durante la elaboración, si cambia la cotización actual y la base contiene importes USD, también deberán actualizarse la base convertida y el Margen en UYU antes de calcular ambos totales.

Una modificación sobre un concepto perteneciente a un tipo que no forme parte de la base seleccionada para el margen no deberá modificar el subtotal de dicho margen.

La fila Total deberá actualizar simultáneamente los importes USD y UYU cuando cambie cualquier subtotal, incluido el Margen en UYU, o, durante la elaboración, la cotización actual utilizada conforme a RF-29. Una vez confirmados los datos económicos conforme a RF-31, el mero transcurso del tiempo no recalculará sus importes ni su cotización; al registrar el envío se conservarán esos mismos valores, sin consultar una nueva cotización para sustituirlos. Si se modifican y vuelven a confirmar los datos económicos, se conservarán los nuevos valores confirmados y el PDF comercial elaborado externamente deberá actualizarse y cargarse de nuevo mediante RF-44, manteniendo las reglas de vigencia y revisión.

El recálculo deberá contemplar materiales, conceptos con importes manuales y conceptos calculados mediante reglas específicas, como el margen.

Proceso de negocio al que responde: Elaboración económica del presupuesto.

Dependencias: RF-27; RF-28; RF-29.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-031 — Consultar resumen previo a una acción sobre el presupuesto


**Descripción**


El sistema deberá presentar un resumen actualizado de los conceptos, importes, monedas, proveedores seleccionados y totales calculados del presupuesto antes de confirmar los datos económicos.

Desde el resumen, el usuario podrá volver a editar los datos permitidos o seleccionar Confirmar datos y continuar, para guardar el presupuesto y avanzar a la sección Documentos sin actualizar precios ni crear relaciones material-proveedor en el catálogo.


**Reglas de negocio**


La consulta del resumen no modificará el estado del presupuesto ni los datos del catálogo.

El resumen deberá reflejar los conceptos y cálculos actualmente registrados y permitir revisar valores copiados desde las referencias de precio.

El usuario autorizado podrá volver a la edición mientras el estado del presupuesto lo permita.

Confirmar datos y continuar guardará los datos técnicos y económicos del presupuesto sin pasar automáticamente a Para revisión interna.

Al confirmar se conservarán los Totales USD y UYU, la cotización aplicada y su fecha de publicación, junto con la fecha de confirmación. Esos valores son los que debe reflejar la propuesta comercial elaborada externamente y los que se conservarán al registrar el envío (RF-29, RF-34). Si no se dispone de una cotización válida, no se permitirá confirmar los datos económicos.

La confirmación no aplicará cambios pendientes a precios ni monedas del catálogo, ni creará nuevas relaciones material-proveedor; esas operaciones se administran exclusivamente desde Catálogo de Materiales.

Si se modifican y confirman datos económicos después de cargar el PDF comercial, su vigencia deberá revisarse conforme a RF-44. La propuesta comercial se elabora externamente; no se generará un Word mediante IA.

Mientras sea editable, el usuario podrá regresar desde la etapa Documentos al resumen y a la tabla, realizar cambios permitidos y volver a confirmarlos.

Proceso de negocio al que responde: Revisión y avance del presupuesto dentro de su ciclo de elaboración.

Dependencias: RF-29; RF-30; RF-20; RF-73.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-032 — Enviar presupuesto a revisión interna


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir enviar un presupuesto a revisión interna una vez que sus datos hayan sido confirmados y exista una propuesta comercial final vigente en formato PDF.


**Reglas de negocio**


El envío a revisión interna deberá realizarse mediante una acción explícita del usuario. 2) No deberá permitirse enviar un presupuesto a revisión interna mientras no exista una propuesta comercial final vigente en formato PDF.

Un presupuesto en estado Borrador deberá pasar a Para revisión interna cuando se ejecute esta acción.

Un presupuesto en Solicitud de modificación del cliente deberá disponer nuevamente de una propuesta PDF actualizada antes de poder regresar a Para revisión interna.

El envío a revisión interna no deberá enviar todavía documentación al cliente.

Todo presupuesto deberá superar la revisión interna antes de ser enviado al cliente.

Proceso de negocio al que responde: Revisión interna de presupuestos.

Dependencias: RF-31; RF-02; RF-44.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-033 — Confirmar revisión interna del presupuesto


**Descripción**


Por sus permisos base, el Supervisor y el Administrador podrán confirmar la revisión interna; el Operador no podrá confirmar revisión interna, sin excepciones individuales conforme a RF-73. Se mantienen las condiciones funcionales del proceso. El sistema deberá permitir que un usuario autorizado confirme la revisión de un presupuesto que se encuentre en estado Para revisión interna.

La acción deberá estar disponible desde el listado de presupuestos y deberá permitir al usuario consultar previamente la propuesta comercial final y la documentación disponible.

La confirmación de la revisión se registrará en el mismo acto en que el usuario autorizado registre el envío externo de la propuesta conforme a RF-34; no existirá una confirmación de revisión separada que deba conservarse hasta ese registro.


**Reglas de negocio**


Únicamente un usuario autorizado podrá confirmar la revisión.

El presupuesto deberá encontrarse en estado Para revisión interna.

Deberá existir un PDF final vigente.

Antes de confirmar, deberá poder consultarse el PDF final.

La acción Confirmar presupuesto abrirá el registro del envío externo definido en RF-34; la revisión quedará confirmada únicamente cuando ese registro se complete.

Si el usuario cancela el registro del envío, la revisión no quedará confirmada y el presupuesto permanecerá en Para revisión interna, sin datos de envío. El cambio a Enviado / pendiente de respuesta del cliente solamente se producirá al completarse el registro de acuerdo con RF-34.

Proceso de negocio al que responde: Revisión y envío de presupuestos.

Dependencias: RF-31; RF-32; gestión de usuarios y permisos.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-034 — Registrar envío externo de la propuesta comercial


**Descripción**


El sistema deberá permitir al Supervisor o Administrador registrar que la propuesta comercial final PDF fue enviada al cliente mediante un canal externo. Ese registro confirma en el mismo acto la revisión interna del presupuesto (RF-33).

El usuario efectuará el envío fuera de la aplicación por correo electrónico o WhatsApp; luego confirmará expresamente en el sistema que lo realizó y registrará el medio y la fecha. La aplicación no enviará automáticamente mensajes de correo ni WhatsApp ni adjuntará el PDF a comunicaciones salientes.


**Reglas de negocio**


Deberá existir una propuesta comercial final vigente en PDF y el presupuesto deberá encontrarse en estado Para revisión interna.

El envío real de la propuesta se realizará mediante herramientas externas a la aplicación de EOP.

Los únicos medios de envío que podrán registrarse serán Correo electrónico y WhatsApp.

El sistema deberá permitir descargar el PDF vigente para facilitar su envío externo. Cuando el usuario elija WhatsApp, deberá ofrecer la acción Abrir WhatsApp, que abre mediante Click to Chat la conversación con el número de WhatsApp registrado del Cliente y un mensaje precargado, sin adjuntar ni enviar documentos automáticamente.

La selección de un medio o la apertura de WhatsApp no deberán interpretarse como evidencia de envío realizado.

Solamente cuando el usuario autorizado confirme que efectivamente envió la propuesta se confirmará la revisión interna, se registrarán medio y fecha del envío y se actualizará el presupuesto al estado Enviado / pendiente de respuesta del cliente.

La fecha del envío se propondrá con la fecha del día y podrá modificarse; no podrá ser posterior a la fecha actual ni anterior a la fecha de carga del PDF final vigente. Si no cumple esta condición, se rechazará el registro y se informará el motivo.

Si el usuario cancela o no confirma el registro, el presupuesto deberá mantenerse en Para revisión interna sin fecha ni medio nuevos y sin revisión confirmada.

Al registrar el envío deberán conservarse los importes y cotización confirmados para esa propuesta, sin recalcularlos por el tiempo transcurrido.

Desde la fecha del envío efectivamente registrada deberá calcularse el vencimiento conforme a RF-36.

El registro del envío no deberá eliminar ni modificar el PDF vigente; si se registra un nuevo envío válido se conservará el medio y la fecha correspondientes al último envío efectivamente registrado.

No se admitirán permisos individuales de Operador para registrar envíos; se respetarán los permisos originales de Supervisor y Administrador.

Proceso de negocio al que responde: Envío y seguimiento de presupuestos.

Dependencias: RF-33; RF-44; RF-73; datos de contacto del cliente.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-035 — Registrar respuesta del cliente al presupuesto


**Descripción**


Por sus permisos base, el Supervisor y el Administrador podrán registrar la respuesta del Cliente; el Operador no tendrá autorización para registrar respuestas del Cliente, sin excepciones individuales conforme a RF-73. La aprobación que requiera crear un Proyecto exigirá además la autorización correspondiente a RF-52. El sistema deberá permitir a un usuario autorizado registrar manualmente la respuesta recibida del cliente respecto de un presupuesto que se encuentre en estado Enviado / pendiente de respuesta del cliente.

El usuario deberá poder indicar si el presupuesto fue aprobado, rechazado o si el cliente solicitó modificaciones.

Para los presupuestos que se encuentren en estado Enviado / pendiente de respuesta del cliente, desde el listado de presupuestos deberán encontrarse disponibles las acciones para registrar la respuesta recibida.

Cuando un presupuesto se encuentre en estado Vencido, las acciones Aprobar, Rechazar y Solicitud de modificación no deberán encontrarse disponibles para su ejecución directa. Si el cliente manifiesta una respuesta sobre un presupuesto vencido, deberá realizarse previamente su reapertura de acuerdo con RF-36. Una vez completada correctamente la reapertura y restablecido el estado Enviado / pendiente de respuesta del cliente, podrán registrarse las respuestas habituales.


**Reglas de negocio**


La respuesta del cliente deberá ser registrada manualmente por un usuario autorizado.

Cuando el cliente acepte el presupuesto, este deberá pasar al estado Aprobado. Cuando la aprobación requiera la creación de un proyecto, el estado Aprobado deberá registrarse definitivamente una vez completada correctamente dicha creación.

Cuando el usuario autorizado seleccione Rechazar, deberá seleccionar obligatoriamente un Motivo de rechazo mediante un desplegable de categorías cerradas durante la confirmación. Sin un Motivo seleccionado no podrá completarse el rechazo. Solo al confirmar se registrarán conjuntamente el estado Rechazado y el Motivo seleccionado junto al Presupuesto; antes de la confirmación el Estado no cambiará.

Cuando el cliente solicite cambios, el presupuesto deberá pasar al estado Solicitud de modificación del cliente.

Un presupuesto en estado Solicitud de modificación del cliente deberá poder ser editado.

Una vez realizadas las modificaciones solicitadas por el cliente, el presupuesto deberá volver al estado Para revisión interna antes de ser reenviado.

Una vez superada nuevamente la revisión interna y enviado nuevamente al cliente, el presupuesto deberá volver al estado Enviado / pendiente de respuesta del cliente.

Cuando la aprobación de un presupuesto implique la creación de un nuevo proyecto, el sistema deberá solicitar la información necesaria para completar dicha creación de acuerdo con RF-52.

Para un presupuesto en estado Enviado / pendiente de respuesta deberán estar disponibles las acciones:
- Aprobar;
- Rechazar;
- Solicitud de modificación.

Al seleccionar Aprobar y confirmar la acción, el presupuesto deberá pasar al estado Aprobado. Si el presupuesto no se encuentra asociado a un proyecto, deberá iniciarse el proceso de creación del proyecto según RF-52 y recién después de crear el proyecto se cambia el estado a Aprobado. Si ya se encuentra asociado a un proyecto, no deberá crearse otro proyecto.

Al seleccionar Rechazar, se solicitará confirmación y se exigirá seleccionar un Motivo de rechazo del catálogo cerrado mediante desplegable. Sin Motivo no se permitirá confirmar; al confirmar, el Presupuesto pasará a Rechazado, quedará almacenado el Motivo junto al Presupuesto y no se generará un Proyecto. Si se cancela, no cambiará el Estado ni se registrarán rechazo o Motivo. El Motivo confirmado quedará disponible para consulta posterior y estadísticas; para Aprobar y Solicitud de modificación no será obligatorio, ni se atribuirá por vencimiento o cancelación.

Al seleccionar Solicitud de modificación, el presupuesto deberá pasar al estado correspondiente y volver a quedar habilitado para edición.

Una nueva versión modificada deberá repetir el resumen y la confirmación de los datos económicos, la carga de un PDF final actualizado, la revisión interna y el registro del envío.

Antes de ejecutar las acciones Aprobar, Rechazar o Solicitud de modificación, el sistema deberá solicitar al usuario la confirmación de la acción seleccionada. Para Rechazar, el catálogo definitivo y cerrado confirmado por EOP comprende exactamente: Precio elevado; Falta de presupuesto o recursos del cliente; Proyecto postergado / no es prioridad actualmente; Selección de otro proveedor o competidor; La propuesta no se ajusta a la necesidad del cliente; Falta de aprobación interna del cliente; El cliente ya cuenta con otra solución o proveedor; Otro. El Motivo seleccionado es obligatorio para confirmar el rechazo; sin Motivo no se cambiará el Estado ni se registrará un rechazo parcial. Otro es categoría válida sin texto libre adicional obligatorio.

Cuando la aprobación requiera crear un nuevo proyecto, el presupuesto no deberá cambiar definitivamente a estado Aprobado hasta que el usuario complete y confirme correctamente los datos necesarios para la creación del proyecto según RF-52.

Si el usuario cancela dicho proceso antes de completar la creación del proyecto, no deberá crearse el proyecto ni registrarse la aprobación del presupuesto. Lo mismo se aplicará cuando un error impida completar correctamente la creación del proyecto, conforme a RF-52.

Las acciones Aprobar, Rechazar y Solicitud de modificación solamente podrán ejecutarse cuando el presupuesto se encuentre en estado Enviado / pendiente de respuesta del cliente.

Cuando un presupuesto se encuentre en estado Vencido, no deberá permitirse registrar directamente una respuesta del cliente.

Para registrar una respuesta correspondiente a un presupuesto Vencido, deberá completarse previamente el proceso de reapertura definido en RF-36.

Una vez completada correctamente la reapertura y restablecido el estado Enviado / pendiente de respuesta del cliente, deberán encontrarse nuevamente disponibles las acciones Aprobar, Rechazar y Solicitud de modificación.

Proceso de negocio al que responde: Seguimiento de presupuestos enviados.

Dependencias: RF-34; RF-02.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-036 — Gestionar vencimiento y reapertura del presupuesto


**Descripción**


El sistema deberá comprobar el vencimiento de un presupuesto al listar presupuestos, consultar su detalle o intentar realizar una operación sobre él; no se ejecutará una tarea programada dedicada exclusivamente a cambiar estados por vencimiento.

Si al efectuar cualquiera de estas comprobaciones se determina que el plazo de un presupuesto Enviado pendiente de respuesta terminó, deberá actualizarse a Vencido antes de mostrarlo o permitir otras operaciones. Supervisor y Administrador podrán reabrirlo conforme a las reglas existentes.


**Reglas de negocio**


El plazo de vigencia será expresado en días corridos y comenzará a partir de la fecha del envío externo efectivamente registrado conforme a RF-34.

La fecha inicial de vencimiento será fecha de envío registrada más plazo de vigencia, considerando días calendario consecutivos, incluidos fines de semana y feriados.

Antes del registro del envío no existirá fecha efectiva de vencimiento.

Las comprobaciones de vencimiento se harán al listar, consultar o intentar operar sobre el presupuesto; no dependerán de un proceso programado en segundo plano.

Si la fecha actual es igual o posterior a la fecha de vencimiento y el presupuesto permanece Enviado pendiente de respuesta, deberá establecerse el estado Vencido antes de permitir acciones incompatibles.

Los presupuestos Aprobados, Rechazados, en Solicitud de modificación o Cancelados no pasarán a Vencido mediante esa comprobación.

Únicamente Supervisor y Administrador podrán ejecutar Reabrir presupuesto y solo si el presupuesto está Vencido.

Para reabrir será obligatorio indicar nuevo plazo de vigencia en días corridos. Se registrará fecha de reapertura y la nueva fecha de vencimiento será fecha de reapertura más plazo.

La reapertura cambiará el estado a Enviado pendiente de respuesta, no habilitará edición directa ni registrará por sí sola una nueva comunicación comercial.

Mientras permanezca Vencido no se permitirán directamente Aprobar, Rechazar ni Solicitud de modificación.

Una vez reabierto, podrán registrarse respuestas del cliente conforme a RF-35; si nuevamente vence sin respuesta, se detectará por los mismos accesos u operaciones.

El Operador podrá consultar el vencimiento de presupuestos a los que tenga acceso, pero no reabrirlos mediante excepciones individuales.

Proceso de negocio al que responde: Seguimiento de presupuestos enviados y gestión de propuestas vencidas.

Dependencias: RF-34; RF-02; RF-35; RF-73.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-037 — Generar planilla de costos y materiales del presupuesto


**Descripción**


El sistema deberá generar a demanda, a partir de los datos técnicos y económicos registrados para un presupuesto, el contenido de la planilla de costos y materiales destinado a la descarga Excel.

La información comprenderá los conceptos, materiales, cantidades, referencias de proveedores, precios históricos, mano de obra, costos adicionales, costos indirectos, margen y totales que correspondan, conforme al formato que acuerde EOP.


**Reglas de negocio**


Cada generación utilizará los datos vigentes del presupuesto en ese momento y sus importes registrados, sin alterar cálculos ni registros.

Los valores de la planilla deberán coincidir con los datos económicos y las monedas del presupuesto; no se sustituirán precios históricos por valores más recientes del catálogo.

La planilla será un resultado generado a demanda y no se conservarán en la aplicación versiones anteriores ni un archivo Excel persistido.

El usuario podrá volver a generar la planilla cuando lo necesite, incluso sin cambios desde la última descarga.

PENDIENTE DE VALIDAR CON EL CLIENTE: estructura exacta, columnas y presentación de la planilla que utiliza EOP.

Proceso de negocio al que responde: Elaboración de documentación interna del presupuesto.

Dependencias: RF-29; RF-30; RF-20.

Necesidad asociada: N-01.

Prioridad: Media.

## RF-038 — Generar y descargar Excel del presupuesto a demanda


**Descripción**


El sistema deberá permitir al usuario autorizado solicitar la generación y descarga inmediata de un archivo .xlsx con la planilla de costos y materiales del presupuesto, conforme a RF-37.

El archivo se generará nuevamente cada vez que se solicite; no se almacenará como documento versionado del presupuesto ni se administrará una copia vigente en Azure Blob Storage.


**Reglas de negocio**


El archivo descargado deberá corresponder al presupuesto seleccionado y a los datos guardados del presupuesto en el momento de la solicitud.

Cualquier usuario con acceso de consulta al presupuesto podrá generar y descargar el archivo, conforme a RF-73; la operación es de solo lectura.

La generación se hará a demanda; no se exigirá marcar la planilla previa como desactualizada ni impedir nuevas descargas cuando no existan cambios.

El archivo .xlsx no se guardará ni versionará dentro de la aplicación, ni figurará como documento almacenado del presupuesto.

La generación y descarga no modificará el estado, los conceptos ni los datos del presupuesto.

Los importes y monedas exportados serán los propios del presupuesto y no serán sustituidos por valores actuales del catálogo.

La estructura y presentación final de la planilla estarán sujetas a validación de EOP conforme a RF-37.

Proceso de negocio al que responde: Elaboración de documentación interna del presupuesto.

Dependencias: RF-37; RF-29; RF-73.

Necesidad asociada: N-01.

Prioridad: Media.

## RF-039 — Utilizar estructura base para la propuesta comercial


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-015, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá utilizar una estructura base definida por EOP para elaborar la propuesta comercial que será presentada al cliente.

La estructura deberá permitir combinar contenido común utilizado por EOP con información específica correspondiente al presupuesto y al trabajo relevado.


**Reglas de negocio**


La propuesta comercial deberá corresponder al presupuesto para el cual se genera.

La información económica y técnica incorporada deberá provenir de los datos registrados para dicho presupuesto incluyendo la información extraida en el relevamiento.

PENDIENTE DE VALIDAR CON EL CLIENTE: definir la plantilla, secciones obligatorias y contenido fijo que deberá utilizarse para las propuestas comerciales.

Proceso de negocio al que responde: Elaboración de propuesta comercial.

Dependencias: Presupuesto elaborado; definición de plantilla por parte de EOP.

Necesidad asociada: N-01.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-040 — Generar contenido de propuesta comercial mediante inteligencia artificial


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-015, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Los permisos individuales específicos se aplicarán conforme a RF-73 sin omitir las reglas del proceso. El sistema deberá utilizar inteligencia artificial para generar el contenido inicial de la propuesta comercial utilizando la estructura definida por EOP, el relevamiento y la información técnica y económica confirmada del presupuesto.

El contenido generado deberá utilizarse para construir el documento editable definido en RF-42.


**Reglas de negocio**


El contenido generado mediante inteligencia artificial deberá considerarse un borrador y no una propuesta definitiva.

La inteligencia artificial no deberá modificar los valores económicos previamente calculados y registrados en el presupuesto.

El borrador deberá corresponder al presupuesto y cliente asociados.

La información suministrada a la inteligencia artificial deberá provenir del relevamiento y del presupuesto correspondiente.

Si los datos técnicos o económicos confirmados del presupuesto cambian posteriormente, el usuario deberá poder solicitar nuevamente la generación del contenido de la propuesta comercial.

Cada nueva generación deberá utilizar la información confirmada vigente al momento de solicitarla.

Proceso de negocio al que responde: Elaboración de propuesta comercial.

Dependencias: RF-39; relevamiento; información económica del presupuesto; servicio de inteligencia artificial.

Necesidad asociada: N-01.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-041 — Consultar borrador de la propuesta comercial


**Descripción**


Estado: DESCARTADO / INTEGRADO EN RF-40 Y RF-42. La funcionalidad inicialmente prevista en este requerimiento quedó integrada en el flujo definido por RF-40 y RF-42, por lo que RF-41 no constituye un requerimiento funcional independiente a implementar. Se conserva únicamente por trazabilidad histórica y para evitar alterar la numeración de los requerimientos posteriores. RF-40 y RF-42 quedaron, a su vez, fuera del alcance de la primera versión al excluirse US-015.


**Reglas de negocio**



**Proceso de negocio al que responde**



**Dependencias**



**Necesidad asociada**


Prioridad: No aplica (Descartado / Integrado en RF-40 y RF-42).

## RF-042 — Generar propuesta comercial editable en formato Word


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-015, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Los permisos individuales específicos se aplicarán conforme a RF-73 sin omitir las reglas del proceso. El sistema deberá generar un documento editable en formato Word .docx utilizando el contenido elaborado mediante RF-40 y la estructura definida por EOP.


**Reglas de negocio**


El formato editable utilizado será Word .docx.

La generación no deberá modificar los datos estructurados del presupuesto.

Los valores económicos incluidos deberán corresponder a los datos confirmados del presupuesto, incluidos los totales en USD y UYU calculados conforme a RF-29; al generar la propuesta deberán conservarse los importes y la cotización utilizados para ella, que serán los mismos que se conservarán al registrar correctamente el envío, sin recálculo automático por el mero tiempo transcurrido.

El documento generado deberá quedar asociado al presupuesto correspondiente.

El usuario podrá descargarlo para realizar modificaciones mediante Microsoft Word u otra herramienta compatible.

Si luego de generar el documento editable se modifican datos técnicos o económicos del presupuesto que puedan afectar su contenido, el documento generado deberá considerarse desactualizado.

Una vez confirmados nuevamente los datos modificados, el usuario deberá poder generar un nuevo documento editable utilizando la información vigente.

El documento generado más recientemente deberá considerarse la versión editable vigente generada por el sistema.

Mientras la propuesta generada corresponda a los datos confirmados vigentes del presupuesto, el sistema no deberá generar nuevamente contenido salvo solicitud explícita del usuario ante una nueva versión del presupuesto.

El archivo Word generado deberá denominarse Pres. AAAAMMDD - Nombre del Proyecto.docx, utilizando la fecha de creación y el nombre del proyecto registrados en el presupuesto.

Las regeneraciones conservarán la misma estructura de nombre, sin sufijos de versión.

Proceso de negocio al que responde: Elaboración y edición de propuesta comercial.

Dependencias: RF-29; RF-31; RF-40;

Necesidad asociada: N-01

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-043 — Descargar propuesta comercial editable


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-015, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Los permisos individuales específicos se aplicarán conforme a RF-73 sin omitir las reglas del proceso. El sistema deberá permitir al usuario descargar el documento editable generado para realizar modificaciones utilizando una herramienta externa.


**Reglas de negocio**


El archivo descargado deberá corresponder al presupuesto seleccionado.

La descarga del documento no deberá modificar el estado ni la información del presupuesto.

El archivo descargado deberá encontrarse en formato .docx.

Cuando se haya generado nuevamente la propuesta editable, la acción de descarga deberá proporcionar el último documento generado con los datos confirmados vigentes.

El archivo descargado conservará la nomenclatura de RF-42: Pres. AAAAMMDD - Nombre del Proyecto.docx, también cuando se descargue el documento vigente regenerado.

Proceso de negocio al que responde: Edición de propuesta comercial.

Dependencias: RF-42.

Necesidad asociada: N-01.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-044 — Cargar propuesta comercial final en formato PDF


**Descripción**


Por sus permisos base, el Operador podrá realizar esta acción en Presupuestos de los que sea Responsable; el Supervisor, sobre cualquier Presupuesto que cumpla las condiciones funcionales; el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. El sistema deberá permitir al usuario cargar en formato PDF la propuesta comercial definitiva elaborada por EOP con herramientas externas a la aplicación.

El documento PDF cargado deberá quedar asociado al presupuesto correspondiente y será considerado la propuesta comercial final destinada al cliente.


**Reglas de negocio**


La propuesta comercial final deberá cargarse en formato PDF.

El archivo PDF deberá quedar asociado al presupuesto correspondiente.

La carga del documento PDF no deberá modificar automáticamente la información técnica o económica registrada en el presupuesto. La propuesta final utilizará los importes y la cotización conservados al confirmar los datos económicos conforme a RF-31; al registrar su envío se conservarán esos mismos valores, sin recalcularlos por el tiempo transcurrido ni realizar una verificación automática del contenido del PDF.

La última propuesta cargada deberá considerarse la versión vigente.

Cuando se cargue un nuevo PDF final para el mismo presupuesto, este deberá reemplazar al documento final anteriormente almacenado.

No será necesario conservar un historial de versiones anteriores de la propuesta comercial.

El PDF final vigente deberá estar disponible para descarga por el usuario, que lo enviará manualmente mediante un canal externo. La aplicación no lo adjuntará ni enviará automáticamente por correo o WhatsApp, conforme a RF-34.

No podrá enviarse el presupuesto a revisión interna mientras no exista una propuesta comercial final vigente en formato PDF.

Si luego de cargar el PDF final se modifican datos técnicos o económicos del presupuesto, el documento final deberá considerarse desactualizado y deberá cargarse nuevamente un PDF actualizado antes de volver a enviarlo a revisión interna.

La propuesta comercial final vigente deberá denominarse Pres. AAAAMMDD - Nombre del Proyecto.pdf, utilizando la fecha de creación y el nombre del proyecto registrados en el presupuesto.

Cuando se cargue un nuevo PDF final para el mismo presupuesto, conservará esta estructura de nombre, sin sufijos de versión.

Proceso de negocio al que responde: Gestión documental de la propuesta comercial.

Dependencias: RF-29; RF-31; gestión documental del Presupuesto.

Necesidad asociada: N-01.

Prioridad: Alta.

## RF-045 — Consultar documentación asociada al presupuesto


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-022, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

Por sus permisos base, Operador, Supervisor y Administrador podrán ejecutar esta acción; las excepciones individuales para Operador y Supervisor se aplicarán conforme a RF-73. El sistema deberá permitir al usuario acceder, desde el listado general de presupuestos, a la documentación disponible correspondiente a cada presupuesto registrado.

Cuando se encuentre disponible la propuesta comercial final en formato PDF, el usuario deberá poder previsualizarla y descargarla. Cuando se encuentre disponible la planilla de costos y materiales en formato Excel, el usuario deberá poder descargarla.


**Reglas de negocio**


Cuando exista PDF final, el usuario deberá poder utilizar la acción Ver PDF.

El PDF deberá poder previsualizarse sin descargarlo previamente.

El usuario deberá poder descargar el PDF.

Cuando se encuentre disponible la planilla Excel del presupuesto, deberá poder utilizarse la acción Descargar Excel.

Los documentos accedidos deberán corresponder al presupuesto seleccionado.

El acceso o descarga de documentos no deberá modificar el presupuesto.

Proceso de negocio al que responde: Consulta y seguimiento histórico de presupuestos.

Dependencias: RF-03; RF-44.

Necesidad asociada: N-01.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-046 — Consultar proyectos registrados


**Descripción**


El sistema deberá permitir al usuario consultar los proyectos registrados en EOP y acceder a la ficha correspondiente a cada uno.

El listado deberá permitir identificar información básica de los proyectos y buscar un proyecto por su nombre.


**Reglas de negocio**


Todo proyecto registrado deberá permanecer disponible para su posterior consulta.

Desde el listado de proyectos deberá ser posible acceder a la ficha del proyecto seleccionado.

La información que deberá mostrarse para cada proyecto dentro del listado general es: Código, Nombre, Cliente, Estado, Tipo de Proyecto, Responsable y Fecha de inicio.

El listado mostrará los campos indicados, sin agregar otros campos

Proceso de negocio al que responde: Gestión y seguimiento de proyectos.

Dependencias: Registro de proyectos.

Necesidad asociada: N-04 / N-05.

Prioridad: Alta.

## RF-047 — Filtrar proyectos


**Descripción**


El sistema deberá permitir aplicar filtros sobre el listado general de proyectos para facilitar su localización y seguimiento.

Los proyectos deberán poder filtrarse por Cliente, Estado, Tipo de proyecto y Responsable. El filtro por Tipo de proyecto deberá utilizar los tipos de proyecto fijos definidos por la aplicación.

Los distintos criterios deberán poder combinarse entre sí cuando resulte necesario.


**Reglas de negocio**


La aplicación de filtros no deberá modificar la información registrada de los proyectos.

El usuario deberá poder filtrar los proyectos por Cliente.

El usuario deberá poder filtrar los proyectos por Estado.

El usuario deberá poder filtrar los proyectos por Tipo de proyecto.

Los valores disponibles en el filtro Tipo de proyecto deberán corresponder a los tipos de proyecto fijos de la aplicación.

La opción Todos no deberá restringir los resultados por Tipo de proyecto.

Al seleccionar un Tipo de proyecto, deberán mostrarse únicamente los proyectos asociados a dicho tipo.

El usuario deberá poder utilizar conjuntamente los filtros por Cliente, Estado, Tipo de proyecto y Responsable.

Cuando se utilicen varios filtros simultáneamente, deberán mostrarse únicamente los proyectos que cumplan todos los criterios seleccionados.

La búsqueda por Nombre y los filtros disponibles deberán poder utilizarse conjuntamente de acuerdo con la funcionalidad de consulta del listado.

El usuario deberá poder filtrar por Responsable. No se requieren otros filtros.

Proceso de negocio al que responde: Gestión y seguimiento de proyectos.

Dependencias: RF-46; tipos de proyecto fijos.

Necesidad asociada: N-04 / N-05.

Prioridad: Alta.

## RF-048 — Crear proyecto manualmente


**Descripción**


El sistema deberá permitir a los usuarios con rol Supervisor o Administrador crear un proyecto manualmente e incorporar automáticamente al Responsable designado como participante del nuevo Proyecto, sin exigir otra acción del usuario; el Operador no podrá realizar esta acción por su rol. No será necesario disponer previamente de un presupuesto.

Durante su creación deberá poder registrarse la información necesaria para identificar y gestionar posteriormente el proyecto, incluyendo su nombre, cliente, tipo de proyecto, etc.


**Reglas de negocio**


Cada proyecto deberá quedar identificado individualmente dentro del sistema.

Todo proyecto deberá estar asociado a un cliente.

Un proyecto podrá existir sin disponer inicialmente de un presupuesto asociado

Cada proyecto deberá registrar:
Nombre del proyecto
Cliente: selección entre los clientes previamente registrados en el sistema. Si el cliente necesario no se encuentra registrado, el usuario deberá poder iniciar el proceso de creación de un nuevo cliente de acuerdo con RF-49.
Tipo de proyecto: selección entre los tipos de proyecto fijos disponibles en la aplicación.
Responsable (select cargado con los usuarios activos de la aplicación)
Fecha de incio y Fecha estimada de finalización.

El Tipo de proyecto deberá seleccionarse entre los tipos de proyecto fijos definidos por la aplicación.

Todo proyecto creado manualmente deberá crearse automáticamente en estado Pendiente de inicio. El estado inicial no deberá ser seleccionado manualmente por el usuario durante la creación del proyecto.

Un proyecto podrá tener como máximo un presupuesto asociado.

El sistema deberá asignar automáticamente al proyecto un código interno único utilizando la estructura PRO-AAAAMMDD-NOMBRE DEL PROYECTO-####. AAAAMMDD será la fecha de creación del proyecto; NOMBRE DEL PROYECTO, su nombre registrado; y ####, el correlativo global de proyectos expresado mediante cuatro cifras. El usuario no ingresará el código manualmente. La secuencia no se reiniciará por fecha, cliente, Tipo de proyecto ni otra categoría y será independiente de la de presupuestos. El código permanecerá asociado al proyecto durante todo su ciclo de vida; modificar posteriormente su nombre u otra información general no generará un nuevo código.

Una vez creado correctamente, el usuario será dirigido automáticamente a la ficha del proyecto creado

El medio de comunicación no deberá seleccionarse durante la creación manual del proyecto. Un proyecto sin presupuesto enviado no tendrá un medio derivado ni se le asignará uno por defecto. Cuando posteriormente exista un presupuesto asociado cuyo envío al cliente haya sido correctamente registrado, el medio utilizado podrá obtenerse desde ese presupuesto.

Proceso de negocio al que responde: Creación y gestión de proyectos.

Dependencias: Gestión de clientes (RF-89); gestión de usuarios y tipos de proyecto fijos.

Necesidad asociada: N-04 / N-05.

Prioridad: Alta.

## RF-049 — Seleccionar cliente durante la creación de un proyecto


**Descripción**


El sistema deberá permitir al usuario buscar y seleccionar un cliente previamente registrado al crear un proyecto.

Cuando el cliente que se desea utilizar no se encuentre registrado, el usuario deberá poder iniciar explícitamente el registro de un nuevo cliente de acuerdo con RF-89.


**Reglas de negocio**


Cuando el usuario seleccione un cliente existente, deberá utilizarse dicho registro y no crearse un nuevo cliente.

La escritura o búsqueda de un nombre que no se encuentre registrado no deberá crear automáticamente un nuevo cliente.

Cuando el cliente no exista, el usuario deberá seleccionar explícitamente la opción de crear un nuevo cliente.

El registro del nuevo cliente deberá realizarse de acuerdo con RF-89.

Una vez registrado, el nuevo cliente deberá quedar disponible inmediatamente para ser seleccionado en el proyecto que se está creando.

Proceso de negocio al que responde: Creación de proyectos y gestión de clientes.

Dependencias: RF-48; RF-89.

Necesidad asociada: N-04.

Prioridad: Alta.

## RF-050 — Modificar información general de un proyecto


**Descripción**


El sistema deberá permitir a los usuarios autorizados modificar la información general registrada para un proyecto. El medio de comunicación utilizado para el envío del presupuesto asociado no deberá poder modificarse manualmente desde la información general del proyecto.


**Reglas de negocio**


Las modificaciones deberán realizarse sobre el mismo proyecto y no deberán generar un nuevo registro.

La información modificable es:
Nombre del proyecto
Tipo de proyecto
Responsable
Fecha incio
Fecha estimada de finalizacion

El Operador podrá modificar Nombre, Tipo de proyecto, Fecha de inicio y Fecha estimada de finalización únicamente si es Responsable del Proyecto, pero no cambiar al Responsable. Supervisor y Administrador podrán modificar esos datos y cambiar al Responsable de cualquier Proyecto. Un Operador no Responsable no podrá modificar la información general de ese Proyecto. Ningún rol podrá modificar el Cliente una vez creado el Proyecto.

Cuando se modifique el Responsable del Proyecto, el nuevo Responsable deberá ser un usuario Activo de la aplicación. En el mismo Proyecto, el Responsable anterior dejará de serlo y de figurar como participante; el nuevo quedará asignado como Responsable y se incorporará automáticamente como participante si todavía no figura, sin duplicados. Si el anterior tiene Tareas asignadas en ese Proyecto, el sistema informará la situación y no completará el cambio hasta que se reasignen a otros participantes según RF-63 o se eliminen con los procedimientos y permisos de RF-64: entretanto conservará sin cambios al Responsable y a los participantes, sin reasignar ni eliminar Tareas automáticamente. La actualización del Responsable y de los participantes deberá completarse consistentemente, sin aplicación parcial ante un error.

El cliente asociado a un proyecto no podrá modificarse una vez creado el proyecto.

Cuando se modifique el Tipo de proyecto de un proyecto existente, el nuevo valor deberá seleccionarse entre los tipos de proyecto fijos definidos por la aplicación.

Proceso de negocio al que responde: Gestión y mantenimiento de proyectos.

Dependencias: RF-48; RF-56.

Necesidad asociada: N-04 / N-05.

Prioridad: Alta.

## RF-051 — Gestionar estado del proyecto


**Descripción**


El sistema deberá mantener identificado el estado actual de cada proyecto y permitir su actualización manual de acuerdo con la situación de su ejecución.

Los estados disponibles serán valores fijos definidos por la aplicación: Pendiente de inicio; En ejecución; En pausa; Finalizado; Cancelado.

Todo nuevo proyecto deberá crearse inicialmente en estado Pendiente de inicio.

El cambio de estado deberá realizarse manualmente desde la ficha del proyecto mediante una acción que permita seleccionar un nuevo estado y confirmar el cambio.


**Reglas de negocio**


Todo proyecto deberá poseer un único estado actual identificable.

Los estados disponibles serán exclusivamente: Pendiente de inicio; En ejecución; En pausa; Finalizado; Cancelado.

Pendiente de inicio representará que el proyecto fue creado pero su ejecución todavía no comenzó.

En ejecución representará que el proyecto se encuentra actualmente en desarrollo.

En pausa representará que la ejecución fue detenida temporalmente y se prevé continuar posteriormente.

Finalizado representará que el trabajo previsto fue llevado a término.

Cancelado representará que se decidió detener anticipadamente el Proyecto sin completar normalmente su ejecución.

Todo proyecto nuevo deberá crearse automáticamente en estado Pendiente de inicio.

El estado inicial no deberá ser seleccionado manualmente durante la creación del proyecto.

El estado no deberá cambiar automáticamente por el transcurso del tiempo.

La actualización del estado deberá realizarse manualmente desde la ficha del proyecto.

Para cambiar el estado, el usuario autorizado deberá seleccionar uno de los estados disponibles y confirmar la acción.

Una vez confirmado correctamente el cambio, el proyecto deberá reflejar inmediatamente el nuevo estado.

El Operador podrá modificar manualmente el Estado únicamente del Proyecto del cual sea Responsable. No se admitirán permisos individuales para ampliar ese alcance.

Supervisor y Administrador podrán modificar el Estado de cualquier Proyecto.

Ser participante de un Proyecto no otorgará por sí solo autorización al Operador para modificar su Estado.

El flujo habitual Pendiente de inicio → En ejecución → Finalizado es descriptivo y no una restricción del sistema.

El flujo habitual En ejecución → En pausa → En ejecución también es descriptivo y no una restricción del sistema.

El paso a Cancelado podrá reflejar la decisión de detener anticipadamente el Proyecto, sin impedir transiciones posteriores autorizadas.

Los estados serán valores fijos del sistema y no podrán ser creados, modificados ni eliminados por los usuarios.

No deberá existir una funcionalidad administrativa para gestionar el catálogo de estados.

Cualquier Estado podrá cambiar manualmente a cualquiera de los otros Estados cuando la acción sea realizada por un usuario autorizado; no existen restricciones de transición por Estado de origen.

Proceso de negocio al que responde: Seguimiento del estado de ejecución de los proyectos.

Dependencias: RF-48; RF-54; gestión de usuarios, roles y permisos.

Necesidad asociada: N-05.

Prioridad: Alta.

## RF-052 — Iniciar creación de proyecto a partir de un presupuesto aprobado


**Descripción**


El sistema deberá iniciar el proceso de creación de un proyecto cuando un usuario autorizado registre la aprobación de un presupuesto que no se encuentre previamente asociado a un proyecto.

El Operador no podrá completar esta creación por su rol. Solo Supervisor o Administrador podrán completar la creación derivada. Para completar la creación, el sistema deberá reutilizar la información disponible en el presupuesto y solicitar al usuario los datos adicionales requeridos.

La aprobación del presupuesto y la creación del proyecto deberán completarse conjuntamente. Si el presupuesto ya se encuentra asociado a un proyecto existente, su aprobación no deberá iniciar la creación de otro proyecto.


**Reglas de negocio**


El proyecto deberá quedar vinculado al presupuesto aprobado que le dio origen.

El cliente asociado al proyecto deberá corresponder al cliente del presupuesto aprobado.

Si el presupuesto ya se encuentra asociado a un proyecto existente, su aprobación no deberá generar un nuevo proyecto ni solicitar información para la creación de otro.

La información adicional que deberá trasladarse automáticamente desde el presupuesto hacia el proyecto es:
Cliente, junto con sus datos de contacto registrados.
Tipo de proyecto
Nombre del proyecto

El proyecto creado a partir de un presupuesto aprobado deberá crearse automáticamente en estado Pendiente de inicio. El usuario no deberá seleccionar manualmente el estado inicial durante este proceso.

Cuando el presupuesto no se encuentre previamente asociado a un proyecto, el sistema deberá solicitar la información adicional necesaria para completar su creación.

El usuario deberá seleccionar un responsable entre los usuarios activos de la aplicación y el proyecto no podrá crearse sin un responsable asignado. Al crear el Proyecto, ese Responsable se incorporará automáticamente como participante sin una segunda acción y sin duplicarlo conforme a RF-56.

El usuario deberá indicar la fecha de inicio y la fecha estimada de finalización del proyecto.

La información obtenida del presupuesto deberá mostrarse previamente completada y no deberá requerirse que el usuario vuelva a ingresarla.

El medio de comunicación no deberá solicitarse durante la creación del proyecto. Cuando el presupuesto asociado posea un medio de envío registrado, dicho dato podrá consultarse posteriormente desde la información del proyecto; no será un dato independiente editable del proyecto.

Un presupuesto podrá estar asociado como máximo a un proyecto.

El sistema deberá asignar automáticamente al nuevo proyecto un código interno único con estructura PRO-AAAAMMDD-NOMBRE DEL PROYECTO-####, utilizando la fecha de creación del proyecto, su nombre y el siguiente correlativo global de proyectos de cuatro cifras. Utilizará la misma secuencia global que los proyectos creados manualmente, independiente de la de presupuestos, sin reutilizar el código ni el correlativo del presupuesto. El código permanecerá asociado al proyecto y no cambiará por modificaciones posteriores de su información general.

Una vez completada correctamente la creación, el sistema deberá dirigir al usuario a la ficha del nuevo proyecto.

La creación del proyecto y la aprobación del presupuesto deberán completarse conjuntamente una vez proporcionados correctamente todos los datos requeridos.

Si el usuario cancela el proceso de creación del proyecto antes de completarlo, no deberá crearse el proyecto ni registrarse definitivamente la aprobación del presupuesto.

Si se produce un error que impida crear correctamente el proyecto, no deberá quedar registrado un proyecto parcial ni registrarse definitivamente la aprobación del presupuesto. El sistema deberá informar al usuario que la operación no pudo completarse y permitir un nuevo intento, sin generar proyectos duplicados ni registros inconsistentes.

Proceso de negocio al que responde: Transición entre presupuestación y ejecución del trabajo.

Dependencias: RF-35; RF-48.

Necesidad asociada: N-01 / N-04.

Prioridad: Alta.

## RF-053 — Crear presupuesto desde un proyecto existente


**Descripción**


El sistema deberá permitir al Operador Responsable iniciar la elaboración de un presupuesto desde la ficha de su Proyecto existente que todavía no disponga de uno asociado. Supervisor y Administrador podrán hacerlo desde cualquier Proyecto sin Presupuesto asociado; un Operador no Responsable no podrá iniciar esa elaboración desde el Proyecto.

Al iniciar el presupuesto desde el proyecto, deberán reutilizarse los datos disponibles del mismo.


**Reglas de negocio**


Todo presupuesto creado desde un proyecto deberá quedar vinculado a dicho proyecto.

Un proyecto podrá tener como máximo un presupuesto asociado.

Si el proyecto ya dispone de un presupuesto asociado, no deberá permitirse crear otro presupuesto para dicho proyecto.

El cliente del presupuesto deberá corresponder al cliente del proyecto.

Deberán trasladarse automáticamente cliente, nombre del proyecto y tipo de proyecto.

La aprobación de un presupuesto creado desde un proyecto existente no deberá crear un nuevo proyecto.

Una vez iniciado, el presupuesto continuará mediante el flujo normal de presupuestación.

El presupuesto deberá permanecer asociado al mismo proyecto independientemente de que sea aprobado, rechazado o pase a Solicitud de modificación.

Proceso de negocio al que responde: Elaboración de presupuestos asociados a proyectos existentes.

Dependencias: RF-01; RF-48.

Necesidad asociada: N-01 / N-04.

Prioridad: Alta.

## RF-054 — Consultar ficha del proyecto


**Descripción**


El sistema deberá permitir a Operador, Supervisor y Administrador acceder a la ficha de un Proyecto según el acceso general definido por el sistema, desde la cual podrán consultar la información y funcionalidades relacionadas con el mismo.

La ficha deberá permitir acceder a la información general del proyecto y a las secciones de Resumen, Presupuesto, Documentos, Facturas de venta, Facturas de compra, Cronograma, Participantes y Resumen económico.


**Reglas de negocio**


La información presentada deberá corresponder al proyecto seleccionado.

La ficha deberá permitir identificar el nombre del proyecto, cliente, estado, tipo de proyecto, responsable, fecha de creación y, cuando exista un envío correctamente registrado, el medio de comunicación utilizado para el envío del presupuesto asociado. Ese medio deberá provenir del presupuesto y no podrá editarse desde la ficha; si no existe presupuesto enviado al cliente, no deberá inventarse un valor. La ficha deberá permitir acceder a una sección Resumen económico con la información económica definida en RF-103. La ficha dispondrá además de secciones independientes de Facturas de venta y Facturas de compra; cada sección mostrará exclusivamente las Facturas del tipo correspondiente actualmente asociadas al Proyecto consultado y, con el permiso correspondiente, permitirá iniciar su registro según RF-77 o RF-104 respectivamente. La consulta y descarga del PDF se regirán por RF-83 y la eliminación lógica por RF-78; no se permitirá editar datos ni reasignar las facturas a otro proyecto. El Resumen económico definido en RF-103 presentará sus cinco indicadores y permitirá gestionar costos adicionales a los usuarios autorizados.

Proceso de negocio al que responde: Gestión y seguimiento centralizado de proyectos.

Dependencias: RF-46; gestión de usuarios y permisos (RF-73).

Necesidad asociada: N-04 / N-05.

Prioridad: Alta.

## RF-055 — Consultar resumen del proyecto


**Descripción**


El sistema deberá permitir visualizar desde la ficha del proyecto un resumen con información general que facilite conocer rápidamente su situación.

El resumen tiene que incluir: cantidad de documentos, Cantidad de Facturas de venta, Cantidad de Facturas de compra, cantidad de participantes, fecha de inicio, fecha estimada de finalización.


**Reglas de negocio**


Las cantidades mostradas deberán calcularse a partir de las asociaciones actuales del Proyecto; las Facturas de venta y las Facturas de compra activas se contarán y mostrarán independientemente, sin un único total combinado. Registrar una Factura activa aumentará la cantidad de su categoría y eliminarla lógicamente la disminuirá. Una Factura guardada no podrá editarse ni reasignarse a otro Proyecto. Los PDF de Facturas no se contarán por separado como documentos genéricos; el contador de documentos solo incluirá los documentos activos del Proyecto, excluyendo los eliminados lógicamente.

Proceso de negocio al que responde: Consulta y seguimiento de proyectos.

Dependencias: RF-54; RF-77; RF-104; gestión documental del Proyecto.

Necesidad asociada: N-04 / N-05.

Prioridad: Alta.

## RF-056 — Gestionar participantes del proyecto


**Descripción**


El sistema deberá permitir por sus permisos base al Operador Responsable de un Proyecto, y a Supervisor y Administrador en cualquier Proyecto, agregar y quitar participantes del mismo desde la ficha del proyecto.

La sección Participantes deberá mostrar los usuarios actualmente asociados al proyecto y permitir su administración según los permisos correspondientes.

Para agregar un participante, el usuario autorizado deberá poder seleccionar entre los usuarios registrados activos en la aplicación.


**Reglas de negocio**


Todo proyecto deberá contar con un responsable.

Todos los roles podrán consultar los participantes. Por sus permisos base, el Operador podrá agregar o quitar participantes solo si es Responsable del Proyecto; Supervisor y Administrador podrán gestionarlos en cualquier Proyecto. Ser solo participante no autoriza al Operador a administrar participantes; no existen excepciones individuales de permisos.

Para incorporar a un usuario como nuevo participante, deberá estar registrado y Activo; los usuarios Inactivos no estarán disponibles para ser seleccionados como nuevos participantes. La inactivación posterior de un participante no eliminará automáticamente su relación existente con el proyecto ni sus relaciones existentes con tareas, de acuerdo con RF-75.

Un proyecto podrá contar con uno o más participantes. Al crearlo, tanto manualmente (RF-48) como desde un presupuesto aprobado (RF-52), el Responsable designado quedará incorporado automáticamente como participante, sin requerir una segunda acción.

Un mismo usuario no deberá incorporarse más de una vez como participante del mismo proyecto, incluida la incorporación automática del Responsable durante la creación o su cambio conforme a RF-50. Al cambiar el Responsable, el anterior dejará de figurar como participante y el nuevo se incorporará si todavía no figura, sin duplicados y sobre el mismo Proyecto; si el anterior tiene Tareas asignadas, el cambio quedará impedido hasta resolverlas mediante RF-63 o RF-64, sin modificar entretanto al Responsable ni a los participantes.

El usuario autorizado no podrá quitar directamente de los participantes al Responsable actual del Proyecto mientras conserve esa responsabilidad: antes deberá cambiarse el Responsable conforme a RF-50. Para los demás participantes previamente agregados, podrá quitarlos solo si no figuran como Responsable de ninguna Tarea registrada en ese mismo Proyecto, independientemente de su Porcentaje de avance, incluso si es 100 %. Si tiene Tareas asignadas, el sistema impedirá quitarlo e informará que primero deberán reasignarse a otros participantes del mismo Proyecto mediante RF-63 o eliminarse conforme al procedimiento y los permisos de RF-64; entretanto, conservará la asociación del participante y las Tareas con sus Responsables sin cambios. Una vez resueltas todas esas asignaciones, podrá quitarse mediante el procedimiento normal. Quitar participantes no autoriza por sí solo a modificar ni eliminar Tareas y no provocará reasignaciones o eliminaciones automáticas.

Quitar un participante del proyecto no deberá eliminar al usuario de la aplicación.

Proceso de negocio al que responde: Organización y seguimiento de proyectos.

Dependencias: RF-48; gestión de usuarios.

Necesidad asociada: N-05.

Prioridad: Alta.

## RF-057 — Acceder a la documentación del presupuesto asociado al proyecto


**Descripción**


Desde la sección Presupuesto de la ficha del proyecto, Operador, Supervisor y Administrador deberán poder consultar el Presupuesto asociado y acceder a su documentación disponible, incluido el PDF final cuando exista y la opción de generar y descargar el Excel a demanda conforme a RF-37 y RF-38, según el acceso general definido por el sistema.

Cuando el proyecto todavía no disponga de un presupuesto asociado, la opción para iniciar su creación se regirá por RF-53.


**Reglas de negocio**


Cada proyecto podrá disponer como máximo de un presupuesto asociado.

Cuando exista un presupuesto asociado, deberá poder accederse al PDF final cuando se encuentre disponible.

El PDF deberá poder previsualizarse y descargarse.

La planilla Excel podrá generarse y descargarse a demanda conforme a RF-37 y RF-38, sin exigir que exista un archivo previamente almacenado ni conservar una copia versionada en el sistema.

La documentación deberá corresponder al presupuesto asociado al proyecto.

Cuando el proyecto no disponga de un presupuesto asociado, la acción para iniciar su creación estará disponible exclusivamente según los permisos y condiciones de RF-53.

Cuando el proyecto ya disponga de un presupuesto asociado, no deberá permitirse iniciar la creación de otro.

Proceso de negocio al que responde: Consulta económica y documental del proyecto.

Dependencias: RF-44; RF-53; RF-37; RF-38.

Necesidad asociada: N-01 / N-04.

Prioridad: Alta.

## RF-058 — Consultar información resumida del presupuesto asociado al proyecto.


**Descripción**


Para Operador, Supervisor y Administrador con acceso al Proyecto, el sistema deberá mostrar desde la sección Presupuesto de la ficha del proyecto la información resumida del presupuesto asociado, cuando exista.


**La información deberá incluir**


- identificador/nombre;

- fecha;

- estado;

- totales en USD y UYU, conservando los importes y la cotización utilizados si el presupuesto fue enviado.


**Reglas de negocio**


La información mostrada deberá corresponder al presupuesto asociado al proyecto.

El estado y los totales en ambas monedas deberán corresponder a la información del presupuesto; si fue enviado, los importes y la cotización conservados no cambiarán por variaciones posteriores de cotización.

Desde dicha información deberá poder accederse al presupuesto correspondiente.

Si el proyecto todavía no dispone de presupuesto asociado, deberá indicarse dicha situación.

Proceso de negocio al que responde: Consulta y seguimiento del proyecto.

Dependencias: RF-57; gestión de presupuestos.

Necesidad asociada: N-01 / N-04.

Prioridad: Alta.

## RF-059 — Subir documentos a un proyecto


**Descripción**


El sistema deberá permitir al usuario autorizado seleccionar obligatoriamente uno de los tipos de documento fijos de la aplicación para posteriormente cargar el documento/archivo al proyecto desde la sección de documentación de su ficha.

Los documentos cargados deberán quedar disponibles para su posterior consulta desde el proyecto correspondiente.


**Reglas de negocio**


Cada documento cargado deberá quedar relacionado con el proyecto desde el cual fue incorporado.

Tipos y formatos de archivo admitidos: PDF, DOC, DOCX, XLS, XLSX, PPT, PPTX, CSV, TXT, JPG, JPEG, PNG, HEIC, TIF/TIFF, DWG y DXF

Todo documento cargado deberá clasificarse mediante la selección obligatoria de un tipo de documento fijo disponible en la aplicación. Los tipos disponibles deberán corresponder al conjunto de opciones fijas predefinidas de la aplicación, sin alta, baja ni administración por parte de los usuarios. El documento quedará asociado al Tipo seleccionado; no podrá asignarse libremente uno inexistente ni crearse uno desde el formulario de carga. PENDIENTE DE VALIDAR CON EOP: lista definitiva de tipos de documento.

Tamaño máximo por archivo: 100 MB por archivo.

Por sus permisos base, el Operador podrá subir documentos cuando sea Responsable o participante del Proyecto. Supervisor y Administrador podrán subirlos a cualquier Proyecto. No podrán concederse permisos individuales para subir documentos a otros Proyectos. Se mantienen las validaciones documentales y de acceso. La consulta documental se regirá por RF-60.

Proceso de negocio al que responde: Gestión documental de proyectos.

Dependencias: RF-54; gestión de tipos de documento fijos; almacenamiento privado de archivos.

Necesidad asociada: N-04 / N-06.

Prioridad: Alta.

## RF-060 — Consultar y filtrar documentos del proyecto


**Descripción**


El sistema deberá permitir a Operador, Supervisor y Administrador con acceso al Proyecto consultar los documentos activos registrados para un proyecto y aplicar criterios de búsqueda o filtrado para facilitar su localización.

La consulta deberá mostrar los documentos asociados al proyecto mediante una tabla con la siguiente información:

Nombre del archivo.

Tipo documental.

Fecha de subida.

Usuario que cargó el documento.

Tamaño del archivo.

Acciones disponibles sobre el documento.

Los documentos deberán poder filtrarse por Tipo de documento y Fecha de subida, y se deberá permitir la búsqueda por Nombre. El filtro Tipo de documento utilizará los tipos de documento fijos de la aplicación y podrá combinarse con Fecha de subida. Al seleccionar un Tipo, se mostrarán únicamente los documentos del proyecto actual asociados a él.

El sistema deberá permitir la descarga segura de todos los formatos admitidos, con vista previa únicamente de PDF, JPG/JPEG y PNG, sin conversión ni visor interno para los demás formatos.


**Reglas de negocio**


El usuario deberá poder descargar los documentos disponibles.

La aplicación de filtros o búsqueda no deberá modificar los documentos ni su información asociada.

Los filtros disponibles para la consulta de documentos serán Tipo de documento y Fecha de subida. El filtro Tipo de documento utilizará los tipos de documento fijos y mostrará únicamente los documentos del proyecto actual asociados al Tipo seleccionado.

El usuario deberá poder buscar documentos mediante su nombre.

El sistema permitirá la vista previa en el navegador únicamente de PDF, JPG/JPEG y PNG. Los demás formatos admitidos podrán descargarse, sin visor ni conversión interna. Los documentos eliminados lógicamente no aparecerán en las consultas activas. Tanto la vista previa como la descarga deberán verificar los permisos del lado del servidor.

Proceso de negocio al que responde: Gestión documental de proyectos.

Dependencias: RF-59.

Necesidad asociada: N-04 / N-06.

Prioridad: Alta.

## RF-061 — Eliminar lógicamente un documento de un proyecto


**Descripción**


El sistema deberá permitir a los usuarios autorizados retirar un documento de las consultas activas de un proyecto mediante eliminación lógica, conservando su registro y su archivo original en almacenamiento privado.


**Reglas de negocio**


La eliminación deberá afectar únicamente al documento seleccionado.

Antes de eliminar el documento, el sistema deberá solicitar al usuario la confirmación de la acción.

Una vez confirmada la eliminación lógica, el documento dejará de aparecer en las consultas activas del proyecto. Su registro y el archivo original deberán conservarse privadamente en Azure Blob Storage; no se realizará eliminación física del archivo.

El Operador no podrá eliminar documentos por su rol. Supervisor y Administrador podrán eliminarlos lógicamente, previa confirmación y verificación del lado del servidor; no se permitirán excepciones individuales.

Proceso de negocio al que responde: Gestión documental de proyectos.

Dependencias: RF-59; RF-60.

Necesidad asociada: N-04 / N-06.

Prioridad: Media

## RF-062 — Registrar tareas del proyecto


**Descripción**


El sistema deberá permitir registrar tareas dentro del cronograma de un proyecto para organizar y realizar el seguimiento de las actividades necesarias para su ejecución.

Para cada tarea deberá poder registrarse: Nombre, Descripción, Responsable, Fecha de inicio, Fecha de fin opcional, Horas estimadas, Horas registradas y Porcentaje de avance. La ausencia de Fecha de fin no impedirá guardar, consultar ni gestionar la tarea.

El Porcentaje de avance deberá representar el nivel actual de ejecución de la tarea mediante un valor comprendido entre 0 % y 100 %.


**Reglas de negocio**


Toda tarea deberá quedar asociada al proyecto correspondiente.

Cada tarea deberá permitir registrar un Porcentaje de avance comprendido entre 0 % y 100 %, ambos inclusive.

El sistema no deberá permitir registrar un Porcentaje de avance inferior a 0 %.

El sistema no deberá permitir registrar un Porcentaje de avance superior a 100 %.

Un valor de 0 % representará una tarea todavía no iniciada.

Un valor de 100 % representará una tarea completamente terminada.

Los valores intermedios representarán distintos niveles parciales de avance de la tarea.

La tarea no deberá utilizar un campo Estado para representar su avance.

La tarea no deberá contar con un campo Prioridad.

Al crear una Tarea serán obligatorios exclusivamente Nombre, Responsable, Fecha de inicio y Horas estimadas; los restantes campos no serán obligatorios al crearla, sin perjuicio de las reglas funcionales vigentes.

El Responsable de la Tarea deberá seleccionarse únicamente entre participantes del Proyecto; seleccionar un Usuario no lo convierte automáticamente en participante.

Por sus permisos base, el Operador podrá registrar Tareas solo cuando sea Responsable del Proyecto; Supervisor y Administrador podrán registrarlas en cualquier Proyecto. El Operador no podrá registrar Tareas en Proyectos de los que no sea Responsable. El Responsable de la Tarea deberá pertenecer a los participantes del Proyecto.

Proceso de negocio al que responde: Planificación y seguimiento de proyectos.

Dependencias: RF-54; gestión de participantes y usuarios.

Necesidad asociada: N-05.

Prioridad: Alta.

## RF-063 — Modificar tareas del proyecto


**Descripción**


El sistema deberá permitir modificar la información registrada para una tarea a medida que avance el proyecto.

La modificación deberá permitir actualizar, cuando corresponda, datos como Responsable, fechas, horas registradas, descripción, Porcentaje de avance y demás información modificable de la tarea.

El Porcentaje de avance deberá poder actualizarse a medida que progrese la ejecución de la tarea.


**Reglas de negocio**


La modificación deberá realizarse sobre la misma tarea y no deberá generar una tarea nueva.

Los cambios realizados deberán reflejar la situación actual de la tarea dentro del proyecto. Podrá mantenerse sin Fecha de fin; cuando el usuario establezca una Fecha de fin válida, pasará a representarse en el Gantt conforme a RF-66, sin que dejarla vacía impida guardar la tarea.

El Porcentaje de avance deberá poder modificarse posteriormente.

Todo nuevo valor de Porcentaje de avance deberá encontrarse comprendido entre 0 % y 100 %, ambos inclusive.

El sistema deberá impedir guardar valores inferiores a 0 % o superiores a 100 %.

La modificación de una tarea no deberá incorporar ni utilizar campos Estado o Prioridad.

No existirán campos inmodificables por el Estado de la Tarea o del Proyecto; la modificación dependerá de los permisos efectivos del usuario.

Por sus permisos base, un Operador participante con una Tarea asignada podrá actualizar solo su descripción, horas registradas y Porcentaje de avance, sin obtener por esa condición permisos de planificación sobre las demás Tareas.

Por sus permisos base, el Operador Responsable del Proyecto podrá registrar Tareas, modificar fechas y la planificación, y asignar o cambiar al Responsable de una Tarea; el nuevo Responsable deberá ser participante del Proyecto.

Supervisor y Administrador podrán modificar cualquier Tarea y su planificación por sus permisos base. No se admitirán permisos individuales para ampliar las acciones o el alcance del Operador; el Responsable de una Tarea continuará siendo participante del Proyecto y el Porcentaje de avance deberá respetar el rango de 0 % a 100 %.

Proceso de negocio al que responde: Seguimiento de tareas y proyectos.

Dependencias: RF-62.

Necesidad asociada: N-05.

Prioridad: Alta.

## RF-064 — Eliminar tarea del proyecto


**Descripción**


El sistema deberá permitir eliminar una tarea registrada dentro del cronograma de un proyecto cuando esta haya sido creada por error o ya no corresponda al proyecto.


**Reglas de negocio**


La eliminación deberá afectar únicamente a la tarea seleccionada.

Antes de eliminar una tarea, el sistema deberá solicitar al usuario la confirmación de la acción.

Una vez confirmada la eliminación, la tarea no deberá continuar formando parte del cronograma del proyecto.

El Operador no podrá eliminar Tareas por su rol y no se admitirán excepciones individuales. Supervisor y Administrador podrán eliminarlas por sus permisos base, siempre previa confirmación.

Proceso de negocio al que responde: Planificación y seguimiento de proyectos.

Dependencias: RF-62.

Necesidad asociada: N-05.

Prioridad: Alta.

## RF-065 — Buscar y filtrar tareas del proyecto


**Descripción**


El sistema deberá permitir buscar y filtrar las tareas registradas dentro del cronograma de un proyecto.

Las tareas deberán poder buscarse por Nombre y filtrarse por Responsable, Fecha de inicio y Fecha de fin.


**Reglas de negocio**


Los filtros deberán aplicarse únicamente sobre las tareas correspondientes al proyecto consultado.

La aplicación de filtros o búsqueda no deberá modificar la información registrada de las tareas.

El usuario deberá poder buscar tareas por Nombre.

El usuario deberá poder filtrar tareas por Responsable.

El usuario deberá poder filtrar tareas por Fecha de inicio.

El usuario deberá poder filtrar tareas por Fecha de fin.

Los criterios disponibles podrán combinarse cuando corresponda.

No se requieren otros criterios de búsqueda o filtrado.

Proceso de negocio al que responde: Planificación y seguimiento de proyectos.

Dependencias: RF-62.

Necesidad asociada: N-05.

Prioridad: Media.

## RF-066 — Visualizar cronograma mediante diagrama de Gantt


**Descripción**


El sistema deberá permitir a Operador, Supervisor y Administrador visualizar en el diagrama de Gantt las Tareas del proyecto que tengan Fecha de inicio y Fecha de fin, y mostrar las restantes sin Fecha de fin en una sección independiente denominada «Tareas sin fecha de finalización», sin barras en el diagrama.

La representación deberá permitir observar gráficamente la distribución temporal de las tareas del proyecto y reflejar su nivel de avance registrado.

El diagrama de Gantt deberá visualizarse dentro de la sección Cronograma de la ficha del proyecto.


**Reglas de negocio**


El diagrama deberá generarse utilizando las tareas y fechas registradas para el proyecto correspondiente.

Una modificación en las fechas de una tarea deberá reflejarse en la visualización actualizada del cronograma; al asignarle una Fecha de fin válida, una tarea de «Tareas sin fecha de finalización» pasará automáticamente al Gantt, y una tarea que quede sin Fecha de fin aparecerá en esa sección sin barra.

El Porcentaje de avance registrado para una tarea deberá reflejarse en la representación correspondiente dentro del cronograma Gantt.

Cuando se modifique el Porcentaje de avance de una tarea, la visualización actualizada del cronograma deberá reflejar el nuevo valor. La vista del Gantt será exclusivamente de consulta; no se permitirá modificar las tareas mediante arrastre, ajuste o edición directa de barras. Las fechas y los porcentajes de avance se modificarán desde las operaciones de tareas (RF-63), y el Gantt no calculará dependencias automáticas entre tareas.

Proceso de negocio al que responde: Planificación y seguimiento de proyectos.

Dependencias: RF-62; RF-63; gestión de tareas del proyecto.

Necesidad asociada: N-05.

Prioridad: Baja.

## RF-067 — Consultar catálogo de EOP


**Descripción**


Por permisos base, Operador, Supervisor y Administrador podrán consultar, buscar y filtrar el catálogo centralizado de materiales de EOP y sus referencias de precios por Proveedor, sin necesidad de ser Responsables de un Presupuesto. Por sus permisos base, el Operador podrá utilizar Materiales existentes al elaborar Presupuestos; Supervisor y Administrador tendrán además las acciones de administración que les correspondan según los RF aplicables. No se admitirán permisos individuales; las acciones se regirán por los permisos fijos y, cuando proceda, restricciones de responsabilidad de RF-73. Desde el catálogo, la acción Editar de un Material solo se mostrará a los usuarios con permiso efectivo de edición conforme a RF-108 y RF-73; el permiso para consultar el catálogo no habilita su modificación.

Para cada relación entre material y proveedor deberá poder consultarse el precio actual, su moneda (USD o UYU) y su fecha de última actualización.

Las referencias de precios se mantienen manualmente mediante RF-21 y RF-108; no se identificarán relaciones con sincronización o actualización automática con Fivisa.


**Reglas de negocio**


Un mismo material podrá encontrarse relacionado con distintos proveedores.

Cada combinación de material y proveedor deberá mantener su propio precio actual y fecha de última actualización.

La información mostrada deberá corresponder a los datos actualmente registrados en el catálogo.

El catálogo permitirá buscar Materiales por Nombre y filtrarlos por Unidad y por Proveedor. Los criterios podrán utilizarse conjuntamente y solo se mostrarán los Materiales que cumplan todos los criterios aplicados.

Las referencias de precio de Fivisa se consultarán y mantendrán manualmente con las mismas reglas aplicables a los demás proveedores; no se mostrará un estado de vinculación externa ni de actualización automática.

Proceso de negocio al que responde: Gestión de materiales y precios de referencia.

Dependencias: Catálogo de materiales; gestión de proveedores.

Necesidad asociada: N-02.

Prioridad: Alta.

## RF-068 — Registrar material manualmente desde el catálogo de EOP


**Descripción**


El sistema deberá permitir al Supervisor o Administrador registrar un Material nuevo exclusivamente desde Catálogo de Materiales, indicando nombre, una unidad de medida del conjunto fijo y su primera relación con un Proveedor ya registrado.

En el alta se seleccionará manualmente el proveedor y se ingresará precio unitario estrictamente mayor que cero y moneda UYU o USD. El Material y su primera relación material-proveedor deberán guardarse conjuntamente.


**Reglas de negocio**


El nombre de Material será obligatorio y único por denominación completa normalizada, ignorando diferencias de mayúsculas, minúsculas y espacios innecesarios.

La Unidad será obligatoria y se seleccionará exclusivamente entre las 14 opciones fijas disponibles (mm, cm, m, km, cm², m², cm³, m³, ml, litro, g, kg, t y unidad). Esta lista de unidades fue validada con EOP; no se podrán crear ni administrar unidades desde este formulario.

Todo material nuevo deberá registrar al menos una primera relación material-proveedor válida en la misma operación de alta.

El proveedor deberá seleccionarse entre los ya registrados; si no existe, se registrará previamente desde el módulo Proveedores por un rol autorizado. No habrá alta embebida de proveedor.

El precio unitario inicial se ingresará manualmente y deberá ser estrictamente mayor que cero; la moneda será UYU o USD.

La fecha de última actualización del precio inicial se registrará automáticamente al guardar.

Un error o una validación fallida no dejará guardado un Material sin su primera relación válida.

El material quedará disponible posteriormente para seleccionarse desde presupuestos; no se podrán dar de alta materiales desde dichos presupuestos.

No se integrará ni actualizará automáticamente el precio con Fivisa, aunque el proveedor elegido sea Fivisa.

Solo Supervisor y Administrador podrán registrar nuevos materiales, según los permisos originales fijos.

Proceso de negocio al que responde: Gestión del catálogo de materiales y precios.

Dependencias: RF-23; RF-67; RF-73; módulo Proveedores.

Necesidad asociada: N-02.

Prioridad: Alta.

## RF-069 — Iniciar sesión en el sistema


**Descripción**


El sistema deberá permitir a los usuarios autorizados iniciar sesión utilizando un servicio externo de gestión de identidad.

La autenticación deberá realizarse mediante Microsoft Entra ID, evitando que la aplicación gestione directamente las contraseñas de los usuarios.

Una vez autenticado el usuario, el sistema deberá identificar al usuario registrado en EOP correspondiente a la identidad autenticada para aplicar los permisos definidos dentro de la aplicación.

Cuando un usuario registrado en EOP acceda por primera vez y todavía no se encuentre vinculado a una identidad de Microsoft Entra ID, el sistema deberá establecer dicha vinculación cuando la dirección de correo corporativo registrada en EOP coincida con la identidad autenticada.

Una vez establecida la vinculación, los accesos posteriores deberán permitir identificar al usuario correspondiente a partir de la identidad externa previamente vinculada.


**Reglas de negocio**


No deberá existir una funcionalidad pública de registro de usuarios.

Solamente los usuarios habilitados para utilizar la aplicación deberán poder acceder al sistema.

La validación de las credenciales deberá ser realizada por Microsoft Entra ID.

La aplicación no deberá almacenar las contraseñas utilizadas por los usuarios para autenticarse.

Una vez autenticado el usuario, el sistema deberá identificar al usuario registrado en EOP correspondiente a la identidad autenticada para aplicar sus permisos.

En el primer acceso, cuando exista un usuario registrado en EOP que todavía no se encuentre vinculado a una identidad de Microsoft Entra ID, deberá establecerse la vinculación cuando el correo corporativo registrado en EOP coincida con el correo de la identidad autenticada.

Una vez establecida la vinculación, los accesos posteriores deberán identificar al usuario de EOP mediante la identidad externa previamente vinculada, sin repetir la vinculación aunque el Correo corporativo almacenado en EOP se actualice conforme a RF-76.

Un usuario que se encuentre en estado Inactivo no deberá poder acceder a las funcionalidades de la aplicación, aunque su identidad haya sido autenticada correctamente por Microsoft Entra ID.

El usuario deberá poder cerrar su sesión.

Si la identidad autenticada no corresponde a un usuario habilitado registrado en EOP, no deberá permitirse el acceso a la aplicación

Proceso de negocio al que responde: Acceso y seguridad del sistema.

Dependencias: Servicio externo de identidad; gestión de usuarios.

Necesidad asociada: N-08

Prioridad: Alta.

## RF-070 — Registrar usuarios desde la administración del sistema


**Descripción**


El sistema deberá permitir a un usuario con rol Administrador registrar usuarios para acceder a la aplicación.

La identidad utilizada para la autenticación deberá existir previamente en Microsoft Entra ID y su creación y administración se realizará fuera de la aplicación EOP.

Durante el alta, el Administrador deberá registrar en EOP Nombre completo y Correo corporativo, y asignar un Rol base: Administrador, Supervisor u Operador. El Estado no se seleccionará manualmente: se asignará automáticamente como Activo.

La vinculación entre el usuario registrado en EOP y la identidad de Microsoft Entra ID se establecerá durante el primer inicio de sesión, cuando el Correo corporativo actualmente registrado en EOP coincida con el de la identidad autenticada.


**Reglas de negocio**


Solamente un usuario con rol Administrador podrá registrar nuevos usuarios.

Los usuarios no podrán registrarse por sí mismos en la aplicación.

Antes de registrar un usuario en EOP deberá existir una identidad válida correspondiente en Microsoft Entra ID.

Durante el alta deberá registrarse la dirección de correo corporativo correspondiente a la identidad de Microsoft Entra ID.

La aplicación EOP no deberá crear ni administrar directamente las identidades de Microsoft Entra ID.

Todo usuario deberá quedar identificado dentro de la aplicación para poder relacionarlo posteriormente con proyectos, tareas y otras funcionalidades.

Todo usuario registrado deberá tener asignado uno de los siguientes roles base: Administrador, Supervisor u Operador.

La aplicación no deberá conocer ni almacenar la contraseña utilizada por el usuario para autenticarse.

La vinculación entre el usuario de EOP y la identidad externa deberá completarse durante el primer acceso cuando exista coincidencia entre las identidades correspondientes.

Todo usuario creado deberá quedar inicialmente en estado Activo.

El Correo corporativo registrado en EOP será obligatorio y único entre Usuarios de EOP para permitir la vinculación inicial con la identidad externa. Antes del alta se verificará que no corresponda a otro Usuario; si ya está registrado, se impedirá el alta y se informará la duplicación.

Los datos del alta son Nombre completo, Correo corporativo y Rol base. El Estado se asigna automáticamente como Activo y no se selecciona durante el alta. No se requieren otros datos personales.

Proceso de negocio al que responde: Administración de usuarios y control de acceso.

Dependencias: RF-69; Microsoft Entra ID; servicio de gestión de identidades.

Necesidad asociada: N-08

Prioridad: Alta.

## RF-071 — Autenticar usuarios mediante múltiples factores


**Descripción**


El acceso al sistema deberá utilizar autenticación multifactor mediante el servicio externo de identidad utilizado por la aplicación.

Además de sus credenciales de acceso, el usuario deberá completar el mecanismo adicional de autenticación configurado para su cuenta.


**Reglas de negocio**


La autenticación multifactor deberá ser administrada por el servicio externo de identidad.

La aplicación no deberá implementar ni almacenar directamente los códigos o secretos utilizados para el segundo factor de autenticación.

El usuario deberá completar correctamente el proceso de autenticación requerido antes de acceder a las funcionalidades protegidas del sistema.

Proceso de negocio al que responde: Seguridad y control de acceso al sistema.

Dependencias: RF-69; servicio externo de identidad.

Necesidad asociada: N-08.

Prioridad: Alta.

## RF-072 — Gestionar roles fijos de los usuarios


**Descripción**


El sistema deberá permitir al Administrador asignar o modificar el rol de un usuario, seleccionando exclusivamente entre Administrador, Supervisor y Operador. El rol asignado determinará sus permisos conforme a RF-73.

Los únicos roles disponibles serán Administrador, Supervisor y Operador; no se podrán crear, eliminar ni configurar otros roles.

Al registrar un usuario se le asignará uno de estos roles; posteriormente el Administrador podrá modificar el rol asignado, respetando la protección del último Administrador activo.

No se concederán, denegarán ni administrarán permisos o excepciones individuales. La autorización dependerá del rol y, cuando corresponda, de la responsabilidad o participación del usuario en el proyecto o presupuesto.


**Reglas de negocio**


Todo usuario habilitado deberá tener asignado exactamente uno de los tres roles fijos.

Los roles serán Administrador, Supervisor y Operador; no se permitirá crear roles adicionales.

Durante el registro de un usuario deberá asignarse uno de los roles fijos.

Solamente el Administrador podrá asignar o modificar roles de usuarios.

Solamente el Administrador podrá acceder a la administración de usuarios.

Los permisos se determinarán por el rol asignado y por las restricciones de responsabilidad o participación aplicables a cada acción, conforme a RF-73.

No se podrán configurar permisos, concesiones, denegaciones ni excepciones individuales para ningún usuario.

El cambio de rol afectará los permisos del usuario cuyo rol se modifica, sin alterar los roles o autorizaciones de los demás usuarios.

El rol Administrador dispondrá de las autorizaciones correspondientes a la administración del sistema, siempre sujeto a las reglas de negocio, validaciones y restricciones de integridad de las operaciones.

No podrá modificarse el rol del último Administrador activo si con ello no queda ningún Administrador activo.
La matriz de permisos fijos y restricciones por responsabilidad o participación se define en RF-73.

Proceso de negocio al que responde: Administración de usuarios y control de acceso.

Dependencias: RF-70; gestión de usuarios.

Necesidad asociada: N-08.

Prioridad: Alta.

## RF-073 — Aplicar permisos por rol y restricciones de acceso


**Descripción**


El sistema deberá autorizar o denegar acciones utilizando exclusivamente el rol fijo del usuario (Administrador, Supervisor u Operador) y las restricciones por responsabilidad o participación aplicables a la operación.

Cada rol tendrá permisos predeterminados, sin posibilidad de agregar permisos individuales ni de crear o modificar roles desde una pantalla de administración.

Para determinadas operaciones, además del rol, se exigirá que el usuario sea Responsable del presupuesto o proyecto, participante del proyecto o Responsable de una tarea, conforme a la matriz y las reglas de negocio.

Autorización efectiva = permisos del rol fijo + cumplimiento de las restricciones de responsabilidad o participación aplicables. No existen excepciones individuales.

El Administrador tendrá acceso a las operaciones incluidas en el alcance desde la perspectiva de autorizaciones, sin quedar eximido de reglas de negocio, validaciones ni restricciones de integridad.

No se implementarán pantallas ni mecanismos para conceder, denegar o personalizar permisos por usuario.


**Reglas de negocio**


La autenticación no implicará autorización automática para realizar cualquier operación.

Las autorizaciones de cada usuario se determinarán exclusivamente por su rol fijo y las condiciones de responsabilidad o participación que correspondan.

Los roles fijos serán Administrador, Supervisor y Operador.

El Administrador tendrá las autorizaciones de administración definidas por el sistema, siempre sujeto a las reglas funcionales de cada acción.

El sistema no admitirá permisos individuales que agreguen, denieguen o amplíen acciones o alcances definidos para un rol.

Solamente el Administrador podrá asignar o modificar el rol fijo de un usuario, conforme a RF-72.

No podrán crearse, eliminarse ni personalizarse roles o matrices de permisos mediante la aplicación.

Ser Responsable o participante no concederá acciones ajenas a las autorizadas por el rol.

La autorización deberá comprobarse en cada operación, incluidas las solicitudes directas al servidor.

La interfaz podrá mostrar solo las opciones autorizadas, pero ocultarlas no sustituirá la verificación del servidor.

Las operaciones deberán respetar las reglas de negocio y validaciones aunque el usuario esté autorizado por su rol.

La asignación o modificación de roles no alterará el historial de los registros vinculados al usuario.

La autorización no dependerá de excepciones guardadas por usuario; solo del rol asignado y del contexto aplicable a la operación.

Las restricciones de acceso deberán verificarse también del lado del servidor, con independencia de los elementos que muestre la interfaz.

Un usuario sin autorización deberá recibir una denegación de la operación aunque intente acceder directamente mediante URL o solicitud HTTP.

Las condiciones de Responsable o participante únicamente limitarán el alcance de las acciones ya autorizadas por el rol; no permitirán ampliar permisos del rol ni omitir las restricciones de los datos.

La matriz fija de permisos por módulo se detalla a continuación y se aplicará solamente a las funcionalidades incluidas, sin excepciones individuales.

Matriz de permisos por rol para las funcionalidades incluidas en la primera versión (siempre sujetas a las reglas de negocio de cada operación):

Presupuestos. Operador: consultar, buscar, filtrar y crear presupuestos, y generar/descargar el Excel de los presupuestos que puede consultar; cuando sea Responsable, continuar y editar los editables, registrar o modificar relevamientos de texto/audio, transcribir y analizar con IA, revisar conceptos, seleccionar materiales y referencias existentes del catálogo, agregar o modificar conceptos, calcular importes, confirmar el resumen, cargar PDF comercial y solicitar revisión interna. Supervisor: realizar estas acciones sobre presupuestos autorizados, confirmar revisión, registrar el envío externo de la propuesta, registrar respuestas del cliente, reabrir vencidos y cancelar presupuestos. Administrador: las acciones incluidas con las mismas validaciones de estado, vigencia y coherencia económica. Ningún rol utilizará generación Word mediante IA ni enviará automáticamente mensajes o correos desde la aplicación.

Proyectos. Todos los roles podrán consultar listados, fichas, documentos, tareas, presupuesto vinculado y resumen económico conforme a las reglas de acceso. Operador: modificar datos y estado de los proyectos de los que sea Responsable, gestionar participantes e iniciar un Presupuesto desde el proyecto cuando sea Responsable (RF-53), sin cambiar al Responsable del proyecto. Supervisor: crear y administrar proyectos, cambiar Responsable, gestionar participantes y realizar las acciones incluidas en proyectos autorizados. Administrador: realizar las acciones administrativas incluidas. Los estados de proyecto serán fijos y se cambiarán manualmente, sin motor obligatorio de transiciones. La vinculación entre presupuesto y proyecto será como máximo uno a uno.

Documentos del proyecto. Operador: consultar, buscar, filtrar, previsualizar PDF/JPG/JPEG/PNG y descargar documentos autorizados, y subirlos cuando sea Responsable o participante. Supervisor y Administrador: consultar, subir y eliminar lógicamente documentos autorizados. Los demás formatos permitidos solo se descargarán; los archivos privados no serán públicos.

Tareas y Gantt. Todos: consultar tareas y visualizar el Gantt. Operador participante: actualizar los datos permitidos de sus tareas asignadas; si es Responsable del proyecto, crear tareas y gestionar su planificación y responsables según RF-62 y RF-63. Supervisor y Administrador: crear, editar y eliminar tareas autorizadas. El Gantt será de solo visualización; las fechas y avances se modificarán mediante las operaciones de tareas.

Materiales y precios. Todos: consultar materiales, proveedores de referencia y precios existentes; seleccionar materiales y referencias registrados durante la presupuestación. Supervisor y Administrador: registrar y editar materiales, relaciones material-proveedor y precios exclusivamente desde el catálogo, según RF-68 y RF-108. Operador: no podrá registrar ni editar materiales o relaciones de catálogo por su rol. No se aplicarán cambios al catálogo al confirmar un presupuesto, ni habrá sincronización con Fivisa.

Proveedores. Todos: consultar y seleccionar proveedores existentes. Supervisor y Administrador: registrar proveedores, agregar cuentas bancarias y editar los datos y cuentas existentes según RF-23, RF-102 y RF-109. Operador: sin facultades de alta o edición de proveedores por su rol; registrar una factura no otorga permisos sobre proveedores. No se permitirá el alta de proveedores desde formularios de otros módulos.

Clientes. Todos: consultar, buscar, filtrar y registrar clientes, según las reglas de validación. Supervisor y Administrador: modificar datos y activar o inactivar clientes. Operador: sin facultad de modificación ni cambio de estado de clientes por su rol.

Facturas de venta y compra. Operador: consultar, previsualizar o descargar PDF originales desde los proyectos autorizados; sin permisos de registrar o eliminar facturas por su rol. Supervisor y Administrador: registrar facturas manualmente y eliminarlas lógicamente, conforme a RF-77, RF-78 y RF-104. No existirán edición de datos ni reasignación de facturas. Cada factura activa pertenece a un único proyecto y tendrá PDF original privado obligatorio. No habrá OCR ni extracción asistida.

Resumen económico. Todos: consultar los cinco indicadores y desgloses de los proyectos autorizados. Operador: no podrá agregar, modificar ni eliminar costos adicionales por su rol. Supervisor y Administrador: gestionar costos adicionales conforme a RF-103. Los indicadores se calcularán automáticamente; no podrán modificarse manualmente.

Estadísticas. Supervisor y Administrador podrán consultar las estadísticas de rechazo de presupuestos incluidas en el alcance; el Operador no tendrá esta autorización por su rol. No se incorporarán dashboards adicionales.

Catálogos fijos. Tipos de proyecto, tipos de documento y unidades de medida serán opciones predefinidas y seleccionables en las operaciones autorizadas. Ningún rol dispondrá de acciones de alta, baja ni administración de estos catálogos.

Usuarios. Todos los roles podrán iniciar y cerrar sesión. Exclusivamente el Administrador podrá registrar usuarios, consultar el listado administrativo, modificar datos, cambiar estado Activo/Inactivo y asignar uno de los tres roles fijos. No habrá administración de permisos individuales ni creación de roles.

Reglas transversales. Responsabilidad o participación no sustituye permisos de rol; la autorización tampoco sustituye confirmaciones, reglas de vencimiento, restricciones de cardinalidad, validaciones de importes, controles de duplicados, reglas de PDF privado ni las demás reglas de negocio. Las operaciones no autorizadas deberán rechazarse del lado del servidor.

Proceso de negocio al que responde: Seguridad y control de acceso al sistema.

Dependencias: RF-69; RF-72; RF-70; reglas de responsabilidad y participación de presupuestos y proyectos.

Necesidad asociada: N-08

Prioridad: Alta.

## RF-074 — Consultar listado de usuarios


**Descripción**


El sistema deberá permitir únicamente al Administrador consultar un listado sencillo de los usuarios registrados en la aplicación.

La consulta mostrará Nombre completo, Correo corporativo, Rol y Estado Activo/Inactivo, sin incorporar buscador ni filtros.


**Reglas de negocio**


Solo el Administrador podrá acceder al listado administrativo de usuarios.

El listado deberá mostrar Nombre completo, Correo corporativo, Rol asignado y Estado Activo/Inactivo.

El listado no incluirá búsqueda por nombre ni por ningún otro campo.

El listado no incluirá filtros por rol, estado ni otros criterios.

Desde el listado se podrá acceder a las acciones administrativas autorizadas de modificación de datos, cambio de rol y cambio de estado, conforme a RF-72, RF-75 y RF-76.

La consulta de usuarios no modificará la información registrada.

Se mostrarán los usuarios registrados sin aplicar filtros de búsqueda o selección por criterios.

Los valores de rol mostrados serán Administrador, Supervisor u Operador.

La modificación del rol se hará mediante RF-72; la modificación de datos mediante RF-76 y el cambio de estado mediante RF-75. No se administrarán permisos individuales.

Proceso de negocio al que responde: Administración de usuarios y control de acceso.

Dependencias: RF-70; RF-72; RF-73.

Necesidad asociada: N-08

Prioridad: Alta.

## RF-075 — Gestionar estado Activo/Inactivo de los usuarios


**Descripción**


El sistema deberá permitir a un usuario con rol Administrador modificar el estado de un usuario entre Activo e Inactivo.

Los usuarios en estado Inactivo deberán conservarse registrados en el sistema, pero no podrán acceder a sus funcionalidades.


**Reglas de negocio**


Todo usuario creado deberá encontrarse inicialmente en estado Activo.

Solamente los usuarios con rol Administrador podrán modificar el estado Activo/Inactivo de otro usuario.

Un usuario en estado Inactivo no deberá poder acceder a las funcionalidades de la aplicación.

El cambio a estado Inactivo no deberá eliminar al usuario ni la información histórica relacionada con él.

Las relaciones existentes del usuario con proyectos, tareas u otros registros deberán conservarse cuando pase a estado Inactivo.

Un usuario Inactivo podrá volver posteriormente al estado Activo.

Antes de cambiar un usuario de Activo a Inactivo, el sistema deberá solicitar confirmación.

Antes de cambiar un usuario a estado Inactivo, el sistema deberá verificar si se encuentra asignado como responsable de algún proyecto. En ese caso, no deberá permitirse su inactivación hasta que dichos proyectos sean reasignados a otro usuario activo.

La inactivación de un usuario no deberá eliminar ni modificar automáticamente las tareas que tenga asignadas. Estas deberán conservarse y podrán posteriormente ser reasignadas mediante el proceso de modificación de tareas o eliminadas cuando corresponda.

El sistema no deberá permitir cambiar a estado Inactivo al último usuario Administrador activo de la aplicación.

Proceso de negocio al que responde: Administración de usuarios y control de acceso.

Dependencias: RF-69; RF-70; RF-74.

Necesidad asociada: N-08

Prioridad: Alta.

## RF-076 — Modificar datos de un usuario


**Descripción**


El sistema deberá permitir a un usuario con rol Administrador modificar los datos registrados de un usuario existente cuando sea necesario corregir o actualizar su información.

La modificación deberá poder realizarse desde la sección de administración de usuarios, sobre el mismo usuario registrado.

El nombre completo del usuario podrá modificarse desde la aplicación EOP.

El Administrador podrá actualizar manualmente el Correo corporativo almacenado en EOP para reflejar un cambio realizado previamente en Microsoft Entra ID. EOP no modificará el correo ni la identidad almacenados en Microsoft Entra ID.

La modificación del rol del usuario deberá realizarse de acuerdo con las reglas definidas en RF-72.

La modificación del estado Activo/Inactivo deberá realizarse de acuerdo con las reglas definidas en RF-75.

Un cambio de Correo corporativo se efectuará primero en Microsoft Entra ID; después, el Administrador podrá actualizar manualmente el valor almacenado en EOP en el mismo Usuario, sin modificar la identidad externa ni crear un Usuario nuevo.


**Reglas de negocio**


Solamente el Administrador podrá modificar datos de Usuarios; esta administración no podrá delegarse a Supervisor ni Operador.

La modificación deberá realizarse sobre el mismo usuario y no deberá generar un nuevo registro.

Los cambios realizados no deberán eliminar ni alterar las relaciones existentes del Usuario con Proyectos, Tareas u otros registros, ni su Rol o Estado cuando se actualice únicamente el Correo corporativo. Si la identidad externa ya estaba vinculada, la actualización del Correo en EOP conservará esa vinculación sin exigir una nueva.

El nombre completo del usuario podrá modificarse desde EOP

Solo el Administrador podrá actualizar en EOP el Correo corporativo para reflejar un cambio previo en Microsoft Entra ID; EOP solo modificará su valor interno y no alterará la identidad ni el correo externos. El nuevo Correo corporativo deberá ser único entre Usuarios de EOP; si ya está registrado para otro Usuario, se impedirá la modificación y se informará la duplicación. Si el Usuario ya estaba vinculado, conservará esa vinculación sin repetirla; si aún no accedió, el Correo corporativo vigente en EOP se usará para verificar la coincidencia en el primer acceso.

La modificación del rol deberá realizarse de acuerdo con RF-72.

La modificación del estado Activo/Inactivo deberá realizarse de acuerdo con RF-75.

La aplicación no deberá modificar directamente la identidad externa administrada mediante Microsoft Entra ID.

Los datos obligatorios del alta del Usuario son únicamente Nombre completo, Correo corporativo y Rol base; el Estado inicial se asigna automáticamente como Activo, sin selección manual.

Proceso de negocio al que responde: Administración de usuarios.

Dependencias: RF-70; RF-72; RF-74; RF-75.

Necesidad asociada: N-08

Prioridad: Alta.

## RF-077 — Registrar facturas de venta mediante carga manual y PDF original


**Descripción**


El sistema deberá permitir a Supervisor o Administrador registrar manualmente una Factura de venta desde la sección Facturas de venta de la ficha de un Proyecto existente. El usuario seleccionará y confirmará el tipo de comprobante, adjuntará el PDF original y completará los campos requeridos antes de Guardar factura.

El sistema no realizará OCR, extracción automática ni precarga mediante Azure AI Document Intelligence. La Factura quedará asociada exclusivamente al Proyecto de origen y contribuirá a los ingresos facturados según RF-103.


**Reglas de negocio**


La operación se iniciará en el Proyecto mediante Registrar factura de venta. Solo Supervisor y Administrador podrán ejecutarla según RF-73; Operador tendrá únicamente permiso de consulta. El Proyecto de origen se obtendrá automáticamente, sin selector para cambiarlo.

El Tipo de comprobante deberá elegirse exclusivamente entre estos seis valores fijos: e-Factura (DGI 111), e-Ticket (101), Nota de crédito de e-Factura (112), Nota de crédito de e-Ticket (102), Nota de débito de e-Factura (113) y Nota de débito de e-Ticket (103). No se permitirán altas, modificaciones ni bajas de tipos. El usuario deberá confirmar explícitamente que el tipo elegido corresponde al PDF antes de continuar.

El PDF original será obligatorio y válido y se adjuntará mediante selección de archivo o arrastrar y soltar. No se guardará ninguna Factura sin su PDF original.

El formulario mostrará como datos automáticos y de solo lectura Proyecto, Cliente del Proyecto, Emisor EOP, RUT emisor normalizado de EOP y RUT receptor normalizado del Cliente del Proyecto. No permitirá cambiar Cliente ni alguno de esos RUT desde el registro.

El usuario ingresará manualmente Fecha de emisión, Serie, Número real del comprobante, Importe registrado y Moneda. Estos datos serán obligatorios y se verificarán del lado del servidor. No se analizará el PDF mediante OCR ni se propondrán datos extraídos.

El Importe será positivo y representará el total final del comprobante, incluidos los impuestos y redondeos que correspondan. No se registrarán por separado subtotales, pagos, adelantos, líneas o saldos. Las Notas de crédito conservarán un importe original positivo.

La Moneda admitirá únicamente UYU y USD; se conservarán el Importe y la Moneda originales. Los valores convertidos no los sustituirán.

Antes de Guardar factura se comprobará el duplicado exclusivamente contra Facturas de venta activas de su misma categoría, utilizando RUT emisor normalizado de EOP + Tipo de comprobante + Serie + Número real. Un comprobante eliminado lógicamente no bloqueará una nueva alta. FV-#### es un código interno y no sustituye el Número real.

Cuando una Factura requiera conversión USD/UYU para el cálculo económico, se usará la cotización histórica válida de datosUruguay API correspondiente a su Fecha de emisión, o la última publicación válida anterior cuando ese día no tenga cotización; una falla temporal del servicio no se interpretará como ausencia de publicación. El valor y la fecha efectivamente aplicados quedarán guardados en la Factura. Si no existe una cotización válida almacenada o disponible a través del servicio, se impedirá Guardar factura y se informará el motivo; no se registrarán conversiones pendientes.

El guardado válido almacenará los datos estructurados y la referencia al PDF original en Azure Blob Storage privado; el archivo no se guardará íntegro en SQL Server ni quedará públicamente disponible por conocer su URL. Su carga no lo convertirá automáticamente en documento genérico del Proyecto.

Al confirmar, se guardará como dato histórico propio de la Factura el RUT receptor normalizado vigente del Cliente en ese momento. Modificaciones posteriores del Cliente no alterarán ese valor histórico ni la Factura registrada.

La Factura nueva recibirá un identificador interno global único FV-####, independiente de la serie de compras FC-####: FV-0001, hasta FV-9999 y después FV-10000, sin reiniciarlo por año, Proyecto o Cliente ni reutilizar códigos anteriores.

Cada Factura activa estará asociada exactamente a un Proyecto. Una vez guardada, sus datos, su PDF y su Proyecto no podrán editarse ni reasignarse; solo será posible su eliminación lógica según RF-78.

Las ventas activas contribuyen únicamente a Ingresos facturados: e-Facturas, e-Tickets y Notas de débito suman, mientras que las Notas de crédito restan una sola vez, de acuerdo con RF-103; no se incluirán en indicadores de costos.

Si el usuario cancela, falla una validación o falta la cotización necesaria, no se creará una Factura definitiva incompleta ni una conversión pendiente.

Proceso de negocio al que responde: Gestión administrativa y centralización de facturas de venta.

Dependencias: RF-54; RF-73; Cliente vinculado al Proyecto; datosUruguay API cuando se requiera conversión; Azure Blob Storage privado.

Necesidad asociada: N-04

Prioridad: Alta.

## RF-078 — Eliminar lógicamente una factura de compra o venta


**Descripción**


El sistema deberá permitir a Supervisor o Administrador eliminar lógicamente una Factura de compra o venta existente, previa confirmación explícita, conservando sus datos históricos y su PDF original almacenado de forma privada.

No se permitirá modificar campos, cambiar el Cliente o Proveedor de la Factura, reemplazar su PDF ni reasignarla a otro Proyecto después del guardado.


**Reglas de negocio**


Solo Supervisor y Administrador podrán eliminar Facturas; Operador tendrá permiso de consulta, pero no de eliminación, según RF-73. Toda solicitud se autorizará del lado del servidor.

Una Factura ya registrada será inmutable: no se habilitarán formularios para editar Tipo de comprobante, Fecha de emisión, Serie, Número real, Importe, Moneda, RUT históricos, Cliente, Proveedor, Proyecto o PDF original.

La acción Eliminar deberá identificar inequívocamente la Factura y solicitar confirmación. Cancelar conservará el registro y PDF sin cambios.

Al confirmar correctamente, se marcará la Factura como eliminada lógicamente. No aparecerá en listados y consultas activos, ni en contadores o indicadores económicos del Proyecto; su registro, identificador interno, asociación, RUT histórico y PDF original se conservarán privadamente.

La eliminación lógica no eliminará al Proyecto, Cliente ni Proveedor asociado ni el archivo original en Azure Blob Storage. No se implementará restauración de Facturas eliminadas.

Los duplicados de RF-77 y RF-104 se validarán solo entre Facturas activas. Un comprobante eliminado lógicamente podrá cargarse nuevamente con su PDF y obtendrá un código interno nuevo, sin restaurar ni reutilizar el registro anterior.

Los identificadores FV-#### y FC-#### de las Facturas eliminadas no se reutilizarán. Eliminar una compra actualizará Costo según facturas de compra y Costo real total; eliminar una venta actualizará Ingresos facturados, de acuerdo con RF-103.

No se informará la eliminación como completada si la Factura permanece activa o computada económicamente. Se preservará la consistencia entre la base de datos, los listados y la referencia privada al PDF.

Proceso de negocio al que responde: Gestión administrativa y depuración lógica de facturas.

Dependencias: RF-77; RF-104; gestión de roles y permisos (RF-73); almacenamiento documental privado.

Necesidad asociada: N-04.

Prioridad: Alta.

## RF-079 — Consultar facturas de venta


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-050, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

La sección general Facturas dispondrá de un apartado exclusivo de Facturas de venta registradas mediante RF-77, independiente de Facturas de compra, con listado, búsqueda y filtros propios. Operador, Supervisor y Administrador podrán consultarlas conforme al acceso efectivo de RF-73.


**Reglas de negocio**


El listado mostrará exclusivamente Facturas de venta activas, cada una asociada a exactamente un Proyecto; no se presentará un Estado de asociación.

Cada entrada mostrará como mínimo identificador interno FV-####, Tipo de comprobante, Serie, Número real del comprobante, Fecha de emisión, Cliente, Importe registrado, Moneda y Proyecto asociado.

Desde el listado se podrá consultar la información de la Factura y acceder a su PDF original mediante RF-83, sujeto a los permisos efectivos.

Consultar no modificará datos, PDF ni asociación con el Proyecto; las Facturas de venta y de compra permanecerán en apartados y listados separados.

Proceso de negocio al que responde: Consulta y gestión administrativa de facturas.

Dependencias: RF-77; RF-83; gestión de usuarios y permisos (RF-73).

Necesidad asociada: N-04.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-080 — Buscar y filtrar facturas de venta


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-050, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

Operador, Supervisor y Administrador podrán buscar y filtrar las Facturas de venta del apartado correspondiente según su acceso efectivo conforme a RF-73. La búsqueda admitirá el identificador interno FV-#### y el Número real del comprobante; los filtros serán Fecha, Cliente y Proyecto asociado.


**Reglas de negocio**


Los filtros podrán combinarse entre sí y con la búsqueda por identificador interno o Número real del comprobante.

La búsqueda y los filtros no modificarán la Factura, su PDF ni su asociación con el Proyecto.

Los criterios del apartado Facturas de venta permanecerán independientes de los del apartado Facturas de compra; no se incluirá filtro de Estado de asociación ni otros filtros adicionales.

Proceso de negocio al que responde: Consulta y gestión administrativa de facturas.

Dependencias: RF-79; gestión de usuarios y permisos (RF-73).

Necesidad asociada: N-04.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-081 — Reasignar una factura de venta a otro proyecto


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-051, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema permitirá a un usuario autorizado ejecutar Reasignar sobre una Factura de venta existente y asociarla a otro Proyecto del mismo Cliente. La asociación inicial se realiza automáticamente durante RF-77; esta operación no crea otra Factura.


**Reglas de negocio**


Supervisor y Administrador podrán reasignar por sus permisos base; Operador necesitará el permiso individual específico correspondiente conforme a RF-73.

Se permitirá buscar por Nombre y seleccionar un Proyecto de destino registrado únicamente si pertenece al mismo Cliente de la Factura de venta. No se admitirá un Proyecto de otro Cliente; el Cliente de la Factura continuará siendo el que comparten los Proyectos de origen y destino.

Podrán elegirse Proyectos en los Estados Pendiente de inicio, En ejecución, En pausa, Finalizado y Cancelado. El Estado no constituirá por sí mismo una restricción adicional.

La reasignación exigirá confirmación explícita y, al completarse, la Factura dejará de pertenecer al Proyecto anterior y quedará asociada exclusivamente al de destino. Toda Factura de venta activa seguirá asociada a exactamente un Proyecto; no se ofrecerá una acción independiente Desvincular.

Se conservarán identificador FV-####, Tipo de comprobante, Serie, Número real, Fecha de emisión, Importe registrado, Moneda, Cliente, RUT receptor histórico y PDF original; no se volverá a cargar el PDF ni se actualizará por la reasignación el RUT receptor histórico con el RUT actual del Cliente; tampoco se modificarán los demás datos de la Factura ni se creará otra Factura.

La Factura dejará de mostrarse en el listado del Proyecto anterior y figurará en el del nuevo; se recalcularán los Ingresos facturados de ambos Proyectos conforme a RF-103.

Si el usuario cancela o la operación falla, la Factura permanecerá íntegramente asociada al Proyecto original, sin cambios parciales.

Proceso de negocio al que responde: Organización de la información administrativa de los proyectos.

Dependencias: RF-77; RF-79; gestión de Proyectos y Clientes; gestión de usuarios y permisos (RF-73).

Necesidad asociada: N-04.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-082 — Consultar facturas de venta y compra asociadas a un proyecto


**Descripción**


La ficha de cada Proyecto contará con secciones separadas Facturas de venta y Facturas de compra, que permitirán consultar únicamente los comprobantes activos pertenecientes al Proyecto y acceder a sus PDF originales bajo control de acceso.


**Reglas de negocio**


Cada sección mostrará exclusivamente Facturas activas de su categoría asociadas al Proyecto consultado; ninguna Factura activa podrá pertenecer a más de un Proyecto.

Los datos visibles incluirán identificador interno FV-#### o FC-####, Tipo, Serie, Número real, Fecha de emisión, Cliente o Proveedor según la categoría, Importe, Moneda y datos pertinentes del Proyecto.

Desde Facturas de venta podrá iniciarse el alta manual conforme a RF-77 y desde Facturas de compra la de RF-104, solamente si el usuario tiene autorización de registro.

Para una Factura activa existente se podrá consultar su detalle y visualizar o descargar su PDF original mediante RF-83; Supervisor y Administrador podrán solicitar su eliminación lógica mediante RF-78. No existirán acciones de edición de datos ni de reasignación.

Las Facturas eliminadas lógicamente quedarán fuera de las consultas activas. La consulta no modificará la información, el PDF ni la relación con el Proyecto.

Las Facturas de compra activas afectarán a los costos y las de venta activas a Ingresos facturados, sin mezclarse, conforme a RF-103.

Proceso de negocio al que responde: Consulta administrativa y seguimiento de proyectos.

Dependencias: RF-54; RF-77; RF-78; RF-83; RF-104; RF-103; gestión de autorizaciones (RF-73).

Necesidad asociada: N-04.

Prioridad: Alta.

## RF-083 — Consultar y descargar el PDF original de una factura de compra o venta


**Descripción**


El sistema deberá permitir a un usuario autorizado consultar y descargar de manera segura el PDF original de una Factura activa de compra o venta desde las secciones correspondientes del Proyecto.


**Reglas de negocio**


Cada Factura correctamente registrada deberá conservar un PDF original obligatorio; el archivo solicitado será el asociado a esa Factura.

La consulta y descarga se realizarán desde el Proyecto al que pertenece la Factura, sin listados generales de Facturas y conforme a los permisos de RF-73.

Los PDF originales permanecerán privados en Azure Blob Storage. La aplicación comprobará la autorización del lado del servidor; conocer una URL de almacenamiento no dará acceso público.

Consultar o descargar no modificará la Factura, su asociación ni el archivo original. Los PDF de facturas no se incorporarán automáticamente a los documentos genéricos del Proyecto (RF-59).

La eliminación lógica conservará el PDF original, pero lo retirará de las consultas habituales de Facturas activas, conforme a RF-78.

Proceso de negocio al que responde: Consulta de documentación administrativa de proyectos.

Dependencias: RF-77; RF-78; RF-104; RF-73; Azure Blob Storage privado.

Necesidad asociada: N-04.

Prioridad: Alta.

## RF-084 — Consultar clientes registrados


**Descripción**


Por permisos base, Operador, Supervisor y Administrador podrán consultar los Clientes registrados en la aplicación y acceder a su información, conforme exclusivamente a los permisos fijos de RF-73, sin excepciones individuales.

La información consultable del Cliente deberá permitir identificar al menos nombre, RUT, Correo de contacto y Correo de facturación cuando estén informados, estado Activo/Inactivo y número de WhatsApp. Si alguno de los Correos no está informado, no deberá inventarse un valor.


**Reglas de negocio**


Los clientes registrados deberán conservarse para mantener las relaciones históricas con presupuestos, proyectos y facturas.

Desde el listado deberá poder accederse a las acciones disponibles para cada cliente según los permisos correspondientes.

Proceso de negocio al que responde: Gestión de clientes.

Dependencias: RF-89.

Necesidad asociada: N-04.

Prioridad: Alta.

## RF-085 — Buscar y filtrar clientes


**Descripción**


Por permisos base, Operador, Supervisor y Administrador podrán buscar y filtrar Clientes registrados, conforme exclusivamente a los permisos fijos de RF-73, sin excepciones individuales para facilitar su localización.

El usuario deberá poder buscar clientes por nombre y RUT y filtrarlos según su estado Activo/Inactivo.


**Reglas de negocio**


Los criterios de búsqueda y filtrado podrán utilizarse conjuntamente.

La búsqueda o aplicación de filtros no deberá modificar la información registrada de los clientes.

Proceso de negocio al que responde: Gestión de clientes.

Dependencias: RF-84.

Necesidad asociada: N-04.

Prioridad: Alta.

## RF-086 — Modificar información de un cliente


**Descripción**


Por permisos fijos, Supervisor y Administrador podrán modificar la información registrada de un Cliente; el Operador no podrá modificarla, según RF-73.

El usuario autorizado deberá poder modificar el nombre, RUT, Correo de contacto, Correo de facturación y número de WhatsApp registrados para un cliente.


**Reglas de negocio**


La modificación deberá realizarse sobre el mismo cliente y no deberá generar un nuevo registro.

Las modificaciones realizadas no deberán eliminar las relaciones existentes del cliente con presupuestos, proyectos, facturas u otros registros. Modificar el Cliente, incluido su RUT, no creará ni eliminará Facturas, generará asociaciones automáticas, cambiará el Proyecto de una Factura existente ni alterará los PDF originales.

El RUT del cliente podrá modificarse cuando sea necesario corregir o actualizar la información registrada en su ficha. Este cambio no actualizará automáticamente el RUT receptor histórico conservado en las Facturas de venta anteriores; las nuevas Facturas de venta utilizarán el RUT actualizado del Cliente al registrarse.

El sistema no deberá permitir que dos clientes diferentes se encuentren registrados con el mismo RUT normalizado. El RUT se almacenará exclusivamente con dígitos, sin puntos, espacios ni guiones; se validará su estructura de RUT uruguayo y su dígito verificador. Los separadores habituales ingresados podrán eliminarse antes de validar y guardar. La unicidad se comprobará sobre el valor normalizado.

Al guardar una modificación, el Cliente deberá continuar disponiendo de al menos uno entre Correo de contacto y Correo de facturación, además de mantener los demás datos obligatorios, incluido el número de WhatsApp. Podrá agregar un Correo ausente o vaciar uno de ellos si el otro permanece informado; no podrá guardar ambos vacíos.

Todo Correo de contacto o Correo de facturación que se encuentre informado deberá cumplir con el formato válido de Correo electrónico; un Correo vacío no será un error si el otro es válido. El número de WhatsApp se almacenará en formato internacional compatible con E.164, con + y código de país, y deberá validarse antes de utilizarlo en enlaces de WhatsApp. Se admitirán números internacionales válidos sin limitación a Uruguay; podrán normalizarse los espacios y separadores habituales ingresados antes de validar y guardar.

Proceso de negocio al que responde: Gestión y mantenimiento de clientes.

Dependencias: RF-84; RF-73; gestión de Clientes y Proyectos.

Necesidad asociada: N-04.

Prioridad: Alta.

## RF-087 — Gestionar estado Activo/Inactivo de clientes


**Descripción**


Por permisos fijos, Supervisor y Administrador podrán activar o inactivar Clientes; el Operador no podrá cambiar su estado, conforme a RF-73.

Un cliente inactivo deberá conservarse registrado en el sistema para mantener la información histórica relacionada con él.


**Reglas de negocio**


Un cliente en estado Inactivo no deberá eliminarse de la aplicación.

El cambio a estado Inactivo no deberá eliminar ni modificar sus presupuestos, proyectos, facturas o demás información histórica asociada

Un cliente Inactivo podrá volver posteriormente al estado Activo.

Antes de cambiar un cliente a estado Inactivo, el sistema deberá solicitar confirmación.

Un cliente Inactivo no deberá estar disponible para su selección al crear nuevos presupuestos o proyectos.

Proceso de negocio al que responde: Gestión de clientes.

Dependencias: RF-84.

Necesidad asociada: N-04.

Prioridad: Alta.

## RF-088 — Dashboard


**Descripción**


Por sus permisos base, Supervisor y Administrador podrán consultar el Dashboard y las estadísticas de Presupuestos; el Operador no tendrá acceso, sin excepciones individuales conforme a RF-73. El sistema deberá disponer de un dashboard que permita consultar de forma resumida indicadores relacionados con los presupuestos registrados.


**El dashboard deberá mostrar**


cantidad total de presupuestos realizados;

cantidad de presupuestos aprobados;

cantidad de presupuestos rechazados.

La información podrá presentarse mediante indicadores numéricos y representaciones gráficas, sin imponer un tipo de gráfico.

El dashboard deberá encontrarse disponible desde una opción independiente del menú principal denominada Estadísticas de presupuestos. Incluirá asimismo la distribución de Presupuestos efectivamente rechazados entre las ocho categorías de Motivo de rechazo del catálogo definitivo de RF-35, incluida la categoría Otro, con la cantidad real por categoría.


**Reglas de negocio**


Los indicadores deberán calcularse utilizando la información registrada actualmente en el sistema.

La consulta del dashboard no deberá modificar la información utilizada para generar los indicadores.

Los presupuestos deberán contabilizarse de acuerdo con su estado registrado en el sistema. La distribución por Motivo incluirá exclusivamente Presupuestos cuyo Estado actual sea Rechazado: cada uno se contará una sola vez en la categoría de su Motivo registrado; si se trata de un registro histórico sin Motivo, se contará en Otro solo a efectos estadísticos. La suma de las ocho categorías deberá coincidir con el total de Presupuestos actualmente Rechazados del mismo conjunto de registros; no se incluirán Presupuestos Vencidos, Cancelados, en Solicitud de modificación ni rechazos no confirmados. Podrá consultarse mediante gráfico y/o indicadores, sin imponer un tipo concreto de gráfico. La clasificación estadística de un rechazo histórico sin Motivo en Otro no modificará el registro histórico ni le atribuirá un Motivo seleccionado por un usuario. Consultar estadísticas no modificará Presupuestos ni Motivos.

Proceso de negocio al que responde: Seguimiento y análisis de la actividad comercial.

Dependencias: Gestión de presupuestos; RF-02.

Necesidad asociada: N-01 / N-05.

Prioridad: Baja.

## RF-089 — Registrar cliente


**Descripción**


Por permisos base, Operador, Supervisor y Administrador podrán registrar nuevos Clientes en la aplicación, según los permisos fijos de RF-73, sin excepciones individuales.

El registro de un cliente deberá poder iniciarse desde la sección general de Clientes y también cuando, durante la creación de un presupuesto o proyecto, el cliente necesario no se encuentre previamente registrado.

El cliente deberá registrar además un número de WhatsApp que será utilizado cuando se seleccione dicho medio de comunicación, permitiendo a la aplicación abrir mediante WhatsApp la conversación correspondiente al cliente.


**Reglas de negocio**


Para registrar un cliente deberán ingresarse obligatoriamente su nombre, RUT y número de WhatsApp; el Cliente deberá disponer de Correo de contacto y Correo de facturación como campos diferenciados y tener informado al menos uno de ellos. Se permitirá registrar solo uno o ambos, pero se impedirá el registro si ambos están vacíos.

El RUT del Cliente se almacenará exclusivamente con dígitos, sin puntos, espacios ni guiones; se validará la estructura de RUT uruguayo, incluido el dígito verificador. Los separadores habituales ingresados podrán normalizarse antes de validar y guardar; la unicidad se verificará sobre el RUT normalizado, de modo que representaciones diferentes no generen Clientes distintos.

Antes de registrar un nuevo Cliente, el sistema deberá comprobar que no exista otro con el mismo RUT normalizado, aunque se haya ingresado con separadores diferentes.

Una vez registrado, el cliente deberá quedar disponible inmediatamente para su utilización en presupuestos y proyectos.

Todo cliente creado deberá encontrarse inicialmente en estado Activo.

El número de WhatsApp se almacenará con + y código de país en formato internacional compatible con E.164. Podrán normalizarse espacios y separadores habituales antes de su validación y almacenamiento. Se aceptarán números internacionales válidos, sin limitarse a Uruguay; no se utilizará un número inválido para abrir un enlace de WhatsApp.

Todo Correo de contacto o Correo de facturación informado deberá tener formato válido; el que permanezca vacío no constituirá un error si el otro está informado y es válido.

Proceso de negocio al que responde: Gestión de clientes.

Dependencias: gestión de Clientes; RF-73.

Necesidad asociada: N-04.

Prioridad: Alta.

## RF-090 — Actualizar automáticamente precios de materiales desde proveedores


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-024 y US-056, excluidas en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá actualizar automáticamente, una vez por semana, los precios de las relaciones material-proveedor que se encuentren habilitadas para actualización automática.

Inicialmente esta funcionalidad se implementará para Fivisa.

La actualización se realizará únicamente sobre relaciones material-proveedor que hayan sido previamente vinculadas y validadas mediante RF-92.

Para cada relación vinculada, el sistema deberá utilizar la URL y la identificación o referencia externa almacenadas para consultar nuevamente el producto correspondiente en Fivisa, verificar que el producto consultado sea el esperado y obtener el precio normal publicado vigente del producto, sin considerar descuentos, promociones, ofertas ni precios especiales.

Cuando se recupere un precio normal válido, estrictamente mayor que cero, el sistema deberá actualizar únicamente el precio actual de la relación material-proveedor y su fecha de última actualización, manteniendo la moneda USD o UYU seleccionada y registrada al vincular el producto, sin obtenerla nuevamente de Fivisa ni convertir automáticamente los importes.

Los datos propios del material de EOP, incluyendo su nombre y unidad de medida, no deberán modificarse como consecuencia de la actualización.


**Reglas de negocio**


La actualización automática se realizará únicamente sobre relaciones material-proveedor que posean una vinculación válida con un producto de Fivisa, se encuentren registradas definitivamente en el catálogo y tengan habilitada la actualización automática

La relación deberá conservar la información necesaria para localizar nuevamente el producto externo, incluyendo su URL y código o referencia correspondiente.

Antes de modificar un precio, el sistema deberá verificar que el producto recuperado corresponda al producto vinculado.

Cuando se obtenga el precio normal publicado vigente del producto de Fivisa, deberá actualizarse exclusivamente el precio actual de la relación material-proveedor y su fecha de última actualización. Si se publican simultáneamente precios normales y promocionales, descuentos u ofertas, se utilizará el normal; ningún precio promocional, rebajado o especial deberá reemplazarlo en esta ni en posteriores ejecuciones.

La actualización automática no deberá modificar el nombre, unidad de medida ni otros datos propios del Material de EOP.

Si no es posible localizar el producto, verificar su identidad o recuperar correctamente un precio normal publicado estrictamente mayor que cero, deberán conservarse el último precio y la moneda registrados, sin modificar la fecha de última actualización.

El fallo en la actualización de una relación no deberá impedir el procesamiento de las restantes.

No deberán importarse automáticamente otros productos del catálogo de Fivisa.

La actualización automática deberá ejecutarse una vez por semana.

El precio de una relación material-proveedor habilitada para actualización automática mediante Fivisa no podrá ser modificado manualmente por el usuario.

Entre dos ejecuciones del proceso automático, deberá conservarse como precio actual el último valor obtenido y validado correctamente desde Fivisa.

Proceso de negocio al que responde: Gestión y actualización de precios de materiales.

Dependencias: Catálogo de materiales; proveedores; relación material-proveedor; precio y fecha de última actualización, RF-92

Necesidad asociada: N-01/N-02.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Media).

## RF-091 — Cancelar presupuesto


**Descripción**


Por sus permisos base, Supervisor y Administrador podrán cancelar Presupuestos; el Operador no podrá cancelar, sin excepciones individuales conforme a RF-73. El sistema deberá permitir cancelar un presupuesto cuando EOP decida no continuar con su procesamiento.

La cancelación deberá cambiar el estado del presupuesto a Cancelado, conservando la información, documentos y relaciones registrados hasta ese momento.


**Reglas de negocio**


Un presupuesto únicamente podrá cancelarse cuando se encuentre en alguno de los siguientes estados: Borrador, Para revisión interna, Enviado / pendiente de respuesta

La cancelación no deberá eliminar físicamente el presupuesto ni la información asociada.

Un presupuesto en estado Cancelado deberá permanecer disponible para su consulta.

Un presupuesto cancelado no podrá continuar con el flujo normal de elaboración, revisión o envío al cliente.

Antes de efectuar la cancelación, el sistema deberá solicitar confirmación explícita al usuario.

La cancelación de un presupuesto que ya se encuentre en estado Enviado / pendiente de respuesta no eliminará ni modificará el registro del envío realizado previamente.

Un presupuesto en estado Cancelado no podrá volver a editarse ni cambiar a otro estado dentro del flujo normal.

Un presupuesto Cancelado no deberá pasar posteriormente al estado Vencido.

Proceso de negocio al que responde: Gestión del ciclo de vida del presupuesto.

Dependencias: Gestión de estados de presupuesto.

Necesidad asociada: N-01.

Prioridad: Media.

## RF-092 — Vincular una relación material-proveedor con un producto de Fivisa


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-056, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá permitir vincular una relación entre un material de EOP y el proveedor Fivisa con un producto específico publicado por dicho proveedor, con el objetivo de habilitar la obtención y actualización automática de su precio. Por permisos base, Supervisor y Administrador podrán ingresar la URL de Fivisa, solicitar su validación y confirmar la vinculación; el Operador solo podrá hacerlo con el permiso individual específico correspondiente conforme a RF-73. Todos los roles podrán consultar si la relación se encuentra vinculada, según su acceso al catálogo.

Cuando el usuario seleccione Fivisa como proveedor de un material, el sistema deberá permitir ingresar la URL correspondiente al producto de Fivisa.

El usuario deberá poder solicitar la validación de dicha URL antes de confirmar la vinculación.

Durante la validación, el sistema deberá consultar la fuente digital de Fivisa e intentar identificar el producto correspondiente, recuperar un código o referencia que permita verificarlo posteriormente y obtener el precio normal publicado del producto.

Cuando la validación sea satisfactoria, el sistema deberá presentar al usuario la información necesaria para confirmar la vinculación.

Cuando la vinculación sea validada y confirmada por el usuario, la URL del producto, su código o referencia externa, el precio normal publicado obtenido, la moneda USD o UYU seleccionada obligatoriamente por el usuario y la fecha de actualización deberán quedar asociados a la relación material-proveedor dentro del flujo en curso. La moneda no se obtendrá ni seleccionará automáticamente a partir de la página de Fivisa; el usuario que confirma la vinculación será responsable de seleccionar la moneda correcta. Sin una moneda válida seleccionada o un precio normal estrictamente mayor que cero, no podrá confirmarse la vinculación.

Cuando la relación material-proveedor ya se encuentre registrada definitivamente en el catálogo, la confirmación de una vinculación válida deberá habilitar su actualización automática mediante RF-90.

Cuando la vinculación se realice al agregar Fivisa como nuevo proveedor de un material ya existente durante la elaboración de un presupuesto, de acuerdo con RF-22, los datos de vinculación deberán permanecer asociados a dicha relación en estado pendiente hasta que el usuario seleccione Confirmar datos y continuar. Recién al registrarse definitivamente la relación en el catálogo deberá quedar habilitada para la actualización automática definida en RF-90.


**Reglas de negocio**


La vinculación deberá realizarse sobre una relación material-proveedor cuyo proveedor sea Fivisa.

La vinculación deberá iniciarse mediante una acción explícita del usuario.

La URL ingresada deberá corresponder a un producto identificable de Fivisa.

La vinculación no deberá confirmarse cuando el sistema no pueda identificar correctamente el producto, recuperar un precio normal publicado estrictamente mayor que cero o el usuario no haya seleccionado expresamente una moneda válida (USD o UYU).

Cuando la validación sea satisfactoria, el sistema deberá conservar la URL y el código o referencia externa necesarios para localizar y verificar nuevamente el producto.

El precio recuperado durante la validación deberá ser el precio normal publicado del producto y utilizarse como precio de la relación material-proveedor dentro del flujo en curso; deberá registrarse su fecha de actualización. Si el producto también presenta un precio promocional o descontado, deberá utilizarse el normal; no se utilizarán descuentos, promociones, ofertas ni precios especiales como precio inicial.

El nombre y la unidad de medida del material deberán conservar los valores registrados por EOP y no deberán ser reemplazados por la información obtenida desde Fivisa.

Una vinculación confirmada deberá dejar preparada la relación material-proveedor para participar en el proceso definido en RF-90. La actualización automática únicamente deberá quedar habilitada cuando dicha relación se encuentre registrada definitivamente en el catálogo.

Cuando la vinculación se realice al agregar Fivisa como nuevo proveedor de un material ya existente durante la elaboración de un presupuesto, la URL, código o referencia externa, precio recuperado y demás datos de vinculación deberán permanecer pendientes junto con dicha relación hasta que el usuario seleccione Confirmar datos y continuar.

La funcionalidad no deberá importar automáticamente al catálogo de EOP otros productos publicados por Fivisa.

Si la validación falla, no deberá registrarse una vinculación válida ni reemplazarse un precio previamente existente.

Una vez confirmada la vinculación con Fivisa y habilitada la actualización automática, el precio de la relación material-proveedor no podrá ser modificado manualmente por el usuario.

Mientras la vinculación con Fivisa permanezca activa, el precio actual de la relación deberá provenir exclusivamente de la información recuperada desde Fivisa mediante RF-92 o RF-90.

Proceso de negocio al que responde: Gestión del catálogo y actualización de precios de materiales.

Dependencias: Catálogo de materiales; gestión de proveedores; relación material-proveedor.

Necesidad asociada: N-01 / N-02.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Media).

## RF-093 — Registrar un tipo de proyecto


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-057, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá permitir al Administrador registrar nuevos Tipos de proyecto desde la sección de administración correspondiente.

Para registrar un Tipo de proyecto, el Administrador deberá ingresar únicamente su Nombre.

Antes de completar el registro, el sistema deberá verificar que no exista otro Tipo de proyecto con el mismo nombre.

Una vez creado correctamente, el nuevo Tipo de proyecto deberá quedar inmediatamente disponible para ser seleccionado en las funcionalidades que requieran indicar un Tipo de proyecto.


**Reglas de negocio**


Solamente un usuario con rol Administrador podrá registrar Tipos de proyecto.

Cada Tipo de proyecto deberá contener únicamente el atributo Nombre.

El Nombre será obligatorio.

El Nombre deberá ser único dentro del catálogo de Tipos de proyecto.

Antes de registrar un nuevo Tipo de proyecto, el sistema deberá verificar que no exista otro con el mismo nombre.

Si ya existe un Tipo de proyecto con el nombre ingresado, el sistema deberá impedir la creación e informar al usuario.

Un Tipo de proyecto registrado correctamente deberá quedar inmediatamente disponible para ser seleccionado en las funcionalidades de la aplicación que requieran indicar un Tipo de proyecto.

El registro de un Tipo de proyecto no deberá requerir ningún dato adicional al Nombre.

Proceso de negocio al que responde: Administración de datos maestros utilizados en presupuestos y proyectos.

Dependencias: RF-72; RF-73.

Necesidad asociada: N-01 / N-04 / N-07.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-094 — Consultar y buscar tipos de proyecto


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-058, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá permitir al Administrador acceder a una sección de administración de Tipos de proyecto desde la cual pueda consultar los tipos registrados y buscarlos por Nombre.

El listado deberá mostrar el Nombre de cada Tipo de proyecto y la acción Eliminar cuando corresponda de acuerdo con RF-95.


**Reglas de negocio**


Solamente un usuario con rol Administrador podrá acceder a la administración de Tipos de proyecto.

El listado deberá mostrar todos los Tipos de proyecto registrados.

Para cada Tipo de proyecto deberá mostrarse su Nombre.

Cuando corresponda, deberá encontrarse disponible la acción Eliminar de acuerdo con RF-95.

El Administrador deberá poder buscar un Tipo de proyecto por su Nombre.

La búsqueda deberá utilizarse únicamente para localizar Tipos de proyecto y no deberá modificar la información registrada.

No será necesario incorporar filtros adicionales para el listado de Tipos de proyecto.

Proceso de negocio al que responde: Administración de datos maestros utilizados en presupuestos y proyectos.

Dependencias: RF-93.

Necesidad asociada: N-01 / N-04.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-095 — Eliminar un tipo de proyecto


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-059, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá permitir al Administrador eliminar un Tipo de proyecto cuando haya sido creado por error y no se encuentre utilizado por ningún presupuesto ni proyecto.

Antes de realizar la eliminación, el sistema deberá solicitar confirmación y comprobar que el Tipo de proyecto no se encuentre asociado a información existente.


**Reglas de negocio**


Solamente un usuario con rol Administrador podrá eliminar Tipos de proyecto.

Antes de eliminar un Tipo de proyecto deberá solicitarse confirmación al usuario.

Antes de completar la eliminación, el sistema deberá verificar si el Tipo de proyecto se encuentra asociado a algún presupuesto.

Antes de completar la eliminación, el sistema deberá verificar si el Tipo de proyecto se encuentra asociado a algún proyecto.

Si el Tipo de proyecto no se encuentra asociado a ningún presupuesto ni proyecto, podrá eliminarse.

Si el Tipo de proyecto se encuentra asociado a uno o más presupuestos o proyectos, el sistema deberá impedir su eliminación.

Cuando la eliminación sea impedida porque el Tipo de proyecto se encuentra en uso, el sistema deberá informar claramente dicha situación al usuario.

La restricción de eliminación deberá proteger tanto las asociaciones actuales como la información histórica existente.

Cancelar la confirmación de eliminación no deberá producir ninguna modificación.

Proceso de negocio al que responde: Administración de datos maestros y preservación de información histórica.

Dependencias: RF-94; gestión de presupuestos; gestión de proyectos.

Necesidad asociada: N-01 / N-04.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Baja).

## RF-096 — Registrar un tipo de documento


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-060, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá permitir al Supervisor o Administrador, o a un Operador con el permiso individual específico correspondiente conforme a RF-73, registrar nuevos Tipos de documento utilizados para clasificar los archivos cargados en los proyectos.

Para registrar un Tipo de documento deberá ingresarse únicamente su Nombre.

Una vez creado correctamente, el nuevo Tipo de documento deberá quedar inmediatamente disponible para su utilización al cargar documentos en proyectos.


**Reglas de negocio**


Por permisos base, Supervisor y Administrador podrán registrar Tipos de documento; el Operador solo podrá hacerlo con el permiso individual específico correspondiente conforme a RF-73.

Cada Tipo de documento tendrá únicamente el atributo Nombre.

El Nombre será obligatorio.

El Nombre deberá ser único dentro del catálogo de Tipos de documento.

Antes de completar el registro, el sistema deberá comprobar que no exista otro Tipo de documento con el mismo Nombre.

Si ya existe un Tipo de documento con el Nombre ingresado, el sistema deberá impedir la creación e informar al usuario.

Una vez registrado correctamente, el Tipo de documento deberá quedar inmediatamente disponible para ser seleccionado al cargar documentos en un proyecto.

No deberá requerirse ningún otro dato para registrar un Tipo de documento.

Proceso de negocio al que responde: Gestión documental y administración de datos de clasificación.

Dependencias: Gestión de usuarios, roles y permisos.

Necesidad asociada: N-04 / N-06.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-097 — Consultar tipos de documento


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-061, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá permitir al Supervisor o Administrador, o a un Operador con el permiso individual específico correspondiente conforme a RF-73, acceder a una sección específica denominada Tipos de documento y consultar los Tipos registrados en la aplicación.

El listado deberá mostrar el Nombre de cada Tipo y la acción Eliminar cuando corresponda de acuerdo con RF-98.


**Reglas de negocio**


Por permisos base, Supervisor y Administrador podrán acceder a la administración de Tipos de documento; el Operador solo podrá hacerlo con el permiso individual específico correspondiente conforme a RF-73.

La sección deberá mostrar los Tipos de documento registrados en la aplicación.

Para cada Tipo de documento deberá mostrarse su Nombre.

Cuando corresponda, deberá encontrarse disponible la acción Eliminar de acuerdo con RF-98.

La consulta no deberá modificar la información registrada.

No se requiere una funcionalidad específica de búsqueda o filtrado dentro del catálogo de Tipos de documento.

Proceso de negocio al que responde: Gestión documental y administración de datos de clasificación.

Dependencias: RF-96.

Necesidad asociada: N-04 / N-06.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-098 — Eliminar un tipo de documento


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-062, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá permitir al Supervisor o Administrador, o a un Operador con el permiso individual específico correspondiente conforme a RF-73, eliminar un Tipo de documento creado por error únicamente cuando dicho tipo no se encuentre utilizado por ningún documento existente.

Antes de realizar la eliminación, deberá solicitarse confirmación.


**Reglas de negocio**


Por permisos base, Supervisor y Administrador podrán eliminar Tipos de documento; el Operador solo podrá hacerlo con el permiso individual específico correspondiente conforme a RF-73.

Antes de completar la eliminación deberá solicitarse confirmación.

Antes de eliminar un Tipo de documento, el sistema deberá verificar si se encuentra asociado a uno o más documentos existentes.

Si el Tipo de documento no se encuentra utilizado por ningún documento, podrá eliminarse.

Si el Tipo de documento se encuentra asociado a uno o más documentos, el sistema deberá impedir su eliminación.

Cuando la eliminación sea impedida por encontrarse el Tipo de documento en uso, el sistema deberá informar dicha situación al usuario.

La restricción deberá preservar la clasificación histórica de todos los documentos existentes.

Cancelar la confirmación no deberá realizar ninguna modificación.

Proceso de negocio al que responde: Gestión documental y preservación de información histórica.

Dependencias: RF-97; gestión documental de proyectos.

Necesidad asociada: N-04 / N-06.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Baja).

## RF-099 — Registrar una unidad


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-063, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá disponer inicialmente de las Unidades m, m², m³, kg y litro y permitir al Supervisor o Administrador, o a un Operador con el permiso individual específico correspondiente conforme a RF-73, registrar nuevas Unidades cuando EOP necesite una que no exista.

Para registrar una nueva Unidad deberá ingresarse únicamente su Nombre. Una vez creada correctamente quedará inmediatamente disponible al registrar nuevos materiales.


**Reglas de negocio**


La aplicación deberá disponer inicialmente de las Unidades m, m², m³, kg y litro, disponibles desde el inicio para seleccionar al registrar materiales.

Por permisos base, Supervisor y Administrador podrán registrar Unidades; el Operador solo podrá hacerlo con el permiso individual específico de administración de Unidades conforme a RF-73.

Cada Unidad tendrá únicamente el atributo Nombre.

El Nombre será obligatorio y único.

Antes de registrar una nueva Unidad se verificará que no exista otra con el mismo Nombre.

Si el Nombre ya existe, se impedirá la creación y se informará al usuario.

Una nueva Unidad quedará inmediatamente disponible para seleccionar al crear materiales.

No se requerirá ningún dato adicional al Nombre.

Proceso de negocio al que responde: Administración del catálogo de materiales y datos maestros.

Dependencias: Gestión de usuarios, roles y permisos.

Necesidad asociada: N-02.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-100 — Consultar unidades


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-064, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá permitir al Supervisor o Administrador, o a un Operador con el permiso individual específico correspondiente conforme a RF-73, acceder a una sección denominada Unidades y consultar todas las Unidades registradas, tanto las precargadas como las creadas posteriormente.


**Reglas de negocio**


Por permisos base, Supervisor y Administrador podrán acceder a la administración de Unidades; el Operador solo podrá hacerlo con el permiso individual específico de administración de Unidades conforme a RF-73.

El listado incluirá todas las Unidades registradas, incluidas m, m², m³, kg y litro.

Para cada Unidad se mostrará su Nombre y, cuando corresponda, la acción Eliminar de acuerdo con RF-101.

Desde la sección podrá iniciarse la creación de una nueva Unidad mediante RF-99.

La consulta no modificará información registrada.

No se requiere búsqueda ni filtrado dentro de la sección Unidades.

Proceso de negocio al que responde: Administración del catálogo de materiales y datos maestros.

Dependencias: RF-99.

Necesidad asociada: N-02.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-101 — Eliminar una unidad


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-065, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

El sistema deberá permitir al Supervisor o Administrador, o a un Operador con el permiso individual específico correspondiente conforme a RF-73, eliminar una Unidad solamente cuando no esté asociada a ningún Material registrado ni a un Concepto de Presupuesto que conserve referencia a esa Unidad. Antes deberá solicitar confirmación y verificarse que no esté en uso.


**Reglas de negocio**


Por permisos base, Supervisor y Administrador podrán eliminar Unidades; el Operador solo podrá hacerlo con el permiso individual específico de administración de Unidades conforme a RF-73.

Antes de completar la eliminación se solicitará confirmación.

Se verificará si la Unidad se encuentra asociada a uno o más Materiales registrados o si algún Concepto de Presupuesto conserva una referencia a ella.

Una Unidad, tanto precargada como creada posteriormente, podrá eliminarse si no se encuentra asociada a ningún Material ni referenciada por ningún Concepto de Presupuesto.

Si está asociada a un Material o referenciada por un Concepto de Presupuesto, se impedirá su eliminación y se informará al usuario.

La eliminación no dejará Materiales existentes ni Conceptos de Presupuesto con referencias inválidas a la Unidad.

Cancelar la confirmación no realizará modificaciones.

Las Unidades precargadas m, m², m³, kg y litro podrán eliminarse si no están asociadas a ningún Material ni referenciadas por un Concepto de Presupuesto; no tendrán protección especial por ser precargadas.

Proceso de negocio al que responde: Administración del catálogo de materiales y preservación de información existente.

Dependencias: RF-100; gestión de materiales.

Necesidad asociada: N-02.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Baja).

## RF-102 — Agregar cuentas bancarias a un proveedor existente


**Descripción**


El sistema deberá permitir al Supervisor o Administrador agregar nuevas Cuentas bancarias a un Proveedor existente desde su sección. Todo Proveedor deberá mantener al menos una Cuenta válida y podrá tener múltiples Cuentas. Cada Cuenta nueva requerirá Banco, Número de cuenta, Titular y Moneda. La edición de Cuentas existentes se rige por RF-109; esta función no permite eliminar Cuentas.


**Reglas de negocio**


La nueva Cuenta quedará asociada al Proveedor seleccionado.

Un Proveedor podrá poseer múltiples Cuentas bancarias.

Agregar una nueva Cuenta no creará un nuevo Proveedor ni reemplazará las Cuentas previamente registradas.

Banco será obligatorio y deberá seleccionarse de la lista fija hardcodeada definida en RF-23.

Número de cuenta y Titular serán obligatorios.

Moneda será obligatoria y se seleccionará exclusivamente entre UYU y USD.

No podrá guardarse una Cuenta incompleta.

Solo Supervisor y Administrador podrán agregar Cuentas bancarias según los permisos fijos de RF-73. Operador no podrá hacerlo.

Proceso de negocio al que responde: Gestión de Proveedores e información bancaria.

Dependencias: RF-23; RF-73; gestión de usuarios, roles y permisos.

Necesidad asociada: N-02.

Prioridad: Media.

## RF-103 — Consultar resumen económico y gestionar costos adicionales del proyecto


**Descripción**


El sistema deberá mostrar en la ficha de cada Proyecto la sección Resumen económico con exactamente cinco indicadores: Costo presupuestado sin margen, Precio de venta presupuestado, Costo según facturas de compra, Costo real total e Ingresos facturados.

El módulo permitirá a Supervisor y Administrador gestionar costos adicionales del Proyecto. Los indicadores se calcularán desde el Presupuesto vinculado, las Facturas activas y los costos adicionales registrados, manteniendo importes originales y conversiones históricas válidas.


**Reglas de negocio**


La información corresponderá exclusivamente al Proyecto consultado; Operador, Supervisor y Administrador podrán consultar sus indicadores si tienen acceso al Proyecto, conforme a RF-73. No se podrán modificar directamente los cinco indicadores calculados.

Costo presupuestado sin margen se obtendrá del Presupuesto asociado como suma de componentes de costo antes del concepto Margen; incluirá sus desgloses según los conceptos aplicables sin sumar nominalmente USD con UYU.

Precio de venta presupuestado representará el total del Presupuesto después del Margen, en USD y UYU según RF-29; cuando ya se hubiera enviado, conservará los totales y cotización confirmados para ese envío.

Si el Proyecto no tiene Presupuesto asociado, los dos indicadores presupuestados figurarán como no disponibles (no como cero). Podrán existir Facturas y costos adicionales aun sin Presupuesto.

Costo según facturas de compra será la suma de las contribuciones de todas las Facturas de compra activas del Proyecto y no incluirá Facturas de venta ni costos adicionales.

Ingresos facturados será la suma de las contribuciones de Facturas de venta activas del Proyecto; estas no alterarán por sí mismas los indicadores de costos.

El efecto de cada comprobante dependerá exclusivamente de su Tipo: e-Factura, e-Ticket y las Notas de débito sumarán; las Notas de crédito restarán. Los importes originales permanecerán positivos y el signo se aplicará una sola vez.

Costo real total = Costo según facturas de compra + suma de costos adicionales del Proyecto. Se calculará automáticamente y no se ingresará manualmente.

Únicamente Supervisor y Administrador podrán agregar, modificar o eliminar costos adicionales conforme a RF-73; Operador solo podrá consultarlos. La autorización para consultar no habilitará su mantenimiento.

Para registrar un costo adicional se exigirán Nombre no vacío, Importe numérico positivo, Moneda UYU o USD y Fecha del costo; se conservarán sus valores originales y su relación exclusiva con el Proyecto de origen.

En el formulario Agregar costo se mostrará la advertencia informativa de que deben registrarse solamente costos no incluidos en las Facturas de compra, para evitar contabilizarlos dos veces; el sistema no promete detectar automáticamente esa duplicación.

Los usuarios autorizados podrán editar Nombre, Importe, Moneda y Fecha del costo de un costo adicional existente, conservando los datos anteriores ante cancelación o validación fallida. También podrán eliminarlo con confirmación, retirándolo del desglose y del cálculo, sin afectar Facturas.

El desglose bajo Costo real total mostrará una fila para Costo según facturas de compra y una por cada costo adicional con Nombre, Importe, Moneda y Fecha; las acciones de mantenimiento se mostrarán solo a los roles autorizados.

Cada Factura y costo adicional conservará su Importe y Moneda originales UYU o USD. Las cotizaciones históricas aplicadas conservarán el valor y la fecha efectiva directamente en el registro correspondiente, sin tabla independiente de cotizaciones.

La cotización de referencia será UYU por USD y se obtendrá de datosUruguay API a la Fecha de emisión de la Factura o Fecha del costo. Si ese día no existe publicación, se utilizará la última publicación válida anterior; no una posterior, un promedio ni un valor arbitrario. Esta regla se aplicará solo ante la ausencia de publicación en esa fecha; una falla temporal de datosUruguay no se interpretará como ausencia de publicación ni habilitará el uso de una cotización de otra fecha.

Para una operación en USD, su equivalente UYU se calculará multiplicando Importe USD por la cotización UYU/USD; para una en UYU, su equivalente USD se calculará dividiendo Importe UYU por esa cotización. Se aplicarán posteriormente los signos económicos del tipo de comprobante.

Para nuevas Facturas o costos adicionales que necesiten una conversión histórica válida, se exigirá cotización válida disponible (incluida una ya recuperada para ese mismo registro cuando corresponda). Si no existe y el servicio no puede suministrarla, el sistema rechazará el guardado, informará el motivo y no almacenará conversiones pendientes.

Si una Factura o costo ya tiene la cotización histórica válida aplicada y registrada, las consultas reutilizarán el valor y fecha guardados aunque datosUruguay esté indisponible, sin sustituirla por la cotización actual.

Al modificar Fecha del costo o Moneda de un costo adicional se verificará nuevamente la cotización histórica aplicable antes de guardar; si solamente cambia el Importe, podrá reutilizarse la cotización guardada válida para su misma fecha y moneda. Si falta la nueva cotización exigida, no se guardará la modificación y se conservarán los valores anteriores.

Costo según facturas de compra e Ingresos facturados se consolidarán en UYU. Costo real total estará disponible en UYU y USD, sumando equivalentes individuales válidos y sin convertir el total consolidado mediante una única cotización actual. Los importes presupuestados mantendrán la lógica y cotizaciones de RF-29.

El registro de una Factura activa y la eliminación lógica de una Factura recalcularán los indicadores correspondientes. El alta, modificación y eliminación de costos adicionales recalcularán Costo real total. No existen recálculos por edición ni reasignación de Facturas guardadas.

La falta de costos registrados no impedirá por sí sola finalizar un Proyecto. Los indicadores son referencias económicas de los comprobantes registrados y no controlarán impuestos netos, pagos, cobros, saldos ni resultados contables definitivos.

Los indicadores se mostrarán con dos decimales y redondeo matemático habitual al resultado final, conservando precisión intermedia y sin alterar los importes originales.

Proceso de negocio al que responde: Seguimiento económico y control de costos e ingresos de proyectos.

Dependencias: RF-54; RF-27; RF-29; RF-73; RF-77; RF-78; RF-104; Presupuesto del Proyecto; costos adicionales; datosUruguay API.

Necesidad asociada: N-04 / N-05.

Prioridad: Alta.

## RF-104 — Registrar facturas de compra mediante carga manual y PDF original


**Descripción**


El sistema deberá permitir a Supervisor o Administrador registrar manualmente una Factura de compra desde Facturas de compra de la ficha de un Proyecto existente. El usuario seleccionará y confirmará un Tipo de comprobante fijo, adjuntará el PDF original obligatorio, seleccionará un Proveedor existente y completará los datos del formulario antes de Guardar factura.

No se utilizarán OCR, Azure AI Document Intelligence ni creación de Proveedores desde la Factura. La Factura se asociará al Proyecto de origen y contribuirá a los indicadores de costos según RF-103.


**Reglas de negocio**


El registro comenzará desde la sección Facturas de compra del Proyecto. Supervisor y Administrador podrán ejecutarlo según RF-73; Operador tendrá únicamente permiso de consulta. El Proyecto de origen se mostrará automáticamente y no se podrá cambiar.

Antes de adjuntar el PDF, el usuario elegirá y confirmará manualmente uno de estos seis Tipos fijos: e-Factura (DGI 111), e-Ticket (101), Nota de crédito de e-Factura (112), Nota de crédito de e-Ticket (102), Nota de débito de e-Factura (113) y Nota de débito de e-Ticket (103). No se podrán administrar tipos ni determinar su valor mediante extracción.

El PDF original será obligatorio, válido y privado, y se podrá adjuntar mediante selección de archivo o arrastrar y soltar; sin PDF no se registrará la Factura.

El formulario mostrará Proyecto, Receptor EOP y RUT receptor EOP configurado como datos automáticos y de solo lectura. El Proveedor emisor se elegirá de los ya registrados, mediante búsqueda/selección manual; no se registrarán Proveedores dentro de este formulario.

Si el Proveedor necesario no existe, deberá darse de alta previamente en el módulo Proveedores conforme a RF-23; una vez existente, podrá seleccionarse al retomar la carga de la Factura. No se realizarán búsquedas automáticas a partir del PDF.

El usuario ingresará manualmente Fecha de emisión, Serie, Número real del comprobante, Importe registrado y Moneda. El Tipo confirmado y el Proveedor elegido serán obligatorios. No habrá precargas ni advertencias basadas en OCR.

El Importe registrado deberá ser positivo y corresponder al total final del comprobante, incluidos impuestos y redondeos. No se registrarán por separado saldos, adelantos, líneas ni pagos parciales; las Notas de crédito conservarán importe positivo.

La Moneda admitirá solamente UYU o USD, y se conservarán el Importe y la Moneda originales.

Al confirmar, se almacenará como dato histórico propio de la Factura el RUT emisor normalizado y validado del Proveedor seleccionado; cambios posteriores al RUT de ese Proveedor no modificarán el dato histórico del comprobante.

Antes de guardar se impedirá el duplicado únicamente frente a Facturas de compra activas de su categoría con igual RUT emisor histórico normalizado + Tipo + Serie + Número real. Una Factura eliminada lógicamente no impedirá volver a registrar el comprobante con un código interno nuevo.

Cuando una Factura requiera conversión USD/UYU para el cálculo económico, se usará la cotización histórica válida de datosUruguay API correspondiente a su Fecha de emisión, o la última publicación válida anterior cuando ese día no tenga cotización; una falla temporal del servicio no se interpretará como ausencia de publicación. El valor y la fecha efectivamente aplicados quedarán guardados en la Factura. Si no existe una cotización válida almacenada o disponible a través del servicio, se impedirá Guardar factura y se informará el motivo; no se registrarán conversiones pendientes.

El guardado válido conservará los datos estructurados y el PDF original de manera privada en Azure Blob Storage, con autorización del lado del servidor y sin guardar el archivo íntegro en SQL Server. No se incorporará automáticamente como documento genérico del Proyecto.

Cada Factura activa pertenecerá a un único Proyecto, obtenido del contexto de registro. No se podrán editar posteriormente datos, cambiar el Proveedor, reemplazar el PDF ni reasignar el Proyecto; únicamente eliminar lógicamente conforme a RF-78.

El identificador interno será FC-####, correlativo global de compras e independiente de FV-####, empezando en FC-0001 y continuando después de FC-9999 con FC-10000; no se reiniciará por año, Proveedor o Proyecto ni se reutilizará.

Las Facturas de compra activas contribuirán a Costo según facturas de compra y Costo real total, aplicando el signo del comprobante una vez; no afectarán Ingresos facturados. Su cálculo se regirá por RF-103.

Una cancelación, validación fallida o falta de cotización histórica requerida impedirá guardar la Factura definitiva incompleta; se informará el motivo.

Proceso de negocio al que responde: Gestión administrativa y registro de compras de proyectos.

Dependencias: RF-23; RF-54; RF-73; Proyecto y Proveedor existentes; datosUruguay API cuando se requiera conversión; Azure Blob Storage privado.

Necesidad asociada: N-04 / N-05.

Prioridad: Alta.

## RF-105 — Consultar facturas de compra.


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-069, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

La sección general Facturas dispondrá de un apartado Facturas de compra separado del de Facturas de venta, con listado, búsqueda y filtros propios para Facturas registradas mediante RF-104. Operador, Supervisor y Administrador podrán consultarlas conforme a su acceso efectivo definido en RF-73.


**Reglas de negocio**


El apartado mostrará exclusivamente Facturas de compra activas, cada una asociada a exactamente un Proyecto; no se presentará un Estado de asociación.

Cada entrada mostrará como mínimo identificador interno FC-####, Tipo de comprobante, Serie, Número real del comprobante, Fecha de emisión, Proveedor, Importe registrado, Moneda y Proyecto asociado.

Desde el apartado podrá consultarse la información de la Factura y acceder a su PDF original mediante RF-83. La consulta no modificará datos, PDF ni asociación con el Proyecto.

La asociación de una Factura con su Proveedor no constituye una relación entre Materiales y Proveedores del catálogo de Presupuestación.

Proceso de negocio al que responde: Gestión administrativa de compras y costos.

Dependencias: RF-104; RF-83; gestión de usuarios y permisos (RF-73).

Necesidad asociada: N-04 / N-05.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-106 — Buscar y filtrar facturas de compra.


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-069, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

Por permisos base, Operador, Supervisor y Administrador podrán buscar y filtrar Facturas de compra conforme al acceso efectivo de RF-73. En el apartado Facturas de compra se podrá buscar por identificador interno FC-#### o Número real del comprobante y filtrar por Fecha, Proveedor y Proyecto asociado.


**Reglas de negocio**


Los resultados se limitarán a las Facturas de compra accesibles para el usuario que cumplan los criterios aplicados.

La búsqueda podrá combinarse con los filtros y estos podrán combinarse entre sí.

Buscar o filtrar no modificará información.

El apartado conservará sus criterios independientes de los de Facturas de venta, sin agregar otros filtros no confirmados.

Proceso de negocio al que responde: Consulta y gestión administrativa de compras.

Dependencias: RF-105; gestión de usuarios y permisos (RF-73).

Necesidad asociada: N-04.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-107 — Reasignar una factura de compra a otro proyecto.


**Descripción**


Estado: FUERA DEL ALCANCE DE LA PRIMERA VERSIÓN. Corresponde a US-070, excluida en el apartado «Alcances y limitaciones». Se conserva únicamente por trazabilidad histórica; no constituye un requerimiento a implementar en esta versión.

La asociación inicial al Proyecto se realiza automáticamente durante RF-104. Este requerimiento permitirá a un usuario autorizado ejecutar Reasignar sobre una Factura de compra existente para cambiar su Proyecto asociado, sin crear una Factura nueva ni modificar los datos o el PDF original.


**Reglas de negocio**


Supervisor y Administrador podrán reasignar por sus permisos base; Operador necesitará el permiso individual específico correspondiente conforme a RF-73.

El usuario podrá buscar por Nombre y seleccionar cualquier Proyecto de destino registrado, independientemente de su Cliente; no se aplicará la restricción de mismo Cliente propia de las Facturas de venta.

Podrán elegirse Proyectos en los Estados Pendiente de inicio, En ejecución, En pausa, Finalizado y Cancelado. El Estado no constituirá por sí mismo una restricción adicional.

La operación requerirá confirmación explícita. Al completarse, la Factura dejará de pertenecer al Proyecto anterior y quedará asociada exclusivamente al Proyecto de destino; toda Factura activa seguirá perteneciendo a exactamente un Proyecto. No se ofrecerá una acción independiente Desvincular.

Se conservarán el código FC-####, Proveedor, Tipo de comprobante, Serie, Número real, Fecha de emisión, Importe registrado, Moneda y PDF original, sin volver a cargarlo; no se modificarán los datos, no se creará otra Factura y se conservarán las conversiones históricas válidas vinculadas a los datos del comprobante. Reasignar por sí solo no cambia Fecha ni cotización aplicable.

La Factura desaparecerá de Facturas de compra del Proyecto anterior y aparecerá en las del Proyecto de destino. Se recalcularán Costo según facturas de compra y Costo real total en ambos Proyectos conforme a RF-103.

Si el usuario cancela o la operación falla, la Factura permanecerá íntegramente asociada al Proyecto original, sin cambios parciales.

Proceso de negocio al que responde: Organización administrativa y seguimiento económico de proyectos.

Dependencias: RF-104; RF-105; gestión de Proyectos; gestión de usuarios y permisos (RF-73).

Necesidad asociada: N-04 / N-05.

Prioridad: No aplica en la primera versión (fuera de alcance; prioridad original: Alta).

## RF-108 — Modificar material y precios de proveedores asociados desde catálogo


**Descripción**


El sistema deberá permitir al Supervisor o Administrador editar desde el Catálogo de Materiales el nombre y unidad de un Material existente y mantener el precio y/o moneda de sus referencias de Proveedor ya registradas.
Las relaciones material-proveedor nuevas se registrarán de manera independiente conforme a RF-22; esta edición no eliminará relaciones ni habilitará actualización automática desde Fivisa.


**Reglas de negocio**


El Material modificado conservará su identidad y sus relaciones previamente registradas; no se creará otro Material.

El nombre seguirá siendo obligatorio y único por denominación completa normalizada; la Unidad se seleccionará entre las opciones fijas.

Se mantendrá al menos una relación material-proveedor válida con precio mayor que cero y moneda UYU o USD.

El usuario autorizado podrá actualizar expresamente precio y/o moneda de cada relación existente conforme a RF-21, actualizando su fecha de última modificación.

No se permitirá editar precios ni monedas del catálogo desde la tabla de un presupuesto; tampoco se crearán relaciones nuevas en esta edición.
Una modificación del catálogo no cambiará retroactivamente precios, monedas ni otros valores históricos registrados en presupuestos o facturas.
No existirá regla especial que impida la edición manual de referencias de Fivisa; no habrá integración ni sincronización automática.
Si falla una validación o el guardado, el Material y sus relaciones conservarán sus valores anteriores sin cambios parciales.
El Operador podrá consultar materiales pero no modificar sus datos ni precios bajo el esquema fijo de permisos.

Proceso de negocio al que responde: Gestión del catálogo de Materiales.

Dependencias: RF-67; RF-68; RF-22; RF-73.

Necesidad asociada: N-02.

Prioridad: Alta.

## RF-109 — Modificar datos de un proveedor existente, incluido su RUT


**Descripción**


El sistema deberá permitir al Supervisor o Administrador editar datos de un Proveedor existente desde el módulo Proveedores, incluidos Nombre, RUT, Teléfono, Correos y datos permitidos de cuentas bancarias existentes.
El RUT de cualquier Proveedor, incluido Fivisa, podrá modificarse con las mismas reglas de validación y unicidad que los demás Proveedores, conservando asociaciones y datos históricos.


**Reglas de negocio**


La modificación se realizará sobre el mismo Proveedor y conservará sus asociaciones con MaterialProveedor, Facturas y Cuentas bancarias.

El Nombre y RUT serán obligatorios y únicos frente a otros Proveedores. El Nombre se comparará por denominación completa normalizada; el RUT por su valor solo dígitos, validando formato uruguayo y dígito verificador.

El Teléfono será obligatorio y se conservará como texto con formato internacional compatible con E.164.

Al menos uno de los Correos de contacto o facturación deberá estar informado y tener formato válido; si ambos se informan deberán ser válidos.

Se conservará al menos una Cuenta bancaria completa; podrá editarse Banco, Número de cuenta, Titular y Moneda de Cuentas existentes sin eliminarlas; agregar otra Cuenta se realizará por RF-102.

La actualización del Proveedor no modificará retroactivamente RUT del emisor, identidad, importes, monedas ni datos históricos guardados en facturas y presupuestos.
Se permitirá modificar el RUT del Proveedor Fivisa con idénticas validaciones de formato, dígito verificador y unicidad; no existirá una restricción especial de inmutabilidad para ese Proveedor.
Si una validación o el guardado falla no se aplicarán cambios parciales y se conservarán los datos previos.
Solo Supervisor y Administrador podrán modificar Proveedores; Operador podrá consultarlos y seleccionarlos, sin excepciones individuales.

Proceso de negocio al que responde: Gestión del catálogo de Proveedores.

Dependencias: RF-23; RF-102; RF-73.

Necesidad asociada: N-02.

Prioridad: Alta.
