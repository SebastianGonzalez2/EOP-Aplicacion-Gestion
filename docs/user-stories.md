# User stories

> Generado desde el anteproyecto corregido. **El documento Word es el original**;
> este archivo es la copia de trabajo del repositorio. Si corregís el Word,
> hay que regenerar este archivo.


76 historias de usuario. 58 dentro del alcance de la primera versión, 18 excluidas.

Las excluidas llevan un aviso al comienzo de su sección.

---

## US-001 — Consultar y localizar presupuestos

Figura 7. Mockup US-001 — Consultar y localizar presupuestos

Tabla 11. Historia de usuario US-001 — Consultar y localizar presupuestos


| COMO: | Como Operador, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero consultar y filtrar los presupuestos registrados |
| PARA: | Para localizar rápidamente el presupuesto sobre el que necesito trabajar. |
| DESCRIPCIÓN: | Desde la opción Presupuestos del menú principal se presenta un listado general. Cada registro permite identificar el código del presupuesto, nombre del proyecto, cliente, fecha, estado y Responsable. El usuario puede aplicar filtros por código, estado, cliente, fecha y Responsable, y acceder al presupuesto seleccionado. Los presupuestos anteriores permanecen disponibles como antecedentes, pero no pueden duplicarse automáticamente para crear uno nuevo. Al acceder a un Presupuesto Rechazado con Motivo de rechazo registrado, dicho Motivo podrá consultarse sin agregarlo necesariamente al listado ni incorporarlo como filtro. |
| Permisos aplicables: | Por sus permisos base, Operador, Supervisor y Administrador pueden consultar, buscar y filtrar Presupuestos utilizando exclusivamente Código, Estado, Cliente, Fecha y Responsable; no se admiten excepciones individuales; se respetan exclusivamente los permisos fijos de RF-73. |
| Reglas de negocio: | Todo presupuesto permanece disponible para consulta. Los filtros no modifican información. Los filtros pueden combinarse. El estado determina qué acciones se encuentran disponibles. No existe duplicación automática de presupuestos anteriores. El listado deberá mostrar el Responsable correspondiente a cada presupuesto. El usuario deberá poder filtrar los presupuestos por Responsable. Al seleccionar un Responsable, deberán mostrarse únicamente los presupuestos asociados a dicho usuario como Responsable. El filtro por Responsable deberá poder combinarse con los demás filtros disponibles. El Motivo de rechazo registrado en un Presupuesto Rechazado deberá poder consultarse al acceder a dicho Presupuesto. |
| Requisitos relacionados: | RF: RF-02, RF-03, RF-04. RNF: RNF-4, RNF-9, RNF-10, RNF-14. |
| Criterios de aceptación |  |
| Escenario: consultar presupuestos | Dado que el usuario posee permisos para acceder a Presupuestos Cuando ingresa a la sección Presupuestos Entonces el sistema deberá mostrar los presupuestos registrados con su información principal Y deberá mostrar el Responsable correspondiente a cada presupuesto. |
| Escenario: consultar Motivo de un Presupuesto rechazado | Dado un Presupuesto Rechazado con Motivo de rechazo registrado Cuando el usuario accede a su información Entonces deberá poder consultar dicho Motivo. |
| Escenario: combinar filtros | Dado que existen presupuestos de diferentes clientes, estados y Responsables Cuando el usuario selecciona uno o más criterios entre Código, Estado, Cliente, Fecha y Responsable Entonces deberán mostrarse únicamente los presupuestos que cumplan los criterios seleccionados. |
| Escenario: filtrar por Responsable | Dado que existen presupuestos asociados a distintos Responsables Cuando el usuario selecciona un Responsable como filtro Entonces deberán mostrarse únicamente los presupuestos cuyo Responsable corresponda al usuario seleccionado. |
| Escenario: acceder a un presupuesto editable | Dado que un presupuesto se encuentra en Borrador, Para revisión interna o Solicitud de modificación Cuando el usuario lo selecciona Entonces puede acceder a su información y continuar trabajando de acuerdo con sus permisos. |
| Escenario: presupuesto no editable | Dado que un presupuesto se encuentra en Enviado, Aprobado, Rechazado, Vencido o Cancelado Cuando el usuario accede al presupuesto Entonces puede consultarlo, pero no modificarlo mediante el flujo normal. |
| Prioridad: | Alta |
| Dependencias: | Ninguna funcional |


## US-002 — Crear un presupuesto

Figura 8. Mockup US-002 — Crear un presupuesto

Tabla 12. Historia de usuario US-002 — Crear un presupuesto


| COMO: | Como Operador, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero crear un nuevo presupuesto |
| PARA: | Para comenzar a registrar y estimar un nuevo trabajo de EOP. |
| DESCRIPCIÓN: | El usuario puede comenzar esta acción desde la tarjeta Crear Presupuesto de Inicio o desde el botón correspondiente de la sección Presupuestos. Debe seleccionar un cliente y registrar el nombre del proyecto/trabajo, seleccionar un tipo de proyecto entre las opciones fijas definidas por la aplicación y registrar el plazo de vigencia expresado en días corridos. El usuario no deberá seleccionar un Responsable durante la creación. Al confirmar correctamente el alta, el sistema deberá asignar automáticamente como Responsable al usuario autenticado que creó el presupuesto. El presupuesto queda inicialmente en estado Borrador. |
| Permisos aplicables: | Por sus permisos base, Operador, Supervisor y Administrador pueden crear Presupuestos; no se admiten excepciones individuales; rigen los permisos fijos de RF-73. El usuario autenticado que realiza el alta queda automáticamente como Responsable, sin seleccionarlo manualmente. |
| Reglas de negocio: | Todo presupuesto debe estar asociado a un cliente. El usuario deberá seleccionar uno de los tipos de proyecto fijos previstos por la aplicación; no se administrarán desde el alta de presupuestos. Debe indicarse nombre del proyecto/trabajo. Debe registrarse un plazo de vigencia expresado en días corridos. El estado inicial es Borrador. Un cliente inactivo no puede seleccionarse para un nuevo presupuesto. Al crearse, cada presupuesto deberá recibir automáticamente un código interno único con formato PRES-AAAAMMDD-NOMBRE DEL PROYECTO-####, utilizando su fecha de creación, el nombre del proyecto y un correlativo global de presupuestos de cuatro cifras, independiente del de proyectos y sin reinicio por fecha, cliente, Tipo de proyecto ni nombre. El usuario no ingresará el código manualmente y las modificaciones posteriores del presupuesto no generarán uno nuevo. Todo presupuesto deberá contar con un Responsable. El Responsable deberá asignarse automáticamente al momento de crear el presupuesto. El Responsable asignado deberá corresponder al usuario autenticado que realizó la creación. El Responsable no deberá ser seleccionado manualmente durante el alta. El Responsable asignado deberá permanecer asociado al presupuesto como referencia de quién estuvo a cargo de su elaboración y seguimiento. |
| Requisitos relacionados: | RF: RF-01, RF-02, RF-87, RF-89. RNF: RNF-4, RNF-9, RNF-10, RNF-14. |
| Criterios de aceptación |  |
| Escenario: crear correctamente | Dado que el usuario completó correctamente los datos obligatorios Cuando confirma la creación Entonces deberá crearse el presupuesto en estado Borrador Y deberá asignarse automáticamente como Responsable al usuario autenticado que realizó su creación Y el sistema deberá generar automáticamente un código interno único utilizando la estructura PRES-AAAAMMDD-NOMBRE DEL PROYECTO-####. |
| Escenario: faltan datos obligatorios | Dado que falta uno o más datos obligatorios Cuando intenta crear el presupuesto Entonces no se crea Y el sistema deberá informar que existen datos obligatorios faltantes y permitir al usuario identificarlos. |
| Escenario: cliente inexistente | Dado que el cliente necesario no existe Cuando el usuario selecciona Crear cliente Entonces puede iniciar el registro de un cliente sin abandonar definitivamente el proceso de creación del presupuesto. |
| Escenario: nuevo cliente | Dado que el nuevo cliente fue registrado correctamente Cuando finaliza su alta Entonces queda inmediatamente disponible para seleccionarlo en el presupuesto actual. |
| Escenario: cliente inactivo | Dado que existe un cliente en estado Inactivo Cuando el usuario crea un nuevo presupuesto Entonces dicho cliente no deberá encontrarse disponible para selección. |
| Escenario: asignar Responsable automáticamente | Dado que un usuario autenticado está creando un nuevo presupuesto Y completó correctamente los datos obligatorios Cuando confirma la creación Entonces el sistema deberá crear el presupuesto Y deberá asignar automáticamente al usuario creador como Responsable del presupuesto. |
| Escenario: no seleccionar Responsable manualmente | Dado que el usuario está creando un nuevo presupuesto Cuando completa los datos necesarios para el alta Entonces no deberá requerirse que seleccione manualmente un Responsable Y el Responsable deberá determinarse automáticamente a partir del usuario autenticado que realiza la creación. |
| Prioridad: | Alta |
| Dependencias: | Tipos de proyecto fijos incluidos en la aplicación (catálogo fijo no administrable). |


## US-003 — Guardar avances del presupuesto

Figura 9. Mockup US-003 — Guardar avances del presupuesto

Tabla 13. Historia de usuario US-003 — Guardar avances del presupuesto


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero guardar explícitamente mi avance |
| PARA: | Para poder abandonar el presupuesto y continuar posteriormente sin perder lo guardado. |
| DESCRIPCIÓN: | El Presupuesto no utiliza guardado automático como sustituto de la acción Guardar borrador. El usuario conserva los datos y conceptos del presupuesto que haya guardado, incluidos los materiales, proveedores, precios y monedas históricos seleccionados. No existen cambios de catálogo pendientes dentro del presupuesto; los precios y relaciones se administran separadamente desde Catálogo de Materiales. Al abandonar con cambios sin guardar se ofrecerá Guardar, Descartar cambios o Continuar editando. |
| Permisos aplicables: | Por sus permisos base, el Operador podrá realizar esta acción sobre un Presupuesto del que sea Responsable; el Supervisor podrá hacerlo sobre cualquier Presupuesto que cumpla las condiciones funcionales y el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. |
| Reglas de negocio: | Solo se garantiza la conservación de información guardada. Guardar no modifica el estado. Los cambios sin guardar pueden descartarse. Antes de abandonar con cambios pendientes debe mostrarse una advertencia. Solo puede editarse en estados editables. |
| Requisitos relacionados: | RF: RF-05. RNF: RNF-4, RNF-10, RNF-14, RNF-19. |
| Criterios de aceptación |  |
| Escenario: Operador no Responsable sin acceso de edición | Dado un Operador que no es Responsable de un Presupuesto editable Cuando intenta modificarlo o guardar un cambio Entonces el servidor deniega la operación conforme a los permisos fijos, sin excepciones individuales. |
| Escenario: Operador Responsable autorizado por rol | Dado un Operador Responsable de un Presupuesto editable Cuando modifica sus conceptos y presiona Guardar borrador Entonces el sistema guarda los cambios permitidos sin alterar las referencias del catálogo. |
| Escenario: guardar | Dado que existen cambios en un presupuesto editable Cuando el usuario presiona Guardar borrador Entonces se guardan los cambios Y el presupuesto mantiene su estado actual. |
| Escenario: salir luego de guardar | Dado un Presupuesto cuyos últimos cambios ya fueron guardados Cuando el usuario abandona su elaboración y regresa posteriormente Entonces encuentra los conceptos, materiales, precios y monedas históricos guardados Y no existe una actualización pendiente del catálogo originada por el Presupuesto. |
| Escenario: salir con cambios pendientes | Dado que existen cambios posteriores al último guardado Cuando el usuario intenta abandonar la pantalla Entonces el sistema deberá mostrar una advertencia Y deberá ofrecer las opciones Guardar, Descartar cambios y Continuar editando. |
| Escenario: guardar desde la advertencia | Dado que se muestra la advertencia de cambios sin guardar Cuando el usuario selecciona Guardar Entonces los cambios deberán guardarse antes de abandonar la pantalla. |
| Escenario: descartar cambios | Dado que se muestra la advertencia de cambios sin guardar Cuando el usuario selecciona Descartar cambios Entonces deberán descartarse los cambios posteriores al último guardado Y el usuario podrá abandonar la pantalla. |
| Escenario: continuar editando | Dado que se muestra la advertencia de cambios sin guardar Cuando el usuario selecciona Continuar editando Entonces deberá permanecer en el presupuesto Y los cambios realizados deberán continuar disponibles para su edición. |
| Prioridad: | Alta |
| Dependencias: | US-002 |


## US-004 — Registrar relevamiento mediante texto o audio

Figura 10. Mockup US-004 — Registrar relevamiento mediante texto o audio

Tabla 14. Historia de usuario US-004 — Registrar relevamiento mediante texto o audio


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero registrar observaciones de relevamiento mediante texto o audio |
| PARA: | Para capturar la información obtenida durante la visita o análisis del trabajo. |
| DESCRIPCIÓN: | Dentro del presupuesto existe una etapa de Relevamiento. El usuario puede: escribir observaciones; grabar audio desde la aplicación; cargar un archivo previamente grabado. Los formatos aceptados son MP3, M4A, WAV y WEBM. El archivo de audio se utiliza para procesar la transcripción, pero no se conserva como documento permanente del presupuesto. |
| Permisos aplicables: | Por sus permisos base, el Operador podrá realizar esta acción sobre un Presupuesto del que sea Responsable; el Supervisor podrá hacerlo sobre cualquier Presupuesto que cumpla las condiciones funcionales y el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. |
| Reglas de negocio: | Las observaciones escritas forman un único contenido textual. La carga de audio permite trabajar con grabaciones realizadas previamente sin conexión. Solo se admiten los formatos establecidos (MP3, M4A, WAV y WEBM). Al grabar o cargar un audio, este deberá quedar identificado como pendiente de transcripción. Solo podrá existir un audio pendiente de transcripción o en procesamiento a la vez. Mientras exista un audio pendiente o en procesamiento, no podrá incorporarse otro audio. Un audio pendiente podrá quitarse antes de ser transcripto. Los audios no forman parte del repositorio documental permanente del presupuesto |
| Requisitos relacionados: | RF: RF-06, RF-07. RNF: RNF-4, RNF-9, RNF-10, RNF-13, RNF-14. |
| Criterios de aceptación |  |
| Escenario: ingresar texto | Dado que el presupuesto se encuentra editable Cuando el usuario escribe observaciones Entonces el contenido queda disponible dentro del relevamiento. |
| Escenario: grabar audio | Dado que no existe otro audio pendiente o en procesamiento Cuando el usuario utiliza la opción Grabar audio y finaliza la grabación Entonces el audio deberá quedar visible como pendiente de transcripción. |
| Escenario: subir audio | Dado que no existe otro audio pendiente o en procesamiento Y el usuario dispone de un archivo MP3, M4A, WAV o WEBM Cuando carga el archivo Entonces deberá quedar visible como pendiente de transcripción. |
| Escenario: formato inválido | Dado que el archivo tiene un formato no admitido Cuando intenta cargarlo Entonces el sistema lo rechaza Y el sistema deberá informar que el formato del archivo no se encuentra admitido. |
| Escenario: Quitar audio pendiente | Dado que existe un audio pendiente de transcripción Cuando el usuario selecciona Quitar Entonces el audio deberá descartarse Y no deberá incorporarse ningún contenido al relevamiento. |
| Escenario: Intentar incorporar otro audio | Dado que existe un audio pendiente de transcripción o en procesamiento Cuando el usuario intenta grabar o cargar otro audio Entonces el sistema no deberá permitir incorporar un nuevo audio. |
| Prioridad: | Alta |
| Dependencias: | US-002. |


## US-005 — Transcribir y revisar el relevamiento

Figura 11. Mockup US-005 — Transcribir y revisar el relevamiento

Tabla 15. Historia de usuario US-005 — Transcribir y revisar el relevamiento


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero transformar los audios en texto y corregir el contenido obtenido |
| PARA: | Para disponer de un relevamiento textual confiable antes de analizarlo. |
| DESCRIPCIÓN: | Cada audio incorporado quedará pendiente hasta que el usuario seleccione la acción Transcribir. Durante el procesamiento, el sistema deberá indicar visualmente que la transcripción se encuentra en curso. Cuando el proceso finalice correctamente, el texto obtenido se agregará al final del contenido existente sin eliminar las observaciones anteriores. Si la transcripción falla, el audio permanecerá disponible para reintentar el procesamiento o quitarlo. El usuario podrá editar libremente el contenido textual resultante antes de solicitar el análisis. |
| Permisos aplicables: | Por sus permisos base, el Operador podrá realizar esta acción sobre un Presupuesto del que sea Responsable; el Supervisor podrá hacerlo sobre cualquier Presupuesto que cumpla las condiciones funcionales y el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. |
| Reglas de negocio: | La transcripción se inicia mediante una acción explícita del usuario. Durante la transcripción deberá mostrarse claramente que el audio se encuentra en procesamiento. Toda transcripción correcta se agrega al final del relevamiento existente. Texto manual y transcripciones forman un mismo contenido editable. La versión corregida por el usuario pasa a ser la versión actual. Una transcripción fallida no deberá modificar el relevamiento existente. Si la transcripción falla, el audio permanecerá disponible para reintentar o quitar. Una vez completada correctamente la transcripción, el audio no se conservará como documentación permanente. |
| Requisitos relacionados: | RF: RF-08, RF-09, RF-10. RNF: RNF-4, RNF-11, RNF-12, RNF-13, RNF-20. |
| Criterios de aceptación |  |
| Escenario: Iniciar transcripción | Dado que existe un audio pendiente de transcripción Cuando el usuario selecciona Transcribir Entonces el sistema deberá iniciar su procesamiento Y deberá indicar visualmente que la transcripción se encuentra en curso. |
| Escenario: Transcripción correcta | Dado que existe texto previamente registrado en el relevamiento Y existe un audio pendiente de transcripción Cuando la transcripción finaliza correctamente Entonces el texto obtenido deberá agregarse al final del contenido existente Y el audio deberá dejar de encontrarse pendiente de transcripción. |
| Escenario: Falla de transcripción | Dado que existe un audio pendiente de transcripción Cuando la transcripción no puede completarse correctamente Entonces el contenido textual existente no deberá modificarse Y el audio deberá permanecer disponible Y el usuario deberá poder reintentar la transcripción o quitarlo. |
| Escenario: Reintentar transcripción | Dado que ocurrió un error durante la transcripción Cuando el usuario selecciona nuevamente Transcribir Entonces deberá realizarse un nuevo intento utilizando el mismo audio pendiente. |
| Escenario: Editar el relevamiento | Dado que el usuario detecta información incorrecta o incompleta en el texto del relevamiento Cuando modifica el contenido y guarda los cambios Entonces la versión corregida deberá conservarse como la versión actual del relevamiento. |
| Prioridad: | Alta |
| Dependencias: | US-004. |


## US-006 — Analizar el relevamiento y generar la tabla de presupuesto

Figura 12. Mockup US-006 — Analizar el relevamiento y generar la tabla de presupuesto

Tabla 16. Historia de usuario US-006 — Analizar el relevamiento y generar la tabla de presupuesto


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero analizar el relevamiento mediante IA |
| PARA: | Para obtener una primera estructura de los conceptos necesarios para elaborar el presupuesto. |
| DESCRIPCIÓN: | El usuario ejecuta una acción Analizar relevamiento. La IA utiliza el texto actual y genera una tabla editable con las columnas: Tipo de concepto \| Concepto detectado \| Concepto \| Cantidad \| Unidad \| Precio/Valor y moneda (cuando sea monetario) \| Subtotal y moneda (cuando sea monetario) Al final aparece una fila Total con importes en USD y UYU conforme a RF-29, sin sumar directamente subtotales de distintas monedas. Cada concepto conserva su precio o importe y su moneda; los porcentuales, como Margen, mantienen sus reglas propias. Los resultados de IA son propuestas y nunca información definitiva sin revisión humana. |
| Permisos aplicables: | Por sus permisos base, el Operador podrá realizar esta acción sobre un Presupuesto del que sea Responsable; el Supervisor podrá hacerlo sobre cualquier Presupuesto que cumpla las condiciones funcionales y el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. |
| Reglas de negocio: | Se analiza exclusivamente el relevamiento actual. El usuario puede modificar y eliminar los elementos propuestos. Los subtotales se calculan según el tipo de concepto, conservando la moneda USD o UYU de cada importe monetario; la fila Total muestra ambos resultados según RF-29, sin sumar nominalmente monedas distintas. Los porcentuales, como Margen, siguen sus reglas específicas. La IA no define precios económicos oficiales. No podrá iniciarse el análisis mientras exista un audio pendiente de transcripción o una transcripción en procesamiento. El campo Concepto detectado conservará la denominación identificada por la IA. Cuando el tipo sea distinto de Material, el campo Concepto tomará inicialmente el mismo valor que Concepto detectado y podrá ser posteriormente modificado. Cuando el tipo sea Material, la selección del campo Concepto se resolverá mediante la vinculación con el catálogo definida en US-007. |
| Requisitos relacionados: | RF: RF-11, RF-17, RF-18, RF-29. RNF: RNF-4, RNF-11, RNF-12, RNF-13, RNF-20. |
| Criterios de aceptación |  |
| Escenario: análisis | Dado que existe un relevamiento textual Cuando el usuario selecciona Analizar relevamiento Entonces el sistema presenta una tabla editable con los conceptos identificados, el precio o importe y la moneda USD o UYU de cada concepto monetario y una fila Total con ambos importes conforme a RF-29, sin sumar directamente monedas distintas. |
| Escenario: resultado IA | Dado que la IA identifica materiales, mano de obra u otros conceptos Cuando termina el procesamiento Entonces cada elemento aparece en una fila de la tabla Y se muestra el tipo de concepto identificado Y se conserva en Concepto detectado la denominación interpretada por la IA. |
| Escenario: modificar | Dado que un dato propuesto es incorrecto Cuando el usuario lo modifica Entonces la fila refleja el nuevo valor. |
| Escenario: eliminar fila | Dado que un concepto propuesto no corresponde Cuando el usuario lo elimina Entonces la fila se retira de la tabla sin requerir confirmación adicional. |
| Escenario: Audio pendiente | Dado que existe un audio pendiente de transcripción o en procesamiento Cuando el usuario intenta continuar con el análisis del relevamiento Entonces el sistema no deberá iniciar el análisis Y deberá requerir que el audio sea transcripto correctamente o quitado antes de continuar. |
| Prioridad: | Alta. |
| Dependencias: | US-005. |


## US-007 — Buscar y seleccionar materiales del catálogo manualmente

Figura 13. Mockup US-007 — Vincular materiales identificados con el catálogo

Tabla 17. Historia de usuario US-007 — Buscar y seleccionar materiales del catálogo manualmente


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero buscar y seleccionar manualmente materiales registrados en el catálogo para las filas de tipo Material del presupuesto. |
| PARA: | Para utilizar referencias existentes y evitar duplicados sin depender de una vinculación automática. |
| DESCRIPCIÓN: | El análisis con IA identifica el concepto detectado, que se conserva en su campo. Para vincularlo, el usuario abre el buscador del campo Concepto y selecciona expresamente un material existente del catálogo. La IA no busca ni selecciona candidatos automáticamente; si no existe el material deberá crearse previamente desde el catálogo por un rol autorizado y luego seleccionarse en el presupuesto. |
| Permisos aplicables: | Operador: solamente sobre Presupuestos de los que sea Responsable. Supervisor y Administrador: según los permisos base y las condiciones funcionales. Sin excepciones individuales (RF-73). |
| Reglas de negocio: | El campo Concepto detectado conservará el texto interpretado por la IA. El campo Concepto permanecerá sin material hasta la selección expresa del usuario. El buscador mostrará materiales existentes y permitirá elegir otro mientras el Presupuesto sea editable. Seleccionar no crea ni modifica materiales, proveedores ni referencias del catálogo. Los nuevos materiales se registrarán únicamente mediante US-041 desde el módulo Catálogo de Materiales. Se conserva la prohibición de combinar un mismo material y proveedor dos veces en un mismo presupuesto conforme a US-010A. |
| Requisitos relacionados: | RF: RF-11, RF-12, RF-13, RF-20, RF-67, RF-68, RF-73. RNF: RNF-4, RNF-10, RNF-14. |
| Criterios de aceptación |  |
| Escenario: material identificado sin asociación | Dado un relevamiento analizado que contiene una fila de tipo Material Cuando el sistema muestra esa fila Entonces Concepto detectado conserva la denominación identificada por IA Y Concepto no queda vinculado automáticamente al catálogo. |
| Escenario: búsqueda y selección manual | Dado que el usuario tiene permiso para editar el Presupuesto Cuando busca y selecciona un material existente desde el campo Concepto Entonces el material seleccionado se asocia a la fila del Presupuesto. |
| Escenario: sustituir selección | Dado un material seleccionado y un Presupuesto editable Cuando el usuario selecciona otro material del catálogo Entonces la fila utiliza el nuevo material, respetando las validaciones económicas y de duplicidad. |
| Escenario: sin material en el catálogo | Dado que la búsqueda manual no encuentra el material requerido Cuando el usuario intenta seleccionarlo Entonces no se crea automáticamente ningún material Y el nuevo material deberá registrarse previamente desde el módulo Catálogo conforme a US-041. |
| Escenario: sin permisos o presupuesto no editable | Dado un usuario sin autorización o un Presupuesto que no admite edición Cuando intenta vincular un material Entonces el servidor rechaza la modificación sin alterar el catálogo ni el Presupuesto. |
| Prioridad: | Alta. |
| Dependencias: | US-006 (análisis del relevamiento); US-041 (alta exclusiva desde catálogo); US-010A (referencias de precios). |


## US-008 — Registrar un material nuevo desde el presupuesto

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 14. Mockup US-008 — Registrar un material nuevo desde el presupuesto

Tabla 18. Historia de usuario US-008 — Registrar un material nuevo desde el presupuesto


| COMO: | Como Supervisor o Administrador, o como Operador con permiso individual específico conforme a RF-73 |
|---|---|
| QUIERO: | Quiero crear un material que no existe en el catálogo |
| PARA: | Para poder utilizarlo inmediatamente en el presupuesto y conservarlo para el futuro. |
| DESCRIPCIÓN: | Si un material identificado mediante inteligencia artificial no posee una correspondencia adecuada en el catálogo, el campo Concepto detectado conservará la denominación interpretada por la IA y el campo Concepto permanecerá sin material seleccionado. El usuario podrá seleccionar Crear material. Cuando el alta se origine a partir de un material detectado por IA, el formulario utilizará Concepto detectado como nombre inicial. El formulario parte de ese nombre y exige: nombre; Unidad de medida seleccionada obligatoriamente entre las Unidades registradas en la aplicación; proveedor; precio unitario. Para proveedores sin integración automática, el usuario deberá ingresar el precio unitario y su moneda (USD o UYU). Cuando el proveedor seleccionado corresponda a Fivisa, el sistema deberá habilitar el proceso de vinculación definido en US-056. Si la vinculación se valida correctamente, el precio unitario deberá ser el precio normal publicado obtenido automáticamente desde Fivisa mediante US-056. El nombre y la unidad de medida utilizados para crear el material serán los definidos por el usuario y no deberán ser sustituidos por los datos obtenidos desde Fivisa. |
| Permisos aplicables: | Por sus permisos base, el Operador puede buscar, consultar y seleccionar Materiales existentes, pero no registrar uno nuevo; Supervisor y Administrador sí pueden registrarlo. Solo un permiso individual específico concedido conforme a RF-73 podrá habilitar al Operador para el alta dentro de un Presupuesto que tenga permiso efectivo para elaborar, sin omitir las validaciones del Material. |
| Reglas de negocio: | La creación debe ser explícita. El nombre puede corregirse antes de guardar. Si su denominación completa coincide con la de un material del catálogo tras ignorar mayúsculas, minúsculas y espacios innecesarios, se impedirá su creación y se informará al usuario; nombres distintos no se equiparan necesariamente. Para registrar definitivamente el Material deberán encontrarse definidos nombre, proveedor, precio unitario estrictamente mayor que cero y moneda (USD o UYU), y deberá quedar registrada conjuntamente su primera relación MaterialProveedor válida; además, deberá seleccionarse obligatoriamente una Unidad registrada mediante un selector cargado con las Unidades disponibles. No podrá ingresarse libremente una Unidad inexistente; las nuevas Unidades creadas mediante US-063 quedarán inmediatamente disponibles para selección. Para proveedores sin integración automática, el precio deberá ser ingresado por el usuario; cuando corresponda a Fivisa, podrá obtenerse mediante US-056. El precio queda asociado a la combinación material-proveedor. La fecha de actualización se registra automáticamente. El Material queda disponible inmediatamente al confirmar su registro junto con la primera relación MaterialProveedor válida; si la relación no puede registrarse, no quedará registrado definitivamente el Material. Las confirmaciones diferidas de cambios en relaciones de materiales existentes se mantienen conforme a US-010A, para las modificaciones pendientes sobre referencias de precios existentes, y a US-010B, para las nuevas relaciones material-proveedor correspondientes a materiales ya registrados. Cuando el proveedor seleccionado corresponda a Fivisa, deberá habilitarse el proceso de vinculación definido en US-056. Una vinculación válida con Fivisa permitirá utilizar como precio inicial el precio normal publicado recuperado desde Fivisa mediante US-056. Los datos externos de Fivisa no modificarán el nombre ni la unidad del material definidos por el usuario. |
| Requisitos relacionados: | RF: RF-15, RF-22, RF-23, RF-92, RF-99, RF-100. RNF: RNF-10. |
| Criterios de aceptación |  |
| Escenario: registrar Material nuevo mediante excepción individual | Dado un Operador cuyo rol base no permite registrar Materiales nuevos Cuando un Administrador le concede el permiso individual específico y el Operador completa las validaciones vigentes Entonces podrá registrar el Material; sin ese permiso no podrá registrarlo. |
| Escenario: iniciar desde concepto detectado | Dado que la IA detectó un material para el cual no existe una correspondencia adecuada en el catálogo Cuando el usuario selecciona Crear material Entonces el formulario deberá utilizar como nombre inicial el contenido de Concepto detectado. |
| Escenario: modificar nombre | Dado que se abrió el formulario Cuando el usuario corrige el nombre Entonces el nuevo nombre se utiliza para registrar el material solo si no coincide, tras normalizar mayúsculas, minúsculas y espacios innecesarios, con una denominación completa existente; de coincidir, se impide el alta y se informa al usuario. |
| Escenario: faltan datos para proveedor sin integración | Dado que el proveedor seleccionado no utiliza integración automática Y falta nombre, unidad, proveedor, precio unitario o su moneda, o el precio ingresado es cero o negativo Cuando el usuario intenta guardar Entonces el material no se crea. |
| Escenario: creación correcta | Dado que un usuario autorizado para registrar un Material nuevo desde un Presupuesto que puede elaborar completó el nombre, seleccionó una Unidad registrada y un proveedor, y dispone de un precio unitario válido conforme a las reglas de ese proveedor Cuando confirma Entonces el material se crea Y queda seleccionado en el campo Concepto de la fila actual, junto con su primera relación MaterialProveedor válida y su precio positivo y moneda; si esa relación no puede registrarse, no se registra definitivamente el Material. |
| Escenario: crear material desde incorporación manual | Dado que el usuario está agregando manualmente un concepto de tipo Material Y no encuentra en el catálogo el material necesario Cuando selecciona Crear material Entonces podrá iniciar su registro sin abandonar el presupuesto. |
| Escenario: seleccionar Fivisa como proveedor | Dado que el usuario está creando un nuevo material Cuando selecciona Fivisa como proveedor Entonces el sistema deberá habilitar la opción de vincular un producto de Fivisa mediante US-056. |
| Escenario: creación con producto Fivisa validado | Dado que el usuario ingresó el nombre y seleccionó una Unidad registrada para el material Y seleccionó Fivisa como proveedor Y validó correctamente un producto mediante US-056 Cuando confirma la creación Entonces el material deberá registrarse con el nombre indicado y Unidad seleccionada por el usuario Y la relación con Fivisa deberá utilizar el precio normal recuperado, estrictamente mayor que cero, junto con la moneda USD o UYU seleccionada obligatoriamente por el usuario conforme a US-056, sin inferirla de la página durante la validación Y deberá quedar habilitada para actualización automática. |
| Escenario: seleccionar Unidad registrada | Dado que existen Unidades registradas Cuando el usuario inicia la creación de un nuevo material Entonces el campo Unidad deberá permitir seleccionar una de las Unidades disponibles Y no deberá permitir registrar el material con una Unidad inexistente. |
| Prioridad: | Alta |
| Dependencias: | US-007. |


## US-009 — Seleccionar proveedores existentes y registrarlos desde Proveedores

Figura 15. Mockup US-009 — Seleccionar o registrar proveedores

Tabla 19. Historia de usuario US-009 — Seleccionar proveedores existentes y registrarlos desde Proveedores


| COMO: | Como Supervisor o Administrador para registrar Proveedores, y como usuario autorizado para seleccionar Proveedores existentes. |
|---|---|
| QUIERO: | Quiero registrar proveedores desde una sección propia y seleccionar los ya registrados en presupuestos, materiales y facturas de compra. |
| PARA: | Para conservar un catálogo único sin permitir altas dispersas ni registros duplicados. |
| DESCRIPCIÓN: | El sistema contará con el módulo independiente Proveedores. Supervisor y Administrador podrán registrar nuevos Proveedores exclusivamente desde su opción de alta. El formulario conserva Nombre, RUT, Teléfono, correos y una o más cuentas bancarias con las validaciones de RF-23. En elaboración de presupuestos, asociación de materiales y registro de facturas de compra se seleccionan únicamente Proveedores existentes; si falta uno, deberá registrarse primero desde Proveedores. No habrá alta embebida ni extracción automática de RUT mediante OCR. |
| Permisos aplicables: | Supervisor y Administrador: alta de Proveedores desde el módulo Proveedores. Operador: consulta y selección de existentes cuando su operación lo admita, sin alta ni permiso individual excepcional. Las validaciones de servidor corresponden a RF-73. |
| Reglas de negocio: | El Nombre, RUT y Teléfono serán obligatorios; Nombre y RUT únicos. RUT validado y normalizado a dígitos con dígito verificador uruguayo; teléfono almacenado como texto internacional compatible con E.164. Al menos uno de los correos (contacto o facturación) válido y al menos una Cuenta bancaria completa (Banco fijo, Número, Titular, Moneda UYU/USD); pueden añadirse múltiples Cuentas al mismo Proveedor. Se impide guardar datos incompletos o duplicados, sin modificar registros anteriores. El alta solo se realiza desde Proveedores con confirmación expresa. No se crean ni vinculan automáticamente Materiales ni referencias a Fivisa. |
| Requisitos relacionados: | RF: RF-23, RF-73, RF-102, RF-109. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: selección de Proveedor existente | Dado un usuario autorizado que necesita seleccionar un Proveedor en un presupuesto, material o factura de compra Cuando abre el selector Entonces se muestran únicamente Proveedores registrados Y puede elegir uno sin crear otro. |
| Escenario: Proveedor no registrado en otro módulo | Dado que el Proveedor requerido no existe Cuando el usuario utiliza el selector desde presupuestación, materiales o facturas Entonces no aparece una acción de alta en ese formulario Y debe registrarse previamente desde la sección Proveedores. |
| Escenario: alta de Proveedor desde sección propia | Dado un Supervisor o Administrador en Proveedores Cuando completa Nombre, RUT válido, Teléfono, al menos un correo válido y una Cuenta completa y confirma Entonces se crea el Proveedor y queda disponible para ser seleccionado en otros módulos. |
| Escenario: proteger alta frente a Operador | Dado un Operador sin permiso base de alta de Proveedores Cuando intenta registrar un Proveedor incluso mediante URL directa Entonces el servidor deniega la operación sin registrar datos. |
| Escenario: RUT inválido y duplicado | Dado un RUT con dígito verificador inválido o un RUT normalizado ya existente Cuando se intenta confirmar el alta Entonces se rechaza y se informa el motivo, conservando los datos válidos de los Proveedores ya registrados. |
| Escenario: Nombre duplicado | Dado otro Proveedor con igual nombre normalizado, aunque cambie el uso de mayúsculas, espacios o tildes Cuando se intenta el alta Entonces no se crea otro Proveedor y se informa el duplicado. |
| Escenario: Teléfono inválido | Dado que el Teléfono obligatorio no cumple el formato internacional previsto Cuando se intenta registrar el Proveedor Entonces no se permite guardar y se indica el dato inválido. |
| Escenario: ambos correos vacíos | Dado que faltan Correo de contacto y Correo de facturación Cuando se confirma el alta Entonces el sistema requiere al menos uno válido. |
| Escenario: correo individual válido | Dado que los demás campos son válidos y se informó únicamente uno de los dos correos Cuando se confirma el alta Entonces se registra el Proveedor conservando el correo informado. |
| Escenario: cuenta obligatoria e incompleta | Dado un formulario sin Cuentas o con una Cuenta sin Banco, Número, Titular o Moneda Cuando se intenta confirmar el alta Entonces el sistema impide registrar el Proveedor hasta contar con una Cuenta completa. |
| Escenario: opciones bancarias y moneda | Dado el formulario de Cuenta bancaria Cuando se seleccionan Banco y Moneda Entonces se muestran únicamente los bancos fijos definidos en RF-23 y las monedas UYU y USD. |
| Escenario: múltiples Cuentas | Dado un Proveedor en alta con una Cuenta completa Cuando se agrega otra Cuenta válida Entonces ambas quedan relacionadas con el mismo Proveedor sin duplicarlo. |
| Escenario: Proveedor registrado disponible | Dado que se confirmó correctamente el alta en Proveedores Cuando un usuario vuelve a seleccionar un Proveedor desde un presupuesto o una factura Entonces el Proveedor nuevo está disponible en el selector existente. |
| Escenario: dos correos válidos al registrar Proveedor | Dado un Supervisor o Administrador registrando un Proveedor desde el módulo Proveedores con Correo de contacto y Correo de facturación válidos Cuando confirma los datos obligatorios y al menos una Cuenta bancaria válida Entonces ambos correos se almacenan en campos independientes y el Proveedor queda disponible para su selección. |
| Prioridad: | Alta. |
| Dependencias: | US-074 (acceso al módulo Proveedores); US-066 (Cuentas bancarias adicionales); US-072 (edición). |


## US-010A — Seleccionar referencias de precios y conservar sus valores históricos

(10A y 10B)

Figura 16. Mockup US-010A y US-010B — Consultar, seleccionar y actualizar referencias de precios existentes y registrar nuevas relaciones entre materiales y proveedores

Tabla 20. Historia de usuario US-010A — Seleccionar referencias de precios y conservar sus valores históricos


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero elegir una referencia de Proveedor existente para un Material seleccionado en el presupuesto. |
| PARA: | Para copiar al Presupuesto el nombre del material, su unidad, el precio unitario y la moneda, conservándolos históricamente. |
| DESCRIPCIÓN: | Para un Material previamente seleccionado del catálogo se muestran sus relaciones material-proveedor con precio, moneda y fecha de actualización. El usuario selecciona manualmente una referencia existente; el sistema copia al concepto del presupuesto el nombre del material, su unidad, el precio unitario y la moneda, y los conserva como valores históricos. No se editan desde la tabla ni se crean cambios pendientes para el catálogo. Las referencias existentes se mantienen mediante US-071 y las nuevas se crean mediante US-010B, exclusivamente en el catálogo. |
| Permisos aplicables: | Operador Responsable del Presupuesto, Supervisor y Administrador según RF-73. No existen concesiones ni excepciones individuales. |
| Reglas de negocio: | Cada relación material-proveedor conserva su precio mayor que cero, moneda UYU/USD y fecha de actualización. Toda selección debe ser expresa, incluso cuando haya un solo proveedor. El nombre del material, la unidad, el precio unitario y la moneda copiados quedan propios del Presupuesto y no se modifican retroactivamente por cambios en el catálogo. Desde la tabla no pueden editarse los valores copiados ni modificar el catálogo. Se impiden dos filas de Material con la misma combinación material-proveedor dentro del mismo Presupuesto, admitiéndose el mismo Material con Proveedores distintos. La verificación se realiza al seleccionar o modificar el material o el proveedor de cualquier fila. |
| Requisitos relacionados: | RF: RF-19, RF-20, RF-21, RF-22, RF-73, RF-108. RNF: RNF-4, RNF-10. |
| Criterios de aceptación |  |
| Escenario: referencias disponibles | Dado un Material del catálogo ya elegido Cuando se consultan sus referencias Entonces se muestran Proveedor, precio, moneda y fecha de última actualización de cada una. |
| Escenario: selección manual de referencia | Dado un Material con una o más referencias Cuando el usuario selecciona expresamente una relación Material-Proveedor Entonces se copian al concepto del Presupuesto el nombre del material, su unidad, el precio unitario y la moneda. |
| Escenario: no editar precio en tabla | Dado un concepto con nombre, unidad, precio y moneda copiados de una referencia Cuando se consulta o modifica su fila en la tabla presupuestal Entonces esos valores no se pueden editar directamente desde allí. |
| Escenario: conservar historia económica | Dado un concepto con nombre, unidad, precio y moneda ya guardados Cuando luego se actualiza en el catálogo el nombre o la unidad del material, o el precio o la moneda de la referencia Entonces el Presupuesto conserva sus valores históricos anteriores. |
| Escenario: referencia única | Dado un Material con una sola referencia Cuando se presenta la selección Entonces el usuario debe confirmarla explícitamente y no se preselecciona automáticamente. |
| Escenario: combinación duplicada | Dado un Presupuesto con una fila asociada a Material X y Proveedor Y Cuando se intenta confirmar otra fila con la misma combinación Entonces se rechaza el duplicado y se indica utilizar la fila existente. |
| Escenario: modificación desde catálogo | Dado una referencia cuyo precio necesita actualización Cuando el usuario quiere cambiar el valor en el catálogo Entonces la modificación se realiza mediante US-071 y no desde el Presupuesto. |
| Prioridad: | Alta. |
| Dependencias: | US-007 (selección de Material); US-071 (mantenimiento de referencias existentes); US-010B (nuevas relaciones). |


## US-010B — Agregar nuevas relaciones material-proveedor desde catálogo

Tabla 21. Historia de usuario US-010B — Agregar nuevas relaciones material-proveedor desde catálogo


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero registrar desde el Catálogo de Materiales una nueva relación entre un material existente y un proveedor previamente registrado con el cual todavía no exista una relación registrada |
| PARA: | Para incorporar al catálogo la referencia de precio de ese proveedor y disponer de ella posteriormente en los presupuestos. |
| DESCRIPCIÓN: | Desde el Catálogo de Materiales, un usuario autorizado selecciona un Material existente y elige Agregar proveedor. El formulario permite elegir exclusivamente un Proveedor registrado e ingresar manualmente precio mayor que cero y moneda UYU o USD. No se pueden crear Proveedores en el formulario. Si el Proveedor falta se registra previamente desde Proveedores. Se verifica unicidad Material-Proveedor, se guarda la relación y la fecha de actualización; la selección posterior desde presupuestos utiliza US-010A. No existe vinculación ni sincronización automática con Fivisa. |
| Permisos aplicables: | Solamente Supervisor y Administrador podrán crear relaciones material-proveedor en el catálogo, según la matriz fija de RF-73. Operador: consulta y selección de referencias existentes. |
| Reglas de negocio: | El material debe existir en el catálogo. La creación de Material y primera relación se rige por US-041. El Proveedor deberá existir previamente. La relación material-proveedor debe ser única, precio mayor que cero y moneda UYU/USD obligatoria; se registra fecha de actualización al guardar. Si falla una validación o guardado, no se crea una relación parcial. El registro no se inicia desde presupuestos ni crea cambios pendientes en ellos. El precio se ingresa manualmente también si el proveedor es Fivisa; no hay conexión automática con tiendas externas. |
| Requisitos relacionados: | RF: RF-22, RF-23, RF-67, RF-73. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: agregar relación desde catálogo | Dado un Supervisor o Administrador que seleccionó un Material existente en el Catálogo Cuando elige Agregar proveedor Entonces puede elegir un Proveedor registrado e ingresar precio y moneda para una nueva relación. |
| Escenario: proveedor inexistente | Dado que el proveedor requerido no está registrado Cuando se abre Agregar proveedor desde el Material Entonces no se permite crear un Proveedor en este formulario Y debe registrarse primero desde la sección Proveedores. |
| Escenario: relación duplicada | Dado que Material y Proveedor ya están relacionados Cuando intenta agregarse nuevamente la misma combinación Entonces se informa el duplicado y no se crea otra relación. |
| Escenario: precio o moneda inválidos | Dado un precio nulo, cero, negativo o moneda diferente de UYU/USD Cuando el usuario intenta confirmar Entonces se rechaza la relación sin registrar cambios parciales. |
| Escenario: alta válida | Dado una combinación nueva y válida con precio y moneda Cuando Supervisor o Administrador confirma Entonces se registra la relación con fecha de actualización Y queda disponible para futuras selecciones de presupuestos. |
| Escenario: Fivisa sin integración | Dado que el proveedor seleccionado es Fivisa Cuando se registra la nueva relación Entonces el precio se ingresa manualmente y no se conecta con productos externos. |
| Escenario: Operador sin autorización | Dado un Operador consultando un material Cuando intenta crear una relación nueva incluso por solicitud directa Entonces el servidor deniega el alta sin crear datos. |
| Prioridad: | Alta. |
| Dependencias: | US-040 (catálogo); US-041 (alta de material y primera referencia); US-074 (módulo de Proveedores); US-010A (selección posterior). |


## US-011 — Agregar manualmente conceptos al presupuesto

Figura 17. Mockup US-011 — Agregar manualmente conceptos al presupuesto

Tabla 22. Historia de usuario US-011 — Agregar manualmente conceptos al presupuesto


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero agregar manualmente conceptos de diferentes tipos a la tabla del presupuesto |
| PARA: | Para incorporar elementos necesarios que no hayan sido incluidos durante el análisis del relevamiento. |
| DESCRIPCIÓN: | El usuario podrá agregar manualmente una nueva fila a la tabla del presupuesto seleccionando previamente el tipo de concepto. Al tratarse de una incorporación manual y no de un elemento obtenido durante el análisis mediante inteligencia artificial, el campo Concepto detectado no aplicará. Si selecciona Material, deberá utilizar el campo Concepto para buscar y seleccionar un material previamente registrado en el catálogo de EOP. Si el material necesario no existe, deberá registrarse previamente desde el módulo Catálogo, según US-041; no habrá alta desde la tabla del presupuesto. Para conceptos con importe manual, como Mano de obra, Alquiler, Traslado o Costos indirectos, el usuario completará directamente el campo Concepto y los demás datos correspondientes al tipo seleccionado. Si selecciona Margen, deberá ingresar el porcentaje correspondiente. El sistema deberá mostrar los tipos de conceptos presentes en el presupuesto junto con la suma de sus respectivos subtotales separada por moneda y permitir seleccionar uno o más de dichos tipos como base para calcular el margen en UYU de acuerdo con RF-27. Si el presupuesto ya posee un concepto Margen, el sistema no deberá permitir agregar un segundo margen. |
| Permisos aplicables: | Por sus permisos base, el Operador podrá realizar esta acción sobre un Presupuesto del que sea Responsable; el Supervisor podrá hacerlo sobre cualquier Presupuesto que cumpla las condiciones funcionales y el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. Los Materiales nuevos se registran exclusivamente desde el Catálogo mediante US-041, con sus autorizaciones propias; esta acción no crea Materiales. |
| Reglas de negocio: | Todo concepto deberá poseer un tipo. El tipo deberá seleccionarse entre los tipos previamente disponibles. En una fila agregada manualmente, Concepto detectado no aplicará. Si el tipo seleccionado es Material, el material elegido del catálogo deberá registrarse en Concepto. Para conceptos con importe manual, el usuario seleccionará USD o UYU y se conservarán el importe original y la moneda, utilizando ese importe como subtotal sin cotización histórica individual. Cantidad y unidad no serán obligatorias cuando no correspondan al tipo seleccionado. Margen deberá encontrarse disponible como tipo de concepto únicamente cuando el presupuesto todavía no posea un margen registrado. Un presupuesto podrá contener como máximo un único concepto de tipo Margen. Cuando se seleccione Margen, el usuario deberá ingresar el porcentaje correspondiente y seleccionar uno o más tipos de conceptos como base de cálculo. Para cada tipo disponible como base, el sistema deberá mostrar la suma actual de sus subtotales por moneda, sin sumar nominalmente USD y UYU. El subtotal del margen deberá calcularse y expresarse siempre en UYU de acuerdo con RF-27, convirtiendo primero a UYU los importes USD de la base seleccionada con la cotización única actual de RF-29, incluso si la base solo contiene USD; no deberá ser ingresado manualmente por el usuario. El concepto Margen no deberá estar disponible como base de cálculo. Cuando el concepto agregado manualmente sea de tipo Material, la selección del material y proveedor deberá respetar la restricción de unicidad definida en RF-20. No se podrá incorporar una combinación material-proveedor ya utilizada en otra fila del mismo presupuesto. |
| Requisitos relacionados: | RF: RF-14, RF-16, RF-24, RF-25, RF-26, RF-27, RF-29. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: agregar concepto | Dado que el usuario se encuentra elaborando un presupuesto Cuando selecciona Agregar concepto Entonces deberá poder seleccionar uno de los tipos de concepto disponibles Y completar los datos aplicables al tipo seleccionado. |
| Escenario: importe manual | Dado que el tipo seleccionado utiliza un importe directo Cuando el usuario registra un importe válido Entonces el usuario podrá seleccionar USD o UYU y el importe y la moneda elegida se conservarán como subtotal de esa fila, sin cotización histórica individual. |
| Escenario: campos no aplicables | Dado que el tipo seleccionado no requiere cantidad ni unidad Cuando se muestra el formulario correspondiente Entonces dichos campos no deberán ser obligatorios y podrán mostrarse deshabilitados. |
| Escenario: agregar material existente | Dado que el usuario selecciona Agregar concepto Y selecciona el tipo Material Cuando busca y selecciona un material existente del catálogo Entonces dicho material deberá registrarse en Concepto Y Concepto detectado no deberá contener información generada por inteligencia artificial. |
| Escenario: impedir agregar manualmente un material duplicado | Dado un presupuesto que ya contiene un material asociado a un proveedor Cuando el usuario intenta agregar manualmente otra fila con el mismo material y proveedor Entonces el sistema deberá impedir registrar la combinación duplicada Y deberá permitir conservar o modificar la fila existente. |
| Escenario: agregar concepto no material con importe manual | Dado que el usuario selecciona Agregar concepto Y selecciona un tipo distinto de Material y Margen Cuando completa la descripción y el importe correspondientes Entonces dicha información deberá registrarse en Concepto Y Concepto detectado no aplicará. |
| Escenario: seleccionar Margen | Dado que el presupuesto contiene conceptos registrados Y todavía no posee un concepto de tipo Margen Cuando el usuario selecciona Agregar concepto Y selecciona el tipo Margen Entonces el sistema deberá solicitar el porcentaje de margen Y deberá mostrar los distintos tipos de conceptos presentes en el presupuesto Y deberá mostrar para cada tipo la suma de sus subtotales separada por moneda Y deberá permitir seleccionar uno o más tipos como base de cálculo. |
| Escenario: seleccionar varios tipos como base | Dado que los conceptos de tipo Material suman USD 100 Y los conceptos de tipo Alquiler suman UYU 2.000 Y la cotización única actual es 40 UYU/USD y el usuario ingresó un margen de 20 % Cuando selecciona Material y Alquiler como base Entonces la base de cálculo deberá ser UYU 6.000 y el importe del Margen deberá ser UYU 1.200, aplicando la cotización actual de 40 UYU/USD; si luego se modifica el porcentaje, el importe del Margen en UYU se recalculará conforme a RF-30. |
| Escenario: impedir segundo margen | Dado que el presupuesto ya posee un concepto de tipo Margen Cuando el usuario intenta agregar otro concepto de tipo Margen Entonces el sistema deberá impedir la operación. |
| Prioridad: | Alta. |
| Dependencias: | US-006. |


## US-012 — Calcular subtotales y total del presupuesto

Tabla 23. Historia de usuario US-012 — Calcular subtotales y total del presupuesto


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero que el sistema calcule automáticamente los importes |
| PARA: | Para disponer de un presupuesto económico consistente. |
| DESCRIPCIÓN: | Para materiales: Subtotal = Cantidad × Precio unitario. Para conceptos con importe manual: Subtotal = Importe ingresado. Para el concepto Margen, el sistema deberá calcular primero la base correspondiente a partir de los tipos seleccionados: Base de cálculo del margen en UYU = suma de los subtotales UYU de los tipos seleccionados + (suma de sus subtotales USD × cotización única actual UYU/USD de RF-29). Posteriormente: Subtotal Margen en UYU = Base de cálculo en UYU × Porcentaje de margen / 100. La fila Total deberá mostrar simultáneamente UYU y USD: Subtotal USD sumará los conceptos expresados en USD y Subtotal UYU los expresados en UYU, incluido siempre el Margen calculado en UYU, aun con base mixta o exclusivamente en USD. Con una única cotización actual UYU/USD de datosUruguay, Total UYU = Subtotal UYU + (Subtotal USD × Cotización) y Total USD = Subtotal USD + (Subtotal UYU ÷ Cotización). El presupuesto podrá contener como máximo un único margen. |
| Permisos aplicables: | Los cálculos son automáticos; Operador Responsable, Supervisor y Administrador pueden gestionar los datos que los originan en sus ámbitos según RF-73, sin excepciones individuales. |
| Reglas de negocio: | Los cálculos y recálculos son automáticos y mantienen sus reglas matemáticas; por sus permisos base, el Operador Responsable, el Supervisor y el Administrador pueden gestionar los datos que los originan conforme a sus respectivos alcances. No se admiten excepciones individuales; los permisos fijos de RF-73 no alteran las fórmulas ni los cálculos. Los subtotales deberán calcularse según las reglas correspondientes al tipo de concepto. Para calcular el margen, el sistema deberá utilizar únicamente los tipos de conceptos seleccionados por el usuario como su base. Para cada tipo seleccionado como base, deberán considerarse todos los conceptos pertenecientes a dicho tipo que formen parte actualmente del presupuesto. La base del margen deberá respetar los importes y monedas originales de los conceptos de los tipos seleccionados; convertirá primero a UYU los importes USD mediante la única cotización actual de RF-29, incluso si todos los conceptos seleccionados están en USD, y luego sumará los importes en UYU. No sumará nominalmente monedas distintas; el subtotal del Margen se expresará siempre en UYU. El subtotal del margen deberá calcularse aplicando el porcentaje ingresado sobre la base calculada. Si cambia un valor que afecte a alguno de los tipos seleccionados como base, deberán recalcularse automáticamente la base y el subtotal del margen. Los conceptos correspondientes a tipos no seleccionados como base no deberán intervenir en el cálculo del margen. Un presupuesto podrá contener como máximo un único concepto de tipo Margen. El concepto Margen no deberá utilizarse como base para calcular otro margen. Todo cambio en un dato económico deberá provocar los recálculos que correspondan antes de mostrar los nuevos totales en USD y UYU con una única cotización actual. Los subtotales por moneda y los totales en USD y UYU deberán reflejar los valores actualmente registrados durante la elaboración; los importes y la cotización conservados al confirmar los datos económicos (US-013) serán los mismos que se registrarán al enviarla, sin recalcular por el mero tiempo transcurrido. Los cálculos determinísticos de subtotales, bases, margen y total no deberán delegarse a inteligencia artificial. La conversión entre USD y UYU utilizará la cotización diaria BCU obtenida mediante datosUruguay API. Si no existe publicación para la fecha actual, se utilizará la última cotización válida anterior, conforme a RF-29. Los totales finales en USD y UYU deberán mostrarse con dos decimales, aplicando redondeo matemático habitual, sin redondear anticipadamente los valores utilizados en los cálculos ni modificar los importes originales de los conceptos. Si no se dispone de una cotización válida para realizar las conversiones necesarias, no se mostrarán los totales convertidos como definitivos ni se permitirá confirmar los datos económicos con esos totales, aunque el presupuesto podrá conservarse como borrador. |
| Requisitos relacionados: | RF: RF-27, RF-28, RF-29, RF-30. RNF: RNF-10, RNF-11, RNF-12, RNF-13, RNF-18. |
| Criterios de aceptación |  |
| Escenario: calcular subtotal de material | Dado un material con cantidad 10 Y precio unitario 20 Cuando el sistema calcula su importe Entonces el subtotal mostrado deberá ser 200 en la moneda propia del precio de ese concepto, aunque luego cambie el precio del catálogo. |
| Escenario: calcular concepto con importe manual | Dado un concepto de Mano de obra con importe 1.000 Cuando el sistema calcula su importe Entonces el subtotal mostrado deberá ser 1.000. |
| Escenario: calcular margen sobre un tipo | Dado que los conceptos de tipo Material suman USD 100 y la cotización actual es 40 UYU/USD Y el usuario ingresó un margen de 20 % Y seleccionó Material como base Cuando el sistema calcula el margen Entonces la base deberá ser UYU 4.000 Y el subtotal del Margen deberá ser UYU 800. |
| Escenario: calcular margen sobre varios tipos | Dado que los conceptos de tipo Material suman USD 100 y la cotización actual es 40 UYU/USD Y los conceptos de tipo Alquiler suman UYU 2.000 Y el usuario ingresó un margen de 20 % Cuando selecciona Material y Alquiler como base Entonces la base deberá ser UYU 6.000 Y el subtotal del Margen deberá ser UYU 1.200, que se agrega a Subtotal UYU; con esos conceptos, Total UYU deberá ser 7.200 y Total USD 180. |
| Escenario: excluir tipo no seleccionado | Dado que existen conceptos de Material, Mano de obra y Alquiler Y el usuario seleccionó únicamente Material y Alquiler como base del margen Cuando el sistema calcula el margen Entonces los subtotales correspondientes a Mano de obra no deberán formar parte de la base. |
| Escenario: recalcular margen por modificación de su base | Dado que existe un margen calculado sobre Material y Alquiler Cuando se modifica la cantidad, precio o importe de un concepto perteneciente a Material o Alquiler Entonces el sistema deberá recalcular la suma correspondiente al tipo afectado Y deberá recalcular la base del margen Y deberá recalcular el subtotal del Margen Y deberá actualizar ambos totales conforme a RF-29; si la base incluye USD, el recálculo del margen en UYU utilizará la cotización única actual. |
| Escenario: modificar concepto no incluido en la base | Dado que existe un margen calculado únicamente sobre Material Cuando se modifica un concepto de Mano de obra que no fue seleccionado como base Entonces el subtotal del Margen no deberá modificarse Y el Total del presupuesto deberá actualizarse si cambió el subtotal de Mano de obra. |
| Escenario: impedir más de un margen | Dado que el presupuesto ya posee un concepto de tipo Margen Cuando se intenta incorporar otro concepto de tipo Margen Entonces el sistema deberá impedir la operación. |
| Escenario: consolidar subtotales y totales en ambas monedas | Dado un Presupuesto con Subtotal USD 100, Subtotal UYU 2.000 y una única cotización actual de 40 UYU por USD obtenida mediante datosUruguay API Cuando el sistema calcula los totales generales Entonces Total UYU es 6.000 y Total USD es 150, aplicando respectivamente Subtotal UYU + (Subtotal USD × Cotización) y Subtotal USD + (Subtotal UYU ÷ Cotización); durante la elaboración un cambio en la cotización actual recalcula ambos totales, sin utilizar cotizaciones históricas individuales de conceptos. |
| Escenario: utilizar última cotización publicada | Dado que el usuario está elaborando un presupuesto durante un día sin cotización BCU publicada Y existe una cotización válida correspondiente a un día anterior Cuando el sistema calcula los totales del presupuesto Entonces deberá utilizar la última cotización válida anterior Y deberá identificar la fecha y el valor utilizados. |
| Escenario: redondear totales monetarios | Dado un presupuesto cuyos cálculos generan importes con más de dos decimales Cuando el sistema presenta los totales finales en USD y UYU Entonces deberá mostrarlos redondeados a dos decimales mediante redondeo matemático habitual Y deberá conservar la precisión original durante los cálculos intermedios. |
| Escenario: cotización no disponible | Dado que el sistema necesita realizar conversiones monetarias Y no dispone de una cotización válida Cuando el usuario solicita calcular los totales o confirmar los datos económicos Entonces el sistema deberá informar que la cotización no está disponible Y no deberá presentar totales convertidos como definitivos ni permitir confirmar los datos económicos Y deberá permitir guardar el presupuesto como borrador. |
| Prioridad: | Alta. |
| Dependencias: | US-010A, US-011. US-010B solo cuando se utilicen nuevas relaciones material-proveedor; no es requisito para calcular sobre referencias existentes. |


## US-013 — Consultar y confirmar el resumen

Figura 18. Mockup US-013 — Consultar y confirmar el resumen

Tabla 24. Historia de usuario US-013 — Consultar y confirmar el resumen


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero revisar un resumen completo antes de generar la documentación |
| PARA: | Para validar los datos técnicos y económicos. |
| DESCRIPCIÓN: | El usuario consulta el resumen técnico y económico del Presupuesto antes de continuar. El resumen muestra conceptos, cantidades, precios y monedas históricos, proveedores, margen y totales USD/UYU. Puede volver a editar o seleccionar Confirmar datos y continuar. La confirmación guarda los datos del Presupuesto y pasa a Documentos; no aplica cambios al catálogo ni registra relaciones nuevas. El PDF comercial se elabora externamente y, si los datos económicos cambian después de su carga, debe verificarse nuevamente su vigencia. |
| Permisos aplicables: | Operador Responsable del Presupuesto, Supervisor y Administrador conforme a los permisos fijos de RF-73; sin excepciones individuales. |
| Reglas de negocio: | Consultar el resumen no cambia datos ni estado. Confirmar guarda los valores y permite continuar hacia Documentos, sin pasar a revisión interna. La confirmación NO actualiza precios o monedas del catálogo ni crea relaciones pendientes. La actualización de referencias y la creación de relaciones se hacen exclusivamente desde el catálogo. Los datos económicos modificados después de cargar el PDF final obligan a revisar su vigencia según RF-44; no se genera ni actualiza ningún Word mediante IA. Al confirmar se conservarán los Totales USD y UYU, la cotización aplicada y su fecha de publicación; son los valores que debe reflejar el PDF comercial y los que se conservarán al registrar el envío. Sin una cotización válida no se permitirá confirmar. |
| Requisitos relacionados: | RF: RF-20, RF-29, RF-30, RF-31, RF-44. RNF: RNF-4, RNF-10, RNF-19. |
| Criterios de aceptación |  |
| Escenario: consultar resumen | Dado un Presupuesto con conceptos económicos registrados Cuando el usuario abre el resumen Entonces se muestran conceptos, referencias, importes, monedas, margen y totales que corresponden. |
| Escenario: volver a editar | Dado un Presupuesto que admite modificación Cuando el usuario regresa desde el resumen a edición Entonces puede corregir sus datos sin cambiar el estado por ese solo hecho. |
| Escenario: confirmar sin cambiar catálogo | Dado un Presupuesto válido para confirmar Cuando se selecciona Confirmar datos y continuar Entonces se guardan los datos y se accede a Documentos Y no se actualizan precios, monedas ni se crean relaciones material-proveedor en el catálogo. |
| Escenario: presupuesto sin datos válidos | Dado un Presupuesto con errores que impiden la confirmación Cuando el usuario confirma Entonces se informa el error y no se producen cambios parciales. |
| Escenario: PDF comercial desactualizado | Dado que ya existe un PDF comercial cargado Cuando se modifican y confirman datos económicos Entonces la vigencia del PDF deberá revisarse conforme a US-016, antes de revisión o envío. |
| Escenario: conservar totales y cotización al confirmar | Dado un Presupuesto con totales calculados y una cotización válida Cuando el usuario selecciona Confirmar datos y continuar Entonces se guardan los Totales USD y UYU, la cotización y su fecha de publicación Y esos valores no cambian por nuevas publicaciones de la cotización hasta una nueva confirmación. |
| Escenario: confirmar sin cotización válida | Dado que no se dispone de una cotización válida Cuando el usuario selecciona Confirmar datos y continuar Entonces el sistema informa que la cotización no está disponible Y no confirma los datos económicos, aunque el presupuesto puede guardarse como borrador. |
| Prioridad: | Alta. |
| Dependencias: | US-012 (cálculos); US-016 (PDF comercial final). |


## US-014 — Generar y descargar la planilla Excel

Figura 19. Mockup US-014 — Generar y descargar la planilla Excel

Tabla 25. Historia de usuario US-014 — Generar y descargar la planilla Excel


| COMO: | Como Operador, Supervisor o Administrador con acceso al Presupuesto |
|---|---|
| QUIERO: | Quiero generar la planilla económica en Excel |
| PARA: | Para disponer del detalle interno del presupuesto. |
| DESCRIPCIÓN: | Desde Documentos del Presupuesto se permitirá seleccionar Generar Excel y descargar inmediatamente un archivo .xlsx elaborado a partir de los datos técnicos y económicos del Presupuesto. Cada solicitud generará el archivo usando los valores que correspondan en ese momento; no se conservarán versiones ni una copia almacenada del Excel dentro de la aplicación. El formato exacto de la planilla seguirá sujeto a validación con EOP. |
| Permisos aplicables: | Operador, Supervisor y Administrador con acceso al Presupuesto, conforme a los permisos fijos de RF-73; generar y descargar el Excel es una consulta de solo lectura que no modifica el Presupuesto. |
| Reglas de negocio: | El Excel se generará y descargará a demanda. Los importes, monedas, proveedores y demás conceptos deberán corresponder al Presupuesto, conservando precios históricos. El archivo no se almacenará ni versionará en Azure Blob Storage como copia del Presupuesto. Se permitirá volver a generar y descargar, existan o no cambios recientes, sin afectar el estado del Presupuesto. La estructura exacta se validará con EOP. |
| Requisitos relacionados: | RF: RF-29, RF-37, RF-38. RNF: RNF-4, RNF-10, RNF-20. |
| Criterios de aceptación |  |
| Escenario: generar Excel a demanda | Dado un Presupuesto con información válida Cuando el usuario autorizado selecciona Generar Excel Entonces se genera un archivo .xlsx con la información del Presupuesto disponible para descargar. |
| Escenario: conservar valores del presupuesto | Dado que el catálogo se actualizó después de registrar una referencia en un Presupuesto Cuando se genera su Excel Entonces los importes y monedas corresponden a los históricos del Presupuesto, no a los precios nuevos del catálogo. |
| Escenario: regenerar sin cambios | Dado que un usuario ya descargó el Excel Cuando vuelve a solicitar su generación sin modificar el Presupuesto Entonces puede descargar otra planilla sin bloqueo ni control de versión vigente. |
| Escenario: no almacenar versiones | Dado que se generó un Excel Cuando se completa la descarga Entonces el sistema no guarda una copia versionada como documento del Presupuesto. |
| Escenario: presupuesto no disponible o acceso inválido | Dado un usuario sin acceso al Presupuesto Cuando solicita generar su Excel Entonces el servidor rechaza la operación. |
| Escenario: generación en curso | Dado que el usuario seleccionó Generar Excel Cuando el sistema está generando el archivo Entonces la acción muestra un indicador de procesamiento Y no puede ejecutarse nuevamente hasta que la generación finalice o falle. |
| Prioridad: | Media. |
| Dependencias: | US-012 (cálculo de importes); formato de la planilla pendiente de validar con EOP. |


## US-015 — Generar propuesta comercial editable mediante IA

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 20. Mockup US-015 — Generar propuesta comercial editable mediante IA

Tabla 26. Historia de usuario US-015 — Generar propuesta comercial editable mediante IA


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero generar un documento Word inicial |
| PARA: | Para reducir el trabajo manual de preparar la propuesta comercial. |
| DESCRIPCIÓN: | La aplicación utiliza la plantilla de EOP, relevamiento y datos confirmados para generar el contenido mediante IA y construir un archivo .docx. No existe una pantalla independiente obligatoria de “borrador IA”; el resultado funcional es el documento editable. |
| Permisos aplicables: | Por sus permisos base, el Operador podrá realizar esta acción sobre un Presupuesto del que sea Responsable; el Supervisor podrá hacerlo sobre cualquier Presupuesto que cumpla las condiciones funcionales y el Administrador posee acceso completo. Un permiso individual específico podrá ampliar o denegar la autorización conforme a RF-73, sin omitir las reglas de negocio. |
| Reglas de negocio: | La IA solo genera contenido inicial. No puede alterar los importes confirmados ni los totales en USD y UYU calculados para el Presupuesto. Al generar el documento se conservarán los importes y la cotización utilizados, que permanecerán vinculados a esa propuesta hasta registrar su envío sin recalcularlos por el tiempo transcurrido. El documento corresponde al cliente y presupuesto actuales. El formato editable es .docx. El archivo generado deberá denominarse Pres. AAAAMMDD - Nombre del Proyecto.docx, utilizando la fecha de creación y el nombre del proyecto registrados en el presupuesto. La plantilla definitiva está pendiente de validación con EOP. Durante la generación del archivo, el botón deberá indicar visualmente que la operación está en curso. Mientras la generación esté en curso, no deberá permitirse ejecutar nuevamente la misma acción. Si se modifican posteriormente datos técnicos o económicos que afecten la propuesta, el documento generado deberá considerarse desactualizado. Después de confirmar nuevamente los datos, el usuario podrá regenerar el documento editable. Cada regeneración deberá utilizar el relevamiento y los datos confirmados vigentes, conservar la misma estructura de nombre del archivo y no incorporar sufijos de versión. |
| Requisitos relacionados: | RF: RF-29, RF-31, RF-39, RF-40, RF-42, RF-43. RF-41: integrado en este flujo. RNF: RNF-5, RNF-6, RNF-11, RNF-12, RNF-13. |
| Criterios de aceptación |  |
| Escenario: generar Word | Dado que existen datos confirmados Cuando el usuario selecciona Generar documento editable Entonces se genera un .docx correspondiente al presupuesto. |
| Escenario: generar documento con nomenclatura definida | Dado que el presupuesto posee una fecha de creación y un nombre de proyecto registrados Cuando se genera correctamente la propuesta comercial editable Entonces el archivo deberá utilizar el nombre Pres. AAAAMMDD - Nombre del Proyecto.docx Y AAAAMMDD deberá corresponder a la fecha de creación del presupuesto. |
| Escenario: valores económicos | Dado que los datos económicos del Presupuesto están confirmados Cuando se genera la propuesta comercial editable mediante IA y se consulta el documento Word resultante Entonces los importes, entre ellos los totales en USD y UYU, corresponden a los datos confirmados del Presupuesto y no son recalculados por IA; quedan conservados junto con la cotización aplicada para ese documento y serán los que se registren cuando se envíe correctamente, aunque haya transcurrido tiempo. |
| Escenario: descargar | Cuando selecciona Descargar Word Entonces obtiene el documento generado. |
| Escenario: falla IA | Cuando el servicio de IA no puede completar la generación Entonces no se pierden los datos del presupuesto Y el sistema deberá informar que la generación no pudo completarse y que se conservan los datos del presupuesto previamente registrados. |
| Escenario: generación en curso | Dado que el usuario selecciona Generar documento editable Cuando el sistema inicia la generación del documento Entonces el botón deberá mostrar un indicador visual de procesamiento Y no deberá permitir una nueva ejecución de la misma acción hasta que el proceso finalice o falle. |
| Escenario: regenerar documento después de modificar el presupuesto | Dado que existe un documento Word generado Y posteriormente se modifican datos técnicos o económicos del presupuesto Cuando el usuario confirma nuevamente los datos y regresa a Documentos Entonces el documento generado anteriormente deberá identificarse como desactualizado Y el usuario deberá poder seleccionar Regenerar documento Y el nuevo .docx vigente deberá generarse utilizando la información confirmada vigente Y conservará la nomenclatura Pres. AAAAMMDD - Nombre del Proyecto.docx, sin sufijos de versión. |
| Prioridad: | Alta. |
| Dependencias: | US-013. |


## US-016 — Cargar propuesta comercial final en PDF

Figura 21. Mockup US-016 — Cargar propuesta comercial final en PDF

Tabla 27. Historia de usuario US-016 — Cargar propuesta comercial final en PDF


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero cargar la propuesta final en PDF |
| PARA: | Para disponer del documento definitivo que será revisado y enviado al cliente. |
| DESCRIPCIÓN: | La propuesta comercial final se elabora por EOP con herramientas externas a la aplicación. Una vez elaborada, el usuario autorizado la carga en PDF desde Documentos del Presupuesto. La última carga queda como PDF vigente y reemplaza funcionalmente a la anterior. No existe generación de documento Word desde el sistema. La carga no altera los datos económicos del presupuesto y el documento permanecerá almacenado privadamente. |
| Permisos aplicables: | Por sus permisos base, el Operador podrá realizar esta acción sobre un Presupuesto del que sea Responsable; el Supervisor podrá hacerlo sobre cualquier Presupuesto que cumpla las condiciones funcionales y el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. |
| Reglas de negocio: | Solo se admite el PDF comercial final. Debe vincularse al Presupuesto y almacenarse privadamente; la última carga sustituye a la versión anterior sin requerir historial funcional de PDFs. Su nombre seguirá Pres. AAAAMMDD - Nombre del Proyecto.pdf conforme a RF-44. Si luego se modifican datos técnicos o económicos se considerará desactualizado y deberá cargarse uno nuevo antes de revisión interna. El mero tiempo transcurrido hasta el envío no modifica sus importes. La aplicación no genera Word mediante IA ni envía automáticamente la propuesta al cliente. |
| Requisitos relacionados: | RF: RF-31, RF-44; RF-34 para su envío externo. RNF: RNF-4, RNF-5, RNF-6, RNF-7, RNF-8, RNF-10, RNF-19. |
| Criterios de aceptación |  |
| Escenario: cargar PDF | Dado que el archivo es PDF Cuando el usuario lo carga Entonces queda definido como propuesta comercial final vigente sin recalcular los importes ni la cotización conservados al confirmar los datos económicos del presupuesto, que serán los mismos al registrar el envío correctamente. |
| Escenario: nomenclatura del PDF final | Dado que existe un presupuesto con fecha de creación y nombre de proyecto registrados Cuando se carga correctamente la propuesta comercial final Entonces el archivo vigente deberá utilizar la nomenclatura Pres. AAAAMMDD - Nombre del Proyecto.pdf Y AAAAMMDD deberá corresponder a la fecha de creación del presupuesto. |
| Escenario: reemplazar | Dado que ya existe un PDF final Cuando carga otro Entonces el nuevo reemplaza funcionalmente al anterior como versión vigente. |
| Escenario: archivo incorrecto | Dado un usuario autorizado que intenta cargar una propuesta comercial con un archivo cuyo formato no es PDF Cuando confirma la carga Entonces el sistema rechaza el archivo y no reemplaza el PDF comercial vigente, si existe. |
| Escenario: modificar presupuesto posteriormente | Dado que existe PDF final Cuando se modifican datos técnicos o económicos Entonces el PDF queda marcado como no vigente para una nueva revisión Y debe cargarse uno actualizado. |
| Prioridad: | Alta. |
| Dependencias: | US-013 (confirmación económica); preparación externa por EOP, seguida de carga de PDF vigente. |


## US-017 — Enviar presupuesto a revisión interna

Figura 22. Mockup US-017 — Enviar presupuesto a revisión interna

Tabla 28. Historia de usuario US-017 — Enviar presupuesto a revisión interna


| COMO: | Como Operador Responsable del Presupuesto, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero enviar el presupuesto a revisión interna |
| PARA: | Para que sea validado antes de enviarlo al cliente. |
| DESCRIPCIÓN: | La acción se habilita únicamente cuando existe PDF final vigente. Al seleccionarla: Borrador → Para revisión interna. También se utiliza después de una Solicitud de modificación. |
| Permisos aplicables: | Por sus permisos base, el Operador podrá realizar esta acción sobre un Presupuesto del que sea Responsable; el Supervisor podrá hacerlo sobre cualquier Presupuesto que cumpla las condiciones funcionales y el Administrador posee acceso completo. Se aplican exclusivamente los permisos fijos y las restricciones por responsabilidad correspondientes a RF-73; no se admiten excepciones individuales. |
| Reglas de negocio: | Acción explícita. PDF vigente obligatorio. Todo presupuesto pasa por revisión antes del envío. La revisión interna no envía todavía nada al cliente. |
| Requisitos relacionados: | RF: RF-02, RF-32, RF-44. RNF: RNF-4, RNF-10, RNF-19. |
| Criterios de aceptación |  |
| Escenario: Operador Responsable envía a revisión interna | Dado un Operador Responsable de un Presupuesto editable que cuenta con PDF final vigente Cuando solicita enviar el Presupuesto a revisión interna Entonces puede pasar a Para revisión interna con sus permisos fijos Y un Operador que no sea Responsable no podrá realizar la acción, aunque acceda mediante URL directa. |
| Escenario: PDF disponible | Dado un presupuesto editable con datos confirmados y PDF comercial vigente Cuando el usuario autorizado selecciona Enviar a revisión interna Entonces el presupuesto cambia a Para revisión interna y no se envía nada al cliente. |
| Escenario: falta PDF | Dado que no existe PDF final vigente Cuando intenta enviar a revisión Entonces la acción no se completa. |
| Escenario: presupuesto modificado | Dado que el cliente solicitó modificaciones Cuando se carga un PDF actualizado y se envía nuevamente a revisión Entonces vuelve a Para revisión interna. |
| Prioridad: | Alta. |
| Dependencias: | US-016. |


## US-018 — Confirmar revisión y registrar el envío externo al cliente

Figura 23. Mockup US-018 — Confirmar revisión y enviar al cliente

Tabla 29. Historia de usuario US-018 — Confirmar revisión y registrar el envío externo al cliente


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero confirmar la revisión y registrar el envío externo de la propuesta comercial al cliente. |
| PARA: | Para llevar seguimiento del envío real sin requerir mensajería automática desde EOP. |
| DESCRIPCIÓN: | Para un Presupuesto en Para revisión interna con PDF comercial vigente, el Supervisor o Administrador revisa la propuesta y la envía al cliente fuera de la aplicación, por correo electrónico o WhatsApp. La aplicación permite descargar el PDF y, si se elige WhatsApp, abrir la conversación con el Cliente mediante Click to Chat con un mensaje precargado, pero no envía documentos ni mensajes automáticamente. Luego registra en el sistema el envío realizado, indicando medio y fecha; ese registro confirma en el mismo acto la revisión interna, pasa el Presupuesto a Enviado y calcula el vencimiento. |
| Permisos aplicables: | Solo Supervisor y Administrador pueden confirmar revisión y registrar envío según los permisos fijos de RF-73. No se habilitan excepciones individuales. |
| Reglas de negocio: | Se requiere PDF final vigente y estado Para revisión interna; la revisión se confirma al registrar el envío. El medio registrado será exclusivamente Correo electrónico o WhatsApp. El envío se efectúa externamente, sin servicio de correo automático ni envío de WhatsApp por la aplicación. Seleccionar medio o abrir chat no implica envío; solo la confirmación explícita del usuario registra medio y fecha, confirma la revisión, actualiza el estado a Enviado y activa vencimiento. Si no confirma se conserva Para revisión interna. Se conservan totales y cotización confirmados sin recálculo por tiempo transcurrido. La fecha del envío se propone con la fecha del día, es editable y no puede ser posterior a la fecha actual ni anterior a la carga del PDF final vigente. |
| Requisitos relacionados: | RF: RF-29, RF-33, RF-34, RF-36, RF-44, RF-73. RNF: RNF-1, RNF-4, RNF-5, RNF-10. |
| Criterios de aceptación |  |
| Escenario: iniciar el registro desde revisión interna | Dado un Presupuesto en Para revisión interna y con PDF comercial vigente Cuando Supervisor o Administrador selecciona Confirmar presupuesto Entonces se abre el registro del envío externo, sin cambiar todavía el estado ni confirmar la revisión. |
| Escenario: registro de envío externo por correo | Dado que el usuario envió efectivamente el PDF por correo fuera de la aplicación Cuando confirma el envío e indica medio Correo electrónico y fecha Entonces se registra el envío, queda confirmada la revisión interna y el Presupuesto pasa a Enviado pendiente de respuesta. |
| Escenario: registro de envío externo por WhatsApp | Dado que el usuario envió efectivamente el PDF por WhatsApp fuera de la aplicación Cuando confirma el envío e indica medio WhatsApp y fecha Entonces se registra el envío, queda confirmada la revisión interna y el Presupuesto pasa a Enviado pendiente de respuesta. |
| Escenario: abrir chat no confirma envío | Dado que se abre WhatsApp Click to Chat desde la aplicación Cuando el usuario no confirma que envió la propuesta Entonces el Presupuesto permanece Para revisión interna y no registra medio ni fecha de envío. |
| Escenario: no enviar automáticamente | Dado que el usuario elige un medio de comunicación Cuando solicita preparar el envío Entonces la aplicación no envía automáticamente correos, mensajes ni adjuntos PDF. |
| Escenario: sin PDF vigente o fuera de revisión interna | Dado un Presupuesto sin PDF final vigente o que no se encuentra en Para revisión interna Cuando se intenta registrar su envío Entonces se impide avanzar y se informa la condición faltante. |
| Escenario: fecha y vencimiento | Dado un envío externo correctamente registrado Cuando se confirma el registro Entonces queda guardada la fecha del envío y se calcula el vencimiento conforme a US-020 sin alterar los importes confirmados. |
| Escenario: cancelar registro | Dado un usuario que inició el registro de envío externo Cuando lo cancela antes de confirmar Entonces no se registra envío, no se confirma la revisión y se conserva el estado anterior. |
| Escenario: usuario sin permiso | Dado un Operador que intenta registrar envío comercial Cuando solicita la operación Entonces el servidor la deniega conforme a los permisos fijos. |
| Escenario: abrir WhatsApp mediante Click to Chat | Dado un Presupuesto en Para revisión interna y un Cliente con número de WhatsApp válido Cuando el usuario elige WhatsApp y selecciona Abrir WhatsApp Entonces se abre la conversación con el número del Cliente y un mensaje precargado Y la aplicación no adjunta ni envía ningún documento automáticamente. |
| Escenario: fecha de envío inválida | Dado un registro de envío externo en curso Cuando el usuario indica una fecha posterior a la actual o anterior a la carga del PDF final vigente Entonces el sistema rechaza el registro e informa el motivo Y el Presupuesto permanece en Para revisión interna. |
| Prioridad: | Alta. |
| Dependencias: | US-016 (PDF comercial vigente); US-017 (revisión interna); US-020 (vencimientos). |


## US-019 — Registrar la respuesta del cliente

Figura 24. Mockup US-019 — Registrar la respuesta del cliente

Tabla 30. Historia de usuario US-019 — Registrar la respuesta del cliente


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero registrar la respuesta recibida del cliente |
| PARA: | Para mantener actualizado el estado comercial del presupuesto. |
| DESCRIPCIÓN: | Desde el listado, para presupuestos Enviado / pendiente de respuesta, aparecen: Aprobar \| Rechazar \| Solicitud de modificación La respuesta se registra manualmente.  Antes de ejecutar cualquiera de estas acciones, el sistema deberá solicitar confirmación al usuario. Si selecciona Rechazar, durante esa confirmación deberá elegir obligatoriamente un Motivo de rechazo desde un desplegable de categorías cerradas; sin Motivo no se completará el rechazo. Tras confirmar, se almacenarán conjuntamente el Estado Rechazado y el Motivo junto al Presupuesto para consulta y estadísticas. El catálogo definitivo confirmado por EOP incluye exactamente: Precio elevado; Falta de presupuesto o recursos del cliente; Proyecto postergado / no es prioridad actualmente; Selección de otro proveedor o competidor; La propuesta no se ajusta a la necesidad del cliente; Falta de aprobación interna del cliente; El cliente ya cuenta con otra solución o proveedor; Otro. El catálogo y la obligación de seleccionar un Motivo están confirmados; sin Motivo no se cambiará el Estado ni se guardará un rechazo parcial. Otro no requiere una descripción libre adicional. Si se registra la aprobación de un presupuesto que todavía no posee un proyecto asociado, deberá iniciarse el proceso de creación del proyecto definido en US-027. En ese caso, la aprobación no quedará registrada definitivamente hasta que se complete correctamente la creación del proyecto. Cuando el presupuesto se encuentre en estado Vencido, no podrá registrarse directamente ninguna de las respuestas del cliente. Si el cliente desea continuar con la propuesta, deberá realizarse primero la reapertura definida en US-020. Una vez reabierto, el presupuesto volverá al estado Enviado / pendiente de respuesta y estarán nuevamente disponibles las acciones Aprobar, Rechazar y Solicitud de modificación. |
| Permisos aplicables: | Supervisor y Administrador pueden registrar respuestas comerciales por sus permisos fijos (RF-73). El Operador no puede registrar aprobación, rechazo ni Solicitud de modificación del cliente. Cuando la aprobación exige crear un Proyecto, deben cumplirse además los requisitos de RF-52 y la creación transaccional. |
| Reglas de negocio: | La respuesta del cliente deberá ser registrada manualmente por un usuario autorizado. Antes de ejecutar Aprobar, Rechazar o Solicitud de modificación, el sistema deberá solicitar confirmación al usuario. Si se aprueba un presupuesto que ya se encuentra asociado a un proyecto, deberá pasar a estado Aprobado y no deberá crearse otro proyecto. Si se aprueba un presupuesto que no posee proyecto asociado, deberá iniciarse el proceso de creación del proyecto definido en RF-52. Cuando la aprobación requiera crear un proyecto, el presupuesto no deberá quedar definitivamente en estado Aprobado hasta completar correctamente la creación del proyecto. Si el usuario cancela la creación del proyecto, no deberá crearse el proyecto ni registrarse definitivamente la aprobación del presupuesto. Si se registra el rechazo del cliente, el presupuesto deberá pasar a estado Rechazado únicamente después de seleccionar un Motivo de rechazo y confirmar la acción; no deberá generarse un proyecto. Si el cliente solicita modificaciones, el presupuesto deberá pasar a estado Solicitud de modificación del cliente y volver a quedar habilitado para edición. Una vez realizadas las modificaciones solicitadas, el presupuesto deberá repetir el resumen y la confirmación de los datos económicos, la carga de un PDF final actualizado, la revisión interna y el registro del envío. Las acciones Aprobar, Rechazar y Solicitud de modificación solamente estarán disponibles para su ejecución cuando el presupuesto se encuentre en estado Enviado / pendiente de respuesta del cliente. Un presupuesto Vencido no permitirá registrar directamente una respuesta del cliente. Para registrar una respuesta correspondiente a un presupuesto Vencido deberá completarse previamente la reapertura definida en US-020. Una vez reabierto el presupuesto y restablecido el estado Enviado / pendiente de respuesta del cliente, deberán volver a encontrarse disponibles las acciones Aprobar, Rechazar y Solicitud de modificación. |
| Requisitos relacionados: | RF: RF-02, RF-35, RF-36, RF-52. RNF: RNF-1, RNF-4, RNF-10. |
| Criterios de aceptación |  |
| Escenario: Operador sin autorización de respuesta | Dado un Presupuesto Enviado pendiente de respuesta Y un usuario con rol Operador Cuando intenta registrar aprobación, rechazo o Solicitud de modificación Entonces el servidor deniega la operación porque su rol no la permite Y no existe una autorización individual excepcional. |
| Escenario: aprobar presupuesto con proyecto existente | Dado que el presupuesto se encuentra Enviado / pendiente de respuesta Y ya posee un proyecto asociado Cuando el usuario selecciona Aprobar y confirma la acción Entonces el presupuesto deberá pasar a estado Aprobado Y no deberá crearse otro proyecto. |
| Escenario: aprobar presupuesto sin proyecto asociado | Dado que el presupuesto se encuentra Enviado / pendiente de respuesta Y no posee un proyecto asociado Cuando el usuario selecciona Aprobar y confirma la acción Entonces deberá iniciarse el proceso de creación del proyecto definido en US-027 Y la aprobación no deberá quedar registrada definitivamente hasta completar correctamente dicho proceso. |
| Escenario: iniciar rechazo | Dado que el presupuesto se encuentra Enviado / pendiente de respuesta Cuando un usuario autorizado selecciona Rechazar Entonces el sistema solicitará confirmación y exigirá seleccionar un Motivo de rechazo desde un desplegable Y el Presupuesto no cambiará a Rechazado hasta confirmar correctamente la acción. |
| Escenario: impedir rechazo sin Motivo | Dado que el usuario seleccionó Rechazar y aún no eligió un Motivo Cuando intenta confirmar Entonces el sistema impedirá la operación y el Presupuesto permanecerá sin pasar a Rechazado. |
| Escenario: confirmar rechazo con Motivo | Dado un Presupuesto Enviado / pendiente de respuesta y un Motivo de rechazo seleccionado Cuando el usuario confirma Entonces se registrarán conjuntamente el estado Rechazado y el Motivo junto al Presupuesto, y no se generará Proyecto. |
| Escenario: cancelar rechazo | Dado que el usuario inició el rechazo y seleccionó un Motivo Cuando cancela la confirmación Entonces no cambiará el Estado ni se almacenarán rechazo o Motivo como confirmados. |
| Escenario: seleccionar Otro | Dado que ninguna de las demás categorías representa el Motivo correspondiente Cuando el usuario selecciona Otro y confirma el rechazo Entonces se registrará Otro como Motivo del Presupuesto, sin requerir texto libre adicional. |
| Escenario: modificación | Dado que el presupuesto se encuentra Enviado / pendiente de respuesta Cuando el usuario selecciona Solicitud de modificación y confirma la acción Entonces deberá pasar a estado Solicitud de modificación del cliente Y deberá volver a quedar habilitado para edición. |
| Escenario: reenviar después de modificar | Dado que se realizaron cambios Cuando un usuario autorizado intenta reenviar al Cliente el Presupuesto modificado tras una Solicitud de modificación Entonces el presupuesto debe volver a confirmar sus datos económicos, cargar un PDF final actualizado y superar una nueva revisión antes de registrar un nuevo envío. |
| Escenario: cancelar creación del proyecto durante la aprobación | Dado que el usuario inició la aprobación de un presupuesto sin proyecto asociado Y se encuentra completando los datos necesarios para crear el proyecto Cuando cancela el proceso Entonces no deberá crearse el proyecto Y el presupuesto no deberá quedar registrado como Aprobado. |
| Escenario: intentar registrar respuesta sobre presupuesto vencido | Dado que el presupuesto se encuentra en estado Vencido Cuando un usuario intenta registrar directamente Aprobar, Rechazar o Solicitud de modificación Entonces el sistema no deberá permitir ejecutar la acción Y deberá requerirse previamente la reapertura del presupuesto. |
| Escenario: registrar respuesta después de reabrir | Dado que un presupuesto se encontraba en estado Vencido Y fue reabierto correctamente mediante US-020 Y volvió al estado Enviado / pendiente de respuesta del cliente Cuando el usuario consulta las acciones disponibles Entonces deberán encontrarse nuevamente disponibles Aprobar, Rechazar y Solicitud de modificación. |
| Escenario: error técnico al crear el proyecto durante la aprobación | Dado que el usuario inició la aprobación de un presupuesto sin proyecto asociado Y completó y confirmó los datos necesarios para crear el proyecto Cuando ocurre un error que impide crear correctamente el proyecto Entonces el presupuesto no deberá quedar registrado definitivamente como Aprobado Y no deberá crearse el proyecto Y el sistema deberá informar al usuario que la operación no pudo completarse y permitir un nuevo intento sin generar proyectos duplicados. |
| Prioridad: | Alta |
| Dependencias: | US-018. |


## US-020 — Gestionar el vencimiento y la reapertura del presupuesto

Figura 25. Mockup US-020 — Gestionar el vencimiento y la reapertura del presupuesto

Tabla 31. Historia de usuario US-020 — Gestionar el vencimiento y la reapertura del presupuesto


| COMO: | Como Supervisor o Administrador para reabrir, y como usuario autorizado para consultar vencimientos. |
|---|---|
| QUIERO: | Quiero que el sistema identifique los presupuestos vencidos y permita reabrirlos cuando corresponda |
| PARA: | Para controlar la vigencia de las propuestas y permitir continuar con una propuesta vencida cuando el cliente manifieste interés. |
| DESCRIPCIÓN: | El plazo de vigencia de un Presupuesto se expresa en días corridos y comienza con la fecha de envío externo registrado. El sistema comprueba si un presupuesto Enviado pendiente de respuesta está vencido al listarlo, consultar su detalle o intentar una operación. Cuando corresponda lo marca Vencido antes de permitir operaciones incompatibles, sin ejecutar una tarea programada de vencimientos. Supervisor o Administrador puede reabrirlo indicando un nuevo plazo; se conserva el flujo acordado de estados y restricciones. |
| Permisos aplicables: | Reapertura: Supervisor o Administrador. Consulta y comprobación de vencimientos: usuarios con acceso al presupuesto; el Operador no puede reabrirlo. |
| Reglas de negocio: | La comprobación de vencimiento se realiza al listar, consultar o intentar operar. No se necesita un proceso programado específico. La fecha inicial de vencimiento es fecha de envío registrado más plazo en días corridos. No se vencerán presupuestos Aprobados, Rechazados, Cancelados o en Solicitud de modificación. Solo Supervisor y Administrador pueden reabrir un Vencido: ingresan nuevo plazo en días corridos, se registra fecha de reapertura, se calcula nueva fecha y vuelve a Enviado pendiente de respuesta. Reabrir no habilita edición directa ni registra un nuevo envío; mientras siga Vencido no se admiten respuestas del cliente. |
| Requisitos relacionados: | RF: RF-02, RF-34, RF-35, RF-36, RF-73. RNF: RNF-4, RNF-10. |
| Criterios de aceptación |  |
| Escenario: verificar al listar | Dado un Presupuesto Enviado cuya fecha de vencimiento es igual o anterior a la fecha actual Cuando un usuario abre el listado Entonces se comprueba el vencimiento y se muestra en estado Vencido. |
| Escenario: verificar al consultar | Dado un Presupuesto Enviado que venció desde la última visita Cuando un usuario accede a su detalle Entonces se actualiza a Vencido antes de habilitar operaciones. |
| Escenario: verificar al operar | Dado un Presupuesto Enviado cuyo plazo terminó Cuando se intenta registrar la respuesta del cliente Entonces el sistema detecta el vencimiento y bloquea la respuesta hasta una reapertura válida. |
| Escenario: no vencer estados definitivos | Dado un Presupuesto Aprobado, Rechazado, Cancelado o en Solicitud de modificación Cuando se consulta después de la fecha prevista Entonces no cambia a Vencido por esa comprobación. |
| Escenario: reabrir válido | Dado un presupuesto Vencido y un Supervisor o Administrador autorizado Cuando indica un nuevo plazo válido y confirma reabrir Entonces se registra la fecha de reapertura y nuevo vencimiento Y el presupuesto regresa a Enviado pendiente de respuesta. |
| Escenario: reapertura no edita directamente | Dado que un Presupuesto Vencido fue reabierto Cuando se consulta Entonces no pasa a Borrador ni se habilita edición sin una posterior Solicitud de modificación del cliente. |
| Escenario: Operador sin permisos | Dado un Operador autorizado para consultar el Presupuesto Cuando intenta reabrir un presupuesto Vencido Entonces el servidor impide la reapertura. |
| Escenario: respuesta del Cliente después de reabrir | Dado un Presupuesto Vencido que un Supervisor o Administrador reabrió con un nuevo plazo válido y que volvió a Enviado Cuando un usuario autorizado registra la respuesta del Cliente antes de la nueva fecha de vencimiento Entonces se aplica el flujo de aprobación, rechazo o solicitud de modificación definido en US-019, sin registrar un nuevo envío automáticamente. |
| Escenario: día del vencimiento | Dado un Presupuesto Enviado cuya fecha de vencimiento es la fecha actual Cuando un usuario lo lista, lo consulta o intenta operar sobre él Entonces se actualiza a Vencido antes de habilitar operaciones. |
| Escenario: calcular vencimiento inicial | Dado un Presupuesto con plazo de vigencia en días corridos Cuando se registra correctamente su envío externo Entonces la fecha de vencimiento es la fecha de envío registrada más el plazo, contando todos los días calendario, incluidos fines de semana y feriados. |
| Escenario: volver a vencer después de reabrir | Dado un Presupuesto reabierto que continúa Enviado pendiente de respuesta Cuando llega su nueva fecha de vencimiento y un usuario lo lista, lo consulta o intenta operar sobre él Entonces vuelve a quedar en estado Vencido. |
| Prioridad: | Alta. |
| Dependencias: | US-018 (envío externo registrado); US-019 (respuesta del cliente). |


## US-021 — Cancelar un presupuesto

Figura 26. Mockup US-021 — Cancelar un presupuesto

Tabla 32. Historia de usuario US-021 — Cancelar un presupuesto


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero cancelar un presupuesto |
| PARA: | Para indicar que EOP decidió no continuar procesándolo sin eliminar su información. |
| DESCRIPCIÓN: | Puede cancelarse desde: Borrador; Para revisión interna; Enviado / pendiente de respuesta. La cancelación cambia el estado a Cancelado |
| Permisos aplicables: | Supervisor y Administrador pueden cancelar Presupuestos en los estados permitidos conforme a los permisos fijos de RF-73; Operador no puede cancelar, sin excepciones individuales. Se mantienen las validaciones y la confirmación obligatoria. |
| Reglas de negocio: | No elimina físicamente datos. Requiere confirmación. Permanece consultable. No puede seguir siendo editado o enviado. Si ya fue enviado, el registro de envío se conserva. No vuelve al flujo normal. No pasa a Vencido. |
| Requisitos relacionados: | RF: RF-02, RF-91. RNF: RNF-1, RNF-4. |
| Criterios de aceptación |  |
| Escenario: rol autorizado para cancelar | Dado un Presupuesto en uno de los estados que admiten cancelación Cuando un Supervisor o Administrador solicita cancelarlo y confirma Entonces el estado pasa a Cancelado y se conservan los datos y documentos anteriores. |
| Escenario: cancelar estado válido | Dado que se encuentra Borrador, Para revisión interna o Enviado Cuando un usuario autorizado selecciona Cancelar Entonces se solicita confirmación. |
| Escenario: confirmar | Dado un Presupuesto cuyo estado permite cancelación y un Supervisor o Administrador autorizado que solicitó Cancelar Cuando confirma expresamente la cancelación Entonces el estado pasa a Cancelado y se conserva la información histórica y documentación anterior. |
| Escenario: cancelar acción | Dado un Presupuesto con una solicitud de cancelación pendiente de confirmar Cuando el usuario cancela la confirmación Entonces el estado y los datos registrados del Presupuesto no se modifican. |
| Escenario: presupuesto cancelado | Dado un Presupuesto que ya se encuentra Cancelado Cuando el usuario intenta continuar su elaboración o cambiarlo de estado mediante el flujo comercial normal Entonces la aplicación rechaza esas operaciones y conserva su estado Cancelado. |
| Prioridad: | Media. |
| Dependencias: | US-001. |


## US-022 — Consultar documentación desde el listado

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 27. Mockup US-022 — Consultar documentación desde el listado

Tabla 33. Historia de usuario US-022 — Consultar documentación desde el listado


| COMO: | Como Operador, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero acceder directamente a los documentos desde el listado |
| PARA: | Para consultarlos sin recorrer nuevamente todo el proceso de elaboración. |
| DESCRIPCIÓN: | En las acciones de cada presupuesto: Ver PDF cuando existe PDF final; Descargar PDF; Descargar Excel cuando existe planilla. |
| Permisos aplicables: | Por sus permisos base, Operador, Supervisor y Administrador pueden consultar sin modificar la documentación de Presupuestos; las excepciones individuales para Operador o Supervisor se aplican conforme a RF-73. |
| Reglas de negocio: | El documento debe pertenecer al presupuesto seleccionado. La consulta no modifica el presupuesto. El PDF se puede previsualizar sin descargar. Los documentos no son públicamente accesibles mediante URL. |
| Requisitos relacionados: | RF: RF-03, RF-44, RF-45; RF-38 solo para la acción condicional Descargar Excel (US-014, prioridad Media). RNF: RNF-5, RNF-6, RNF-9. |
| Criterios de aceptación |  |
| Escenario: ver PDF | Dado que existe PDF final Cuando selecciona Ver PDF Entonces se previsualiza el PDF del presupuesto seleccionado. |
| Escenario: Excel | Dado que existe Excel Cuando selecciona Descargar Excel Entonces se descarga el archivo correspondiente. |
| Escenario: documento inexistente | Dado que todavía no se generó un documento Entonces no se ofrece una acción que implique acceder a un archivo inexistente |
| Prioridad: | Alta. |
| Dependencias: | US-016. |


## US-023 — Consultar estadísticas de presupuestos

Figura 28. Mockup US-023 — Consultar estadísticas de presupuestos

Tabla 34. Historia de usuario US-023 — Consultar estadísticas de presupuestos


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero consultar indicadores resumidos de presupuestación |
| PARA: | Para conocer el resultado general de la actividad comercial. |
| DESCRIPCIÓN: | Existe una opción propia del menú lateral: Estadísticas de presupuestos Muestra al menos: total, de presupuestos; aprobados; rechazados y distribución de los Presupuestos efectivamente rechazados entre las categorías de Motivo de RF-35. Puede utilizar tarjetas numéricas y gráficos. |
| Permisos aplicables: | Supervisor y Administrador pueden consultar las estadísticas por sus permisos fijos de RF-73. Operador no tiene autorización ni se admiten excepciones individuales. La distribución de motivos de rechazo utiliza las ocho categorías de RF-35, incluida Otro. |
| Reglas de negocio: | Los valores se calculan con los datos actuales. La distribución incluirá solo Presupuestos cuyo Estado actual sea Rechazado, contabilizando una vez cada uno en su Motivo registrado; los registros históricos Rechazados sin Motivo se contabilizarán en Otro exclusivamente para las estadísticas, sin modificar ni atribuir un Motivo al dato almacenado. La suma de las ocho categorías, incluida Otro, coincidirá con el total de Presupuestos actualmente Rechazados del mismo conjunto; no incluirá Vencidos, Cancelados, Solicitud de modificación ni rechazos no confirmados. Se contabilizan según estado. Consultar no modifica Presupuestos ni Motivos. La distribución podrá representarse mediante indicadores y/o gráficos sin fijar un tipo de gráfico. |
| Requisitos relacionados: | RF: RF-88. RNF: RNF-4, RNF-14. |
| Criterios de aceptación |  |
| Escenario: consulta según rol fijo | Dado un Supervisor o Administrador con acceso a Estadísticas de presupuestos Cuando abre la sección Entonces puede consultar los indicadores y motivos de rechazo previstos. Y un Operador que intente acceder a esa sección deberá recibir una denegación del servidor. |
| Escenario: consultar | Dado un Supervisor o Administrador autorizado para consultar Estadísticas de presupuestos Cuando accede a la sección Estadísticas de presupuestos Entonces visualiza los totales de presupuestos, aprobados y rechazados según los criterios definidos. |
| Escenario: consistencia | Dado un conjunto de presupuestos registrados con sus estados actuales Cuando un Supervisor o Administrador consulta las Estadísticas de presupuestos Entonces cada indicador refleja los presupuestos y estados registrados sin modificar su información. |
| Escenario: consultar distribución de Motivos de rechazo | Dado que existen Presupuestos con rechazo confirmado y Motivos registrados Cuando el usuario accede a Estadísticas de presupuestos Entonces podrá visualizar la cantidad de rechazos correspondiente a cada Motivo registrado. |
| Escenario: contabilizar categoría Otro | Dado que existen Presupuestos rechazados con Motivo registrado Otro Cuando el sistema calcula las estadísticas Entonces deberán contabilizarse dentro de la categoría Otro. |
| Escenario: contabilizar rechazo histórico sin Motivo en Otro | Dado que existe un Presupuesto histórico cuyo Estado actual es Rechazado y no tiene Motivo registrado. Cuando un usuario autorizado consulta las estadísticas de Presupuestos. Entonces el Presupuesto deberá contabilizarse dentro de la categoría Otro. Y deberá contribuir una única vez a la distribución de Presupuestos Rechazados. Y la consulta no deberá modificar su información histórica. |
| Escenario: actualizar estadísticas | Dado que se confirma un nuevo rechazo con Motivo registrado Cuando posteriormente se consultan las Estadísticas de presupuestos Entonces la distribución reflejará ese rechazo en la categoría correspondiente. |
| Prioridad: | Baja. |
| Dependencias: | Gestión de presupuestos y de sus estados (RF-02). |


## US-024 — Actualizar automáticamente precios desde proveedores

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Tabla 35. Historia de usuario US-024 — Actualizar automáticamente precios desde proveedores


| COMO: | Como usuario de EOP |
|---|---|
| QUIERO: | Que los precios de las relaciones Material-Proveedor vinculadas con Fivisa se actualicen automáticamente. |
| PARA: | Disponer de referencias de precio actualizadas sin tener que realizar manualmente la actualización periódica. |
| DESCRIPCIÓN: | La actualización automática se realizará una vez por semana por el sistema, sin que ningún usuario deba iniciar semanalmente la operación. Los resultados podrán consultarse según los permisos generales del catálogo. Inicialmente se realizará sobre relaciones material-proveedor vinculadas correctamente con productos de Fivisa mediante US-056. Para cada relación habilitada, el sistema utilizará la URL y el código o referencia externa almacenados para localizar nuevamente el producto, verificar que corresponda al producto vinculado y obtener el precio normal publicado vigente del producto, sin utilizar descuentos, promociones, ofertas ni precios especiales. Cuando el producto y su precio normal estrictamente mayor que cero puedan validarse correctamente, deberán actualizarse únicamente el precio actual y la fecha de última actualización de la relación material-proveedor. La moneda previamente seleccionada por el usuario al vincular el producto permanecerá sin cambios y no se efectuarán conversiones automáticas. La actualización no deberá modificar el nombre ni la unidad de medida del material. No implica importar el catálogo de Fivisa. |
| Permisos aplicables: | — |
| Reglas de negocio: | Solo se procesarán relaciones material-proveedor previamente vinculadas mediante US-056, registradas definitivamente en el catálogo y con actualización automática habilitada. La actualización utilizará la información de localización almacenada durante la vinculación. Antes de modificar el precio deberá verificarse que el producto recuperado corresponda al producto vinculado. Cuando se obtenga un precio normal estrictamente mayor que cero se actualizarán exclusivamente el precio y la fecha de última actualización de la relación material-proveedor; no se modificará su moneda USD o UYU previamente registrada ni se convertirán los importes. La actualización no modificará el nombre ni la unidad de medida del material. Si no puede identificarse el producto o recuperarse correctamente su precio normal publicado, se conservará el último precio registrado; no se utilizará un precio promocional como sustituto. La falla en la actualización de un producto no impedirá procesar los demás. El sistema ejecutará el proceso automáticamente una vez por semana, sin acción manual de ningún rol. Los resultados se consultarán según los permisos generales del catálogo. No se importarán automáticamente otros productos del catálogo de Fivisa. Los precios correspondientes a relaciones material-Fivisa habilitadas para actualización automática no podrán ser modificados manualmente. Hasta que se obtenga una nueva actualización válida desde Fivisa, deberá conservarse el último precio obtenido correctamente. |
| Requisitos relacionados: | RF: RF-90, RF-92. RNF: RNF-11, RNF-12, RNF-13. |
| Criterios de aceptación |  |
| Escenario: producto vinculado | Dado que existe una relación material-Fivisa correctamente vinculada mediante US-056, registrada definitivamente en el catálogo y con actualización automática habilitada Cuando el sistema ejecuta automáticamente la actualización semanal, sin inicio manual de un usuario Y el sistema localiza y valida el producto Y obtiene el precio normal publicado vigente Entonces deberá actualizarse el precio de la relación material-proveedor Y deberá actualizarse su fecha de última actualización Y no deberán modificarse el nombre ni la unidad del material, ni la moneda USD o UYU previamente registrada para esa relación; no se convertirán automáticamente los importes. |
| Escenario: producto dudoso | Dado que existe una relación vinculada Cuando no puede verificarse suficientemente el producto o recuperar correctamente el precio normal publicado Entonces deberán conservarse el último precio, su moneda y la fecha de actualización registrada; también se rechazarán como inválidos los precios normales iguales a cero o negativos. |
| Escenario: falla individual | Dado que falla la actualización de un producto Cuando continúa el procesamiento semanal Entonces los demás productos vinculados deberán poder procesarse normalmente. |
| Escenario: conservar último precio entre actualizaciones | Dado que una relación material-Fivisa posee actualización automática habilitada Y existe un último precio obtenido correctamente Cuando todavía no se ha ejecutado una nueva actualización válida Entonces deberá mantenerse dicho precio como precio actual Y no deberá permitirse su modificación manual. |
| Prioridad: | Media. |
| Dependencias: | US-056. |


## US-025 — Consultar y localizar proyectos

Figura 29. Mockup US-025 — Consultar y localizar proyectos

Tabla 36. Historia de usuario US-025 — Consultar y localizar proyectos


| COMO: | Como Operador, Supervisor o Administrador con acceso a Proyectos |
|---|---|
| QUIERO: | Quiero consultar, buscar y filtrar los proyectos registrados |
| PARA: | Para localizar rápidamente el proyecto sobre el que necesito trabajar o consultar información. |
| DESCRIPCIÓN: | El usuario podrá acceder a la sección Proyectos y visualizar el listado general de proyectos. El listado mostrará código, nombre, cliente, estado, Tipo de Proyecto, Responsable y fecha de inicio. El usuario podrá buscar proyectos por Nombre y aplicar filtros por Cliente, Estado, Tipo de proyecto y Responsable. El filtro Tipo de proyecto deberá cargarse utilizando los tipos de proyecto fijos disponibles en la aplicación. Los criterios de búsqueda y filtrado podrán utilizarse conjuntamente. Cuando se apliquen varios criterios, el listado deberá mostrar únicamente los proyectos que cumplan simultáneamente con todos ellos. La búsqueda y los filtros no deberán modificar la información almacenada. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Todo proyecto registrado deberá permanecer disponible para consulta. El listado mostrará: Código. Nombre. Cliente. Estado. Tipo de Proyecto. Responsable. Fecha de inicio. Se podrá buscar un proyecto por nombre. Se podrá filtrar por cliente. Se podrá filtrar por Estado, Tipo de proyecto y Responsable. Los valores del filtro Tipo de proyecto deberán provenir de los tipos de proyecto fijos de la aplicación. La opción Todos no aplicará restricción por Tipo de proyecto. Al seleccionar un Tipo de proyecto deberán mostrarse únicamente los proyectos correspondientes a ese tipo. Los filtros por Cliente, Estado, Tipo de proyecto y Responsable podrán combinarse. Cuando se apliquen varios filtros, deberán mostrarse únicamente los proyectos que cumplan todos los criterios seleccionados. La búsqueda por Nombre podrá utilizarse conjuntamente con los filtros disponibles. Los filtros y la búsqueda no modificarán la información de los proyectos. Desde cada proyecto deberá poder accederse a su ficha. |
| Requisitos relacionados: | RF: RF-46, RF-47. RNF: RNF-14, RNF-18, RNF-4. |
| Criterios de aceptación |  |
| Escenario: Consultar proyectos | Dado que el usuario accede a la sección Proyectos. Cuando existan proyectos registrados. Entonces el sistema deberá mostrar el listado con código, nombre, cliente, estado, Tipo de Proyecto, Responsable y fecha de inicio. |
| Escenario: Buscar por nombre | Dado que el usuario se encuentra en el listado Cuando ingresa parte o la totalidad del nombre de un proyecto Entonces deberán mostrarse los proyectos que correspondan al criterio ingresado. |
| Escenario: filtrar por Cliente y/o Estado | Dado que existen proyectos registrados Cuando el usuario selecciona un Cliente y/o un Estado Entonces deberán mostrarse únicamente los proyectos que cumplan los criterios seleccionados. |
| Escenario: filtrar por Tipo de proyecto | Dado que existen proyectos asociados a distintos Tipos de proyecto Cuando el usuario selecciona un tipo de proyecto fijo Entonces deberán mostrarse únicamente los proyectos asociados al Tipo de proyecto seleccionado. |
| Escenario: utilizar tipos de proyecto fijos | Dado que la aplicación define un conjunto fijo de tipos de proyecto Cuando el usuario despliega el filtro Tipo de proyecto Entonces deberá poder seleccionar únicamente esas opciones fijas o la opción Todos. |
| Escenario: combinar filtros, incluido Responsable | Dado que existen proyectos con diferentes Clientes, Estados, Tipos de proyecto y Responsables Cuando el usuario selecciona simultáneamente un Cliente, un Estado, un Tipo de proyecto y un Responsable Entonces deberán mostrarse únicamente los proyectos que cumplan simultáneamente todos los criterios seleccionados. |
| Escenario: combinar búsqueda y filtros | Dado que el usuario se encuentra en el listado de proyectos Cuando ingresa un criterio de búsqueda por Nombre Y aplica uno o más filtros disponibles Entonces deberán mostrarse únicamente los proyectos que cumplan la búsqueda y los filtros seleccionados. |
| Escenario: Acceder al proyecto | Dado que el usuario visualiza un proyecto en el listado Cuando selecciona dicho proyecto Entonces deberá acceder a su ficha. |
| Prioridad: | Alta |
| Dependencias: | Registro de proyectos. |


## US-026 — Crear un proyecto manualmente

Figura 30. Mockup US-026 — Crear un proyecto manualmente

Tabla 37. Historia de usuario US-026 — Crear un proyecto manualmente


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero crear un proyecto sin necesidad de disponer previamente de un presupuesto. |
| PARA: | Para registrar y gestionar trabajos que se originan fuera del proceso de presupuestación. |
| DESCRIPCIÓN: | El Operador no podrá crear Proyectos manualmente por su rol; solo Supervisor y Administrador podrán hacerlo. Desde la sección Proyectos, Supervisor y Administrador podrán iniciar la creación manual de un proyecto indicando nombre, cliente, Tipo de proyecto seleccionado entre las opciones fijas de la aplicación, responsable y fechas. El proyecto podrá existir inicialmente sin presupuesto y recibirá automáticamente un código interno único con formato PRO-AAAAMMDD-NOMBRE DEL PROYECTO-####. Una vez creado correctamente, deberá quedar automáticamente en estado Pendiente de inicio. El usuario no deberá seleccionar manualmente el estado inicial. |
| Permisos aplicables: | Solo Supervisor y Administrador podrán crear manualmente un Proyecto por su rol base. No se admiten permisos individuales para esta acción. Todo proyecto deberá estar asociado a un cliente. |
| Reglas de negocio: | Un cliente en estado Inactivo no deberá encontrarse disponible para su selección al crear un proyecto. Un proyecto podrá existir sin presupuesto. Cada proyecto podrá tener como máximo un presupuesto. Se deberá registrar: Nombre. Cliente. El Tipo de proyecto deberá seleccionarse entre los tipos de proyecto fijos de la aplicación, sin administrar su catálogo. Responsable. Fecha de inicio. Fecha estimada de finalización. El responsable deberá seleccionarse entre usuarios activos y quedará automáticamente como participante del Proyecto al crearlo, sin segunda acción ni duplicación. El medio de comunicación no deberá solicitarse durante la creación manual del proyecto. Si el proyecto no posee un presupuesto cuyo envío al cliente se haya registrado correctamente, no deberá inventarse un medio de comunicación. El código interno único se generará automáticamente con formato PRO-AAAAMMDD-NOMBRE DEL PROYECTO-####, utilizando la fecha de creación, el nombre del proyecto y el siguiente correlativo global de proyectos disponible, de cuatro cifras e independiente del de presupuestos. El usuario no ingresará manualmente el código, que permanecerá asociado al proyecto aunque después se modifique su información general. Si el cliente no existe, su creación deberá ser explícita. Una vez creado, el usuario será dirigido a la ficha del proyecto. El estado inicial de todo proyecto creado manualmente será Pendiente de inicio y será asignado automáticamente por el sistema. |
| Requisitos relacionados: | RF: RF-48, RF-49, RF-87, RF-89. RNF: RNF-4, RNF-10, RNF-14, RNF-18. |
| Criterios de aceptación |  |
| Escenario: Crear correctamente | Dado que un Supervisor o Administrador selecciona Crear proyecto Cuando completa correctamente los datos requeridos y confirma Entonces el sistema deberá crear el proyecto Y asignarle automáticamente un código interno con estructura PRO-AAAAMMDD-NOMBRE DEL PROYECTO-####, utilizando su fecha de creación y su nombre Y utilizar el siguiente correlativo global de proyectos disponible Y asignarle automáticamente el estado Pendiente de inicio Y dirigir al usuario a su ficha, con el Responsable designado ya incorporado automáticamente a Participantes sin duplicarlo. |
| Escenario: Datos incompletos | Dado que el formulario contiene información obligatoria incompleta. Cuando el usuario intenta crear el proyecto. Entonces no deberá crearse y deberán indicarse claramente los campos pendientes. |
| Escenario: Crear cliente durante el proceso | Dado que el cliente requerido no existe. Cuando el usuario selecciona Crear nuevo cliente y lo registra correctamente. Entonces deberá quedar disponible inmediatamente para el proyecto actual. |
| Escenario: cliente inactivo | Dado que existe un cliente en estado Inactivo Cuando el usuario inicia la creación de un proyecto Entonces dicho cliente no deberá encontrarse disponible para selección. |
| Escenario: seleccionar Tipo de proyecto fijo | Dado que existen tipos de proyecto fijos definidos por la aplicación Cuando el usuario crea un proyecto manualmente Entonces deberá poder seleccionar uno de los tipos de proyecto fijos disponibles Y no deberá poder ingresar libremente un Tipo de proyecto inexistente. |
| Escenario: Operador no crea Proyecto manualmente | Dado un usuario con rol Operador Cuando intenta crear un Proyecto manualmente Entonces no deberá poder completar la creación por su rol base. |
| Prioridad: | Alta. |
| Dependencias: | Gestión de clientes (RF-49 y RF-89), usuarios, tipos de proyecto fijos y autorización por roles (RF-73). |


## US-027 — Crear un proyecto al aprobar un presupuesto.

Figura 31. Mockup US-027 — Crear un proyecto al aprobar un presupuesto

Tabla 38. Historia de usuario US-027 — Crear un proyecto al aprobar un presupuesto


| COMO: | Como Supervisor o Administrador con autorización para aprobar y completar la creación del Proyecto. |
|---|---|
| QUIERO: | Quiero completar la creación de un proyecto cuando se aprueba un presupuesto. |
| PARA: | Para continuar la gestión del trabajo sin volver a ingresar información ya registrada. |
| DESCRIPCIÓN: | Cuando un usuario con autorización efectiva registre la aprobación de un Presupuesto que todavía no tenga Proyecto, el sistema iniciará la creación del proyecto utilizando cliente, nombre y tipo de proyecto provenientes del presupuesto y solicitará únicamente los datos adicionales del proyecto que continúen siendo necesarios, como responsable y fechas. Si el presupuesto ya pertenece a un proyecto, no deberá crearse otro. Cuando se complete correctamente la creación de un nuevo proyecto a partir del presupuesto aprobado, el sistema deberá asignarle automáticamente el estado Pendiente de inicio e incorporar al Responsable designado a Participantes sin otra acción ni duplicación. |
| Permisos aplicables: | El Operador no podrá completar la creación derivada por su rol; Supervisor y Administrador podrán hacerlo conforme a RF-73. Un presupuesto podrá pertenecer como máximo a un proyecto. |
| Reglas de negocio: | El proyecto quedará vinculado al presupuesto. Se trasladarán: Cliente. Nombre. Tipo de proyecto. Deberá seleccionarse un responsable activo, que será participante automático del Proyecto al crearse sin duplicarlo. Deberán registrarse fecha de inicio y fecha estimada de fin. El medio de comunicación utilizado con el cliente no deberá solicitarse nuevamente durante la creación del proyecto; provendrá del envío registrado del presupuesto asociado y podrá consultarse desde el proyecto. El proyecto no podrá crearse sin responsable. Si se cancela el formulario, no se creará el proyecto ni quedará registrada definitivamente la aprobación. Si el presupuesto ya tiene proyecto, no se solicitarán estos datos. El nuevo proyecto recibirá automáticamente un código interno único con formato PRO-AAAAMMDD-NOMBRE DEL PROYECTO-####, utilizando su fecha de creación, su nombre y el siguiente correlativo global de proyectos disponible, independiente del presupuesto y compartido con los proyectos creados manualmente. El código del presupuesto no se reutilizará. El nuevo proyecto deberá crearse automáticamente en estado Pendiente de inicio y el usuario no deberá seleccionar manualmente el estado inicial. |
| Requisitos relacionados: | RF: RF-35, RF-52. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: Iniciar creación del proyecto | Dado un presupuesto pendiente de respuesta sin proyecto asociado Cuando un Supervisor o Administrador confirma Aprobar Entonces deberá abrirse el proceso de creación del proyecto con cliente, nombre y tipo ya completados. |
| Escenario: Completar creación del proyecto | Dado que el formulario de creación se encuentra abierto Cuando el usuario completa correctamente los datos requeridos y confirma Entonces deberán completarse conjuntamente la aprobación del presupuesto y la creación del proyecto sin exigir seleccionar un medio de comunicación, conservando la asociación con el presupuesto y permitiendo consultar posteriormente desde el proyecto el medio registrado para el envío de dicho presupuesto Y el nuevo proyecto deberá quedar en estado Pendiente de inicio con el Responsable designado incorporado automáticamente como participante sin duplicación Y recibirá automáticamente un código interno PRO-AAAAMMDD-NOMBRE DEL PROYECTO-#### con su fecha de creación, su nombre y el siguiente correlativo global de proyectos, sin reutilizar el código ni el correlativo del presupuesto. |
| Escenario: Evitar proyecto duplicado | Dado un presupuesto ya asociado a un proyecto Cuando sea aprobado Entonces no deberá crearse un segundo proyecto. |
| Escenario: Cancelar creación del proyecto | Dado que el usuario inició la aprobación de un presupuesto sin proyecto asociado Y se encuentra en el formulario de creación del proyecto Cuando cancela el proceso antes de confirmar la creación Entonces no deberá crearse ningún proyecto Y el presupuesto no deberá quedar registrado definitivamente como Aprobado. |
| Escenario: Operador no completa creación derivada | Dado un usuario con rol Operador Cuando intenta completar la creación de un Proyecto a partir de un Presupuesto aprobado Entonces no deberá poder completarla por su rol base. |
| Escenario: Error técnico al crear el proyecto | Dado que el usuario inició la aprobación de un presupuesto sin proyecto asociado Y completó y confirmó los datos requeridos para crear el proyecto Cuando ocurre un error que impide crear correctamente el proyecto Entonces no deberá crearse el proyecto ni quedar registrado de forma parcial Y el presupuesto no deberá quedar registrado definitivamente como Aprobado Y el sistema deberá informar al usuario que la operación no pudo completarse y permitir un nuevo intento sin generar proyectos duplicados. |
| Prioridad: | Alta |
| Dependencias: | US-019; RF-35. |


## US-028 — Consultar la ficha y resumen de un proyecto

Figura 32. Mockup US-028 — Consultar la ficha y resumen de un proyecto

Tabla 39. Historia de usuario US-028 — Consultar la ficha y resumen de un proyecto


| COMO: | Como Operador, Supervisor o Administrador con acceso efectivo al Proyecto. |
|---|---|
| QUIERO: | Quiero consultar una ficha centralizada de un Proyecto con su información general, resumen y secciones funcionales. |
| PARA: | Para conocer su situación y acceder a la información relacionada con ese trabajo. |
| DESCRIPCIÓN: | Cada Proyecto tendrá una ficha con información general y las secciones Resumen, Presupuesto, Documentos, Facturas de venta, Facturas de compra, Cronograma, Participantes y Resumen económico. El Resumen general mostrará cantidades de documentos, Facturas de venta, Facturas de compra y participantes, así como fecha de inicio y fecha estimada de finalización. La sección Resumen económico, distinta del Resumen general, presentará los cinco indicadores y la gestión autorizada de costos adicionales conforme a RF-103. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | La información deberá corresponder al proyecto seleccionado. Se mostrará: Nombre. Cliente. Estado. Tipo. Responsable. Fecha de creación. Medio de comunicación utilizado para el envío del presupuesto asociado, cuando exista un envío correctamente registrado. El resumen incluirá: Cantidad de documentos. Cantidad de Facturas de venta. Cantidad de Facturas de compra. Cantidad de participantes. Fecha de inicio. Fecha estimada de finalización. Las cantidades de Facturas de venta y Facturas de compra se calcularán y mostrarán independientemente con base en las Facturas activas actualmente asociadas al Proyecto, sin un único total combinado. El contador de documentos incluirá únicamente los documentos activos actualmente asociados, excluyendo los eliminados lógicamente. Las demás cantidades reflejarán la información actualmente asociada. El medio de comunicación mostrado corresponderá al envío correctamente registrado del Presupuesto asociado y no a una selección manual en la creación del Proyecto; si no hubo envío, no se inventará un medio. Las secciones independientes de Facturas mostrarán únicamente las Facturas activas de su categoría actualmente asociadas al Proyecto. Al registrar una Factura aumentará el contador de su categoría; al eliminarla disminuirá; una Factura ya guardada no podrá editarse ni reasignarse a otro Proyecto. Los PDF originales de Facturas no se contarán como documentos genéricos adicionales. Desde cada sección un usuario con permiso efectivo podrá iniciar el registro de la categoría correspondiente conforme a RF-77 o RF-104; la consulta y descarga del PDF se regirán por RF-83 y la eliminación lógica por RF-78. No habrá edición ni reasignación de Facturas. El Resumen económico permitirá consultar los cinco indicadores y gestionar costos adicionales exclusivamente con los permisos de RF-73 y las reglas de RF-103. |
| Requisitos relacionados: | RF: RF-54, RF-55, RF-82, RF-103, RF-73, RF-77, RF-78, RF-104. RNF: RNF-4, RNF-5, RNF-14. |
| Criterios de aceptación |  |
| Escenario: Consultar ficha del proyecto | Dado que el usuario seleccionó un proyecto Cuando accede a su ficha Entonces deberá visualizar sus datos generales y las pestañas correspondientes. |
| Escenario: Consultar resumen del proyecto | Dado un Proyecto con Facturas de venta y Facturas de compra asociadas Cuando un Operador, Supervisor o Administrador con acceso visualiza el Resumen Entonces deberá mostrar independientemente la Cantidad de Facturas de venta y la Cantidad de Facturas de compra, además de cantidad de documentos, cantidad de participantes, fecha de inicio y fecha estimada de finalización, según las asociaciones actuales. |
| Escenario: acceder al Resumen económico | Dado un usuario con acceso efectivo a un Proyecto Cuando abre la sección Resumen económico de su ficha Entonces visualiza Costo presupuestado sin margen, Precio de venta presupuestado, Costo según facturas de compra, Costo real total e Ingresos facturados Y la sección es distinta del Resumen general. |
| Escenario: consultar medio de comunicación registrado | Dado un proyecto con un presupuesto asociado cuyo envío fue correctamente registrado y posee un medio registrado Cuando el usuario consulta la ficha del proyecto Entonces deberá poder visualizar el medio de comunicación correspondiente al envío del presupuesto. |
| Escenario: proyecto sin envío de presupuesto registrado | Dado un proyecto sin presupuesto enviado al cliente Cuando el usuario consulta su ficha Entonces el sistema no deberá inventar un medio de comunicación. |
| Escenario: acceder a Facturas de venta | Dado que el usuario se encuentra en la ficha de un Proyecto Cuando selecciona Facturas de venta Entonces deberá acceder exclusivamente a las Facturas de venta asociadas a dicho Proyecto. |
| Escenario: registrar y contar una Factura | Dado un Proyecto con contadores separados de Facturas de compra y de venta Cuando un usuario autorizado registra una Factura de compra desde su sección Entonces aumenta únicamente el contador de compra de ese Proyecto. |
| Escenario: eliminar y descontar una Factura | Dado un Proyecto con una Factura de venta activa asociada Cuando un usuario autorizado confirma su eliminación Entonces disminuye el contador de venta de ese Proyecto y la Factura marcada como eliminada deja de figurar en su sección activa, conservándose el registro y el PDF original sin borrado físico. |
| Escenario: denegar reasignación de Facturas | Dada una Factura activa asociada al Proyecto A Cuando un usuario intenta reasignarla al Proyecto B Entonces el sistema mantiene su asociación original sin modificar los contadores de ambos Proyectos. |
| Escenario: denegar edición de Facturas | Dada una Factura activa asociada a un Proyecto Cuando un usuario intenta editar sus datos luego de guardarla Entonces el sistema rechaza la edición y el contador de Facturas permanece sin cambios. |
| Escenario: no contar PDF como documento genérico | Dado un Proyecto con una Factura activa y su PDF original Cuando el usuario consulta el Resumen general Entonces la Factura integra el contador de su categoría y su PDF no incrementa adicionalmente el contador de documentos genéricos. |
| Escenario: iniciar registro desde su sección | Dado un usuario con permiso efectivo para registrar Facturas de venta en un Proyecto Cuando selecciona Registrar factura desde Facturas de venta Entonces se inicia el registro conforme a RF-77 en ese Proyecto. |
| Escenario: denegar registro sin permiso | Dado un usuario con acceso al Proyecto pero sin permiso efectivo para registrar Facturas de compra Cuando intenta iniciar Registrar factura desde Facturas de compra Entonces se deniega esa operación sin crear una Factura. |
| Escenario: acceder a Facturas de compra | Dado que el usuario se encuentra en la ficha de un Proyecto Cuando selecciona Facturas de compra Entonces deberá acceder exclusivamente a las Facturas de compra asociadas a dicho Proyecto. |
| Prioridad: | Alta. |
| Dependencias: | RF-46; Proyectos registrados; registro de Facturas de venta y compra (RF-77, RF-104); permisos efectivos. |


## US-029 — Modificar información general del proyecto

Figura 33. Mockup US-029 — Modificar información general del proyecto

Tabla 40. Historia de usuario US-029 — Modificar información general del proyecto


| COMO: | Como Operador Responsable de su Proyecto, Supervisor o Administrador, conforme a los permisos fijos por rol. |
|---|---|
| QUIERO: | Quiero modificar la información general de un proyecto. |
| PARA: | Para mantener actualizados sus datos durante la ejecución. |
| DESCRIPCIÓN: | El Operador Responsable podrá modificar Nombre, Tipo de Proyecto, Fecha de inicio y Fecha estimada de finalización de su Proyecto, pero no podrá cambiar su Responsable. Supervisor y Administrador podrán modificar la información general permitida de cualquier Proyecto y cambiar su Responsable por un Usuario Activo. Un Operador no Responsable no podrá modificar la información general del Proyecto ni cambiar su Responsable. El Cliente permanecerá inmutable tras la creación. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Se modifica el mismo proyecto. No se crea un nuevo registro. Son modificables: Nombre. Tipo de proyecto. Cuando se modifique, deberá seleccionarse una de las opciones fijas de tipo de proyecto. Responsable, modificable únicamente por Supervisor o Administrador según sus permisos por rol. Fecha de inicio. Fecha estimada de finalización. El nuevo Responsable deberá ser un Usuario Activo; por sus permisos base, solo Supervisor o Administrador podrán cambiarlo; el Operador no podrá cambiar al Responsable. Al completar el cambio en el mismo Proyecto, el anterior dejará de ser Responsable y participante y el nuevo quedará como Responsable y participante, sin duplicados. Si el anterior tiene Tareas asignadas, el sistema informará el impedimento y conservará sin cambios al Responsable y los participantes hasta que se reasignen o eliminen las Tareas mediante RF-63 o RF-64 con los permisos correspondientes, sin hacerlo automáticamente; la actualización conjunta no podrá quedar parcialmente aplicada. El Operador Responsable podrá modificar Nombre, Tipo de Proyecto y fechas de su Proyecto, pero no cambiar el Responsable; el Operador no Responsable no podrá modificar la información general por su rol base. El cliente no podrá modificarse. El medio de comunicación correspondiente al envío del presupuesto tampoco podrá modificarse manualmente desde el proyecto. |
| Requisitos relacionados: | RF: RF-50; RF-56; RF-62; RF-63; RF-64. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: Modificar datos del proyecto | Dado un Operador Responsable del Proyecto, Supervisor o Administrador Cuando modifica datos válidos y guarda Entonces deberá actualizarse el mismo proyecto, únicamente en los datos permitidos para ese usuario; cuando cambie el Responsable, su asignación y las asociaciones de participantes deberán aplicarse de forma consistente, sin cambios parciales si la operación falla. |
| Escenario: Impedir modificación del cliente | Dado que el proyecto ya fue creado Cuando se abre la edición Entonces el cliente deberá permanecer sin posibilidad de modificación. |
| Escenario: modificar Tipo de proyecto | Dado un proyecto existente Y existen tipos de proyecto fijos disponibles en la aplicación Cuando un usuario autorizado modifica el Tipo de proyecto Entonces deberá seleccionar uno de los tipos de proyecto fijos disponibles en la aplicación Y, al guardar correctamente, el proyecto deberá quedar asociado al Tipo de proyecto seleccionado. |
| Escenario: Operador Responsable no cambia Responsable | Dado un Operador Responsable de un Proyecto Cuando modifica su información general Entonces podrá modificar Nombre, Tipo de Proyecto, Fecha de inicio y Fecha estimada de finalización Pero no podrá cambiar el Responsable por sus permisos base ni modificar el Cliente; solamente Supervisor o Administrador podrán cambiar al Responsable por otro usuario Activo. |
| Escenario: Operador no Responsable no modifica información general | Dado un Operador que no es Responsable de un Proyecto Cuando intenta modificar su información general Entonces no podrá modificarla por sus permisos base; no se admitirán excepciones individuales ni modificaciones del Cliente. |
| Escenario: Supervisor o Administrador cambia Responsable | Dado un Supervisor o Administrador Cuando modifica cualquier Proyecto y selecciona como nuevo Responsable a otro Usuario Activo Entonces, si el anterior no tiene Tareas asignadas, podrá completar el cambio sobre el mismo Proyecto: el anterior dejará de ser Responsable y participante, el nuevo quedará como Responsable y participante sin duplicados y el Cliente no cambiará; si hay Tareas asignadas, no se modificará ninguno de esos datos hasta resolverlas conforme a RF-63 o RF-64. |
| Prioridad: | Alta. |
| Dependencias: | RF-48, gestión de usuarios y permisos. |


## US-030 — Actualizar el estado de un proyecto

Figura 34. Mockup US-030 — Actualizar el estado de un proyecto

Tabla 41. Historia de usuario US-030 — Actualizar el estado de un proyecto


| COMO: | Como Operador Responsable de su Proyecto, Supervisor o Administrador, conforme a los permisos fijos por rol. |
|---|---|
| QUIERO: | Quiero actualizar manualmente el estado de un proyecto |
| PARA: | Para que la aplicación refleje correctamente su situación actual durante la ejecución. |
| DESCRIPCIÓN: | Cada proyecto deberá poseer un único estado actual. Los estados disponibles serán: Pendiente de inicio; En ejecución; En pausa; Finalizado; Cancelado. Todo proyecto será creado inicialmente en estado Pendiente de inicio. El cambio de estado se realizará manualmente desde la ficha del proyecto mediante la acción Cambiar estado. El usuario autorizado seleccionará uno de los estados disponibles y confirmará el cambio. Una vez confirmado correctamente, la ficha deberá reflejar inmediatamente el nuevo estado. Por sus permisos base, el Operador podrá actualizar el Estado si es Responsable del Proyecto; el Operador no podrá modificar el estado de un Proyecto del que no sea Responsable. Supervisor y Administrador podrán modificar el Estado de cualquier Proyecto. Ser participante de un proyecto no otorgará por sí solo autorización para modificar su estado. Los flujos Pendiente de inicio → En ejecución → Finalizado y En ejecución → En pausa → En ejecución son solo habituales y descriptivos; no restringen las transiciones. Cualquier Estado podrá cambiar manualmente a cualquiera de los otros Estados por acción de un usuario autorizado, sin restricciones según el Estado de origen. |
| Permisos aplicables: | Por sus permisos base, el Operador podrá cambiar el Estado solo del Proyecto del cual sea Responsable; no podrá cambiar el de Proyectos de los que no sea Responsable. Supervisor y Administrador podrán cambiar el Estado de cualquier Proyecto. |
| Reglas de negocio: | Todo proyecto deberá poseer un único estado actual. Los únicos estados disponibles serán Pendiente de inicio, En ejecución, En pausa, Finalizado y Cancelado. Todo nuevo proyecto deberá comenzar en Pendiente de inicio. El estado deberá cambiarse manualmente. El estado no deberá cambiar automáticamente por el transcurso del tiempo. La acción deberá realizarse desde la ficha del proyecto. El usuario deberá seleccionar un nuevo estado y confirmar el cambio. Un participante que no sea Responsable no podrá modificar el estado únicamente por su condición de participante. Los flujos Pendiente de inicio → En ejecución → Finalizado y En ejecución → En pausa → En ejecución serán descriptivos y no obligatorios. Un proyecto podrá pasar a Cancelado cuando se determine que no continuará. Finalizado representa que el trabajo previsto se completó normalmente. Cancelado representa que se decidió interrumpir anticipadamente el Proyecto. Los estados serán fijos y no administrables por los usuarios. No existen restricciones de transición: cualquier Estado podrá cambiar manualmente a cualquiera de los otros Estados cuando la acción corresponda a un usuario autorizado. |
| Requisitos relacionados: | RF: RF-51, RF-54, RF-73. RNF: RNF-4, RNF-10. |
| Criterios de aceptación |  |
| Escenario: estado inicial | Dado que se crea correctamente un nuevo proyecto Cuando finaliza el proceso de creación Entonces su estado deberá ser Pendiente de inicio. |
| Escenario: cambiar de Pendiente de inicio a En ejecución | Dado que un proyecto se encuentra en estado Pendiente de inicio Y el usuario es su Operador Responsable, un Supervisor o un Administrador Cuando selecciona En ejecución Y confirma el cambio Entonces el proyecto deberá quedar en estado En ejecución Y la ficha deberá reflejar inmediatamente el nuevo estado. |
| Escenario: pausar proyecto | Dado que un proyecto se encuentra En ejecución Y el usuario es su Operador Responsable, un Supervisor o un Administrador Cuando selecciona En pausa Y confirma el cambio Entonces el proyecto deberá quedar en estado En pausa. |
| Escenario: reanudar proyecto | Dado que un proyecto se encuentra En pausa Y el usuario es su Operador Responsable, un Supervisor o un Administrador Cuando selecciona En ejecución Y confirma el cambio Entonces el proyecto deberá volver a quedar En ejecución. |
| Escenario: finalizar proyecto | Dado que un proyecto se encuentra en ejecución Y el trabajo previsto fue llevado a término Y el usuario es su Operador Responsable, un Supervisor o un Administrador Cuando selecciona Finalizado Y confirma el cambio Entonces el proyecto deberá quedar en estado Finalizado. |
| Escenario: cancelar proyecto | Dado que se determina que un proyecto no continuará Y el usuario es su Operador Responsable, un Supervisor o un Administrador Cuando selecciona Cancelado Y confirma el cambio Entonces el proyecto deberá quedar en estado Cancelado. |
| Escenario: Responsable modifica su proyecto | Dado que un Operador es Responsable de un Proyecto Cuando accede a Cambiar estado de ese Proyecto Entonces deberá poder seleccionar cualquiera de los otros Estados disponibles y confirmar el cambio. |
| Escenario: Administrador modifica cualquier proyecto | Dado un usuario con rol Administrador Cuando accede a la acción Cambiar estado de cualquier proyecto Entonces deberá poder seleccionar uno de los estados disponibles y confirmar el cambio. |
| Escenario: participante sin autorización | Dado un Operador que participa en un Proyecto Pero no es su Responsable Y no posee la condición de Responsable que autoriza la acción Cuando intenta ejecutar Cambiar estado Entonces el sistema deberá impedir la operación. |
| Escenario: estado no cambia automáticamente | Dado un proyecto con un estado registrado Cuando transcurre el tiempo sin que un usuario autorizado confirme un cambio de estado Entonces el estado deberá permanecer sin modificaciones. |
| Escenario: transición libre entre Estados | Dado un Proyecto existente Y un usuario autorizado para cambiar su Estado Cuando selecciona cualquiera de los otros Estados disponibles y confirma el cambio Entonces el Proyecto deberá adoptar el nuevo Estado Sin que el sistema impida la transición por el Estado de origen. |
| Escenario: Supervisor cambia Estado de cualquier Proyecto | Dado un Supervisor Cuando accede a Cambiar estado de cualquier Proyecto Entonces podrá seleccionar cualquiera de los otros Estados y confirmar el cambio. |
| Escenario: Operador no Responsable no cambia Estado | Dado un Operador que no es Responsable de un Proyecto Cuando intenta cambiar su Estado Entonces no podrá cambiarlo por sus permisos base. |
| Prioridad: | Alta. |
| Dependencias: | RF-48; RF-54; gestión de usuarios, roles y permisos. |


## US-031 — Gestionar participantes del proyecto

Figura 35. Mockup US-031 — Gestionar participantes del proyecto

Tabla 42. Historia de usuario US-031 — Gestionar participantes del proyecto


| COMO: | Como Operador Responsable de su Proyecto, Supervisor o Administrador, conforme a los permisos fijos por rol. |
|---|---|
| QUIERO: | Quiero agregar y quitar participantes. |
| PARA: | Para mantener identificado quiénes intervienen en el trabajo. |
| DESCRIPCIÓN: | Todos los roles podrán consultar participantes. Por sus permisos base, un Operador podrá agregarlos o quitarlos solo cuando sea Responsable del Proyecto; no podrá gestionarlos en Proyectos de los que no sea Responsable; Supervisor y Administrador podrán gestionarlos en cualquier Proyecto. Para agregar participantes se seleccionarán usuarios Activos. |
| Permisos aplicables: | El Responsable del Proyecto podrá gestionar sus participantes. Supervisor y Administrador podrán gestionarlos en cualquier Proyecto. Un Operador que solo sea participante no podrá hacerlo por esa condición; no se admiten excepciones individuales. |
| Reglas de negocio: | Todo proyecto tendrá responsable. Solo los usuarios Activos podrán seleccionarse e incorporarse como nuevos participantes. La inactivación posterior de un participante no eliminará automáticamente su relación existente con el proyecto ni sus relaciones existentes con tareas, conforme a RF-75. No se podrá incorporar dos veces al mismo usuario, incluido el Responsable agregado automáticamente al crear o cambiar el Responsable conforme a RF-50. Tras un cambio válido, el anterior dejará de figurar como participante y el nuevo quedará incorporado sin duplicados; si el anterior tiene Tareas asignadas, el cambio no se completará hasta resolverlas y se conservarán Responsable y participantes sin cambios. El Responsable actual no podrá quitarse manualmente de la lista mientras conserve esa responsabilidad, aun sin Tareas. Quitar un participante no elimina al usuario de la aplicación. No se podrá quitar manualmente de un Proyecto a un participante mientras figure como Responsable de alguna Tarea de ese mismo Proyecto, incluso con 100 % de avance. Al intentarlo, el sistema deberá impedir la operación, informar que primero se deben reasignar las Tareas a otros participantes del Proyecto según RF-63 o eliminarlas con los permisos y la confirmación de RF-64, y conservar la asociación, las Tareas y sus Responsables. Cuando ya no tenga Tareas asignadas, podrá quitarse por el procedimiento normal solo si no es el Responsable actual del Proyecto; en tal caso, primero deberá cambiarse el Responsable conforme a RF-50. El permiso para quitar participantes no concede permiso para modificar o eliminar Tareas ni implica reasignaciones o eliminaciones automáticas; la inactivación conserva las relaciones históricas según RF-75. |
| Requisitos relacionados: | RF: RF-50; RF-56; RF-62; RF-63; RF-64. RNF: RNF-1, RNF-4. |
| Criterios de aceptación |  |
| Escenario: Agregar participante | Dado que el usuario es Operador Responsable, Supervisor o Administrador. Cuando ingresa a la sección Participante, luego selecciona un usuario activo y confirma. Entonces deberá incorporarse como participante. |
| Escenario: Evitar participante duplicado | Dado que el usuario ya participa. Cuando se intenta incorporarlo nuevamente. Entonces el sistema deberá impedir el duplicado, también cuando ese usuario ya fue incorporado automáticamente por ser el Responsable al crear el Proyecto o al cambiarlo conforme a RF-50. |
| Escenario: Quitar participante o impedir quitar al Responsable actual | Dado un participante existente que no es el Responsable actual del Proyecto y que no tiene Tareas asignadas como Responsable en ese Proyecto. Cuando un Operador Responsable, Supervisor o Administrador solicita quitarlo. Entonces deberá solicitarse confirmación antes de eliminar su relación con el proyecto. Si se intenta quitar directamente al Responsable actual, aun sin Tareas asignadas, se impedirá la operación y se informará que antes debe cambiarse el Responsable conforme a RF-50. |
| Escenario: impedir quitar participante con Tareas asignadas | Dado un Proyecto con un participante que tiene una o más Tareas asignadas como Responsable, aun cuando tengan 100 % de avance. Cuando un usuario autorizado intenta quitarlo de los Participantes. Entonces el sistema deberá impedir la operación. Y deberá informar que primero se deben reasignar a otros participantes del mismo Proyecto o eliminar las Tareas conforme a RF-63 y RF-64 y sus respectivos permisos. Y deberá conservar al participante asociado al Proyecto y las Tareas con sus Responsables existentes. |
| Escenario: quitar participante después de reasignar sus Tareas | Dado un participante que no es el Responsable actual y cuyas Tareas fueron reasignadas correctamente por usuarios autorizados a otros participantes del mismo Proyecto. Y ya no tiene Tareas asignadas como Responsable. Cuando un usuario autorizado solicita y confirma quitarlo del Proyecto. Entonces el participante deberá dejar de estar asociado al Proyecto. Y las Tareas reasignadas deberán conservar sus nuevos Responsables. |
| Escenario: quitar participante después de eliminar sus Tareas | Dado un participante que no es el Responsable actual y cuyas Tareas asignadas fueron eliminadas correctamente por usuarios autorizados conforme a RF-64. Y ya no tiene Tareas asignadas como Responsable. Cuando un usuario autorizado solicita y confirma quitarlo del Proyecto. Entonces deberá eliminarse únicamente su asociación con el Proyecto. Y el Usuario deberá permanecer registrado en la aplicación. |
| Escenario: consultar participantes y gestión del Supervisor | Dado un Operador, Supervisor o Administrador con acceso al Proyecto Cuando consulta Participantes Entonces podrá visualizar los participantes; Supervisor y Administrador podrán además gestionarlos en cualquier Proyecto. |
| Escenario: Operador solo participante no gestiona participantes | Dado un Operador participante que no es Responsable del Proyecto Cuando intenta agregar o quitar participantes Entonces no podrá gestionarlos por sus permisos base. |
| Prioridad: | Alta. |
| Dependencias: | Gestión de usuarios. |


## US-032 — Consultar y gestionar el presupuesto asociado al proyecto

Figura 36. Mockup US-032 — Consultar y gestionar el presupuesto asociado al proyecto

Tabla 43. Historia de usuario US-032 — Consultar y gestionar el presupuesto asociado al proyecto


| COMO: | Como Operador, Supervisor o Administrador con acceso al Proyecto. |
|---|---|
| QUIERO: | Quiero consultar el presupuesto asociado a un proyecto o iniciar uno cuando no exista. |
| PARA: | Para mantener relacionada la información comercial y la ejecución del trabajo. |
| DESCRIPCIÓN: | Un proyecto podrá disponer como máximo de un presupuesto. La pestaña Presupuesto mostrará información resumida y documentación del presupuesto existente; si todavía no existe, un Operador Responsable, Supervisor o Administrador podrá iniciar su elaboración. Un Operador no Responsable no podrá iniciarla por su rol base. Al iniciarla, se reutilizarán Cliente, Nombre y Tipo del Proyecto. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Máximo un Presupuesto por Proyecto. Todos los roles con acceso podrán consultar el Presupuesto asociado, información resumida, PDF disponible y generación/descarga de Excel a demanda. Sin Presupuesto, por sus permisos base podrán iniciarlo el Operador Responsable, Supervisor o Administrador según RF-53. Un presupuesto creado desde el proyecto quedará vinculado a él. Se reutilizarán: Cliente. Nombre. Tipo. Si ya existe presupuesto, no podrá crearse otro. Se mostrará: Código y nombre. Fecha. Estado. Importes totales en USD y UYU; si fue enviado, valores y cotización conservados al enviarlo. Si existe PDF final, podrá verse y descargarse. El Excel podrá generarse y descargarse a demanda a partir de los datos vigentes del presupuesto, conforme a US-014 y RF-37/38, sin almacenar ni administrar una copia versionada. Aprobar posteriormente este presupuesto no deberá crear otro proyecto |
| Requisitos relacionados: | RF: RF-37, RF-38, RF-53, RF-57, RF-58. RNF: RNF-5, RNF-6, RNF-4. |
| Criterios de aceptación |  |
| Escenario: Consultar proyecto sin presupuesto | Dado un Proyecto sin Presupuesto Cuando un Operador Responsable, Supervisor o Administrador accede a Presupuesto Entonces deberá indicarse que no existe y ofrecer Crear presupuesto. Cuando accede un Operador no Responsable, deberá indicarse que no existe, pero no podrá iniciar su creación por su rol base. |
| Escenario: Consultar proyecto con presupuesto | Dado un proyecto con presupuesto Cuando accede a dicha pestaña Entonces deberá mostrarse su información resumida y no aparecer la posibilidad de crear otro; si el usuario solicita el Excel, deberá generarse y descargarse a demanda sin exigir una copia almacenada. |
| Prioridad: | Alta. |
| Dependencias: | Módulo Presupuestación; US-014 para generar y descargar Excel. |


## US-033 — Subir documentación al proyecto

Figura 37. Mockup US-033 — Subir documentación al proyecto

Tabla 44. Historia de usuario US-033 — Subir documentación al proyecto


| COMO: | Como Operador Responsable o participante del Proyecto, Supervisor o Administrador, conforme a los permisos fijos por rol. |
|---|---|
| QUIERO: | Quiero cargar documentos en un proyecto |
| PARA: | Para centralizar la información documental correspondiente al trabajo |
| DESCRIPCIÓN: | Por sus permisos base, el Operador podrá subir documentos cuando sea Responsable o participante del Proyecto; Supervisor y Administrador podrán hacerlo en cualquier Proyecto. Desde Documentos se seleccionará obligatoriamente uno de los tipos de documento fijos definidos por la aplicación y se cargará un archivo compatible de hasta 100 MB. No habrá registro ni administración de nuevos tipos documentales desde el sistema. Cada documento quedará asociado exclusivamente al proyecto correspondiente y al Tipo seleccionado, sin permitir ingresar libremente uno inexistente. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Todo archivo quedará asociado al Proyecto. Por sus permisos base, el Operador podrá cargarlo si es Responsable o participante; Supervisor y Administrador podrán cargarlo en cualquier Proyecto. Tamaño máximo: 100 MB. Formatos: PDF, DOC, DOCX, XLS, XLSX, PPT, PPTX, CSV, TXT, JPG, JPEG, PNG, HEIC, TIF/TIFF, DWG y DXF. El usuario deberá seleccionar obligatoriamente un tipo de documento fijo de la aplicación. Los tipos disponibles serán opciones fijas, sin pantallas para crearlos, consultarlos administrativamente o eliminarlos. No podrá asignarse libremente un Tipo inexistente ni crearse desde el formulario de carga. |
| Requisitos relacionados: | RF: RF-59. RNF: RNF-4, RNF-5, RNF-6, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: Cargar documento válido | Dado un archivo de formato admitido y tamaño menor o igual a 100 MB Y un Operador Responsable o participante, Supervisor o Administrador seleccionó un tipo de documento fijo Cuando confirma la carga Entonces el documento deberá quedar disponible en el proyecto Y asociado al Tipo de documento seleccionado. |
| Escenario: seleccionar Tipo de documento fijo | Dado que existen tipos de documento fijos Cuando el usuario inicia la carga de un documento Entonces deberá seleccionar uno de los Tipos de documento disponibles Y no deberá poder asignar libremente un Tipo inexistente. |
| Escenario: Rechazar documento no válido | Dado un archivo que excede 100 MB o posee formato no admitido Cuando se intenta cargar Entonces deberá rechazarse y mostrarse un mensaje claro. |
| Escenario: permisos de carga documental | Dado un Operador que no es Responsable ni participante de un Proyecto Cuando intenta subir un documento al Proyecto Entonces el sistema deberá impedir la carga, porque el Operador no es Responsable ni participante. Supervisor y Administrador podrán subir documentos a cualquier Proyecto, respetando las validaciones de tipo, formato y tamaño. |
| Prioridad: | Alta |
| Dependencias: | RF-54; tipos de documento fijos de la aplicación; almacenamiento documental privado (RNF-5 y RNF-6). |


## US-034 — Consultar, buscar y filtrar documentos del proyecto

Figura 38. Mockup US-034 — Consultar, buscar y filtrar documentos del proyecto

Tabla 45. Historia de usuario US-034 — Consultar, buscar y filtrar documentos del proyecto


| COMO: | Como Operador, Supervisor o Administrador con acceso al Proyecto. |
|---|---|
| QUIERO: | Quiero consultar y localizar documentos de un proyecto |
| PARA: | Para acceder rápidamente a la documentación necesaria. |
| DESCRIPCIÓN: | Para Operador, Supervisor y Administrador con acceso al Proyecto, la pestaña Documentos permitirá buscar por Nombre, filtrar por Tipo de documento y Fecha de subida, descargar archivos y previsualizar únicamente PDF, JPG/JPEG y PNG. Los valores del filtro Tipo de documento serán las opciones fijas disponibles en la aplicación; al seleccionar uno, se mostrarán únicamente los documentos del proyecto actual asociados a ese Tipo. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Operador, Supervisor y Administrador podrán consultar, buscar, filtrar, descargar y previsualizar documentos compatibles del Proyecto, según su acceso. Solo se mostrarán documentos del Proyecto actual. Búsqueda por nombre. Filtros por tipo de documento fijo de la aplicación y Fecha de subida. Al seleccionar un Tipo, se mostrarán únicamente los documentos del proyecto actual asociados a él. Los filtros podrán aplicarse sin modificar datos. Se podrán descargar archivos. Únicamente los PDF, JPG/JPEG y PNG podrán previsualizarse en navegador bajo control de acceso; los demás formatos se descargarán, sin visor ni conversión interna. Los documentos eliminados lógicamente no aparecerán en el listado activo. |
| Requisitos relacionados: | RF: RF-60. RNF: RNF-4, RNF-5, RNF-6, RNF-10, RNF-14. |
| Criterios de aceptación |  |
| Escenario: Buscar o filtrar documentos | Dado que existen documentos Cuando el usuario busca o filtra Entonces deberán mostrarse únicamente los documentos correspondientes. |
| Escenario: filtrar por Tipo de documento | Dado un proyecto que contiene documentos asociados a distintos Tipos de documento Cuando el usuario selecciona uno de los tipos de documento fijos como filtro Entonces deberán mostrarse únicamente los documentos del proyecto asociados al Tipo seleccionado. |
| Escenario: Previsualizar documento | Dado un documento activo PDF, JPG/JPEG o PNG Cuando selecciona Ver Entonces deberá visualizarlo sin necesidad de descargarlo previamente. |
| Escenario: Descargar documento | Dado un documento del proyecto Cuando se seleccione “Descargar” Entonces se deberá iniciar la descarga de dicho documento |
| Escenario: Documento sin vista previa | Dado un documento activo de otro formato admitido, distinto de PDF, JPG/JPEG o PNG Cuando el usuario autorizado consulta las acciones disponibles Entonces podrá descargar el archivo, pero no se ofrecerá vista previa ni conversión interna. |
| Escenario: Documento eliminado lógicamente | Dado un documento previamente eliminado de forma lógica Cuando el usuario autorizado consulta o filtra los documentos activos del Proyecto Entonces el documento eliminado no deberá aparecer en los resultados. |
| Prioridad: | Alta |
| Dependencias: | RF-59. |


## US-035 — Eliminar lógicamente un documento del proyecto

Figura 39. Mockup US-035 — Eliminar un documento del proyecto

Tabla 46. Historia de usuario US-035 — Eliminar lógicamente un documento del proyecto


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero eliminar un documento que ya no corresponda al proyecto. |
| PARA: | Para mantener organizada la documentación disponible. |
| DESCRIPCIÓN: | Supervisor y Administrador podrán realizar la eliminación lógica; el Operador no podrá eliminar documentos por su rol ni recibir excepciones individuales. Un usuario autorizado podrá eliminar un documento asociado a un proyecto cuando este ya no corresponda. Antes de realizar la eliminación, el sistema deberá solicitar una confirmación identificando el archivo seleccionado. Si el usuario confirma, el documento dejará de aparecer en las consultas activas del proyecto y se conservarán su registro y su archivo original privado en Azure Blob Storage; si cancela, no se realizará ninguna modificación |
| Permisos aplicables: | El Operador no podrá eliminar documentos por su rol. Supervisor y Administrador podrán eliminarlos lógicamente con confirmación previa y validación del lado del servidor; no se concederán excepciones individuales. |
| Reglas de negocio: | Solo se elimina el documento seleccionado. Debe solicitarse confirmación. Tras confirmar, dejará de aparecer en las consultas activas del proyecto, conservándose su registro y el archivo original en Azure Blob Storage privado, sin eliminación física. |
| Requisitos relacionados: | RF: RF-61. RNF: RNF-1, RNF-4, RNF-5, RNF-6, RNF-8. |
| Criterios de aceptación |  |
| Escenario: Solicitar eliminación de documento | Dado un documento existente. Cuando un Supervisor o Administrador selecciona Eliminar. Entonces deberá mostrarse una confirmación identificando el archivo. |
| Escenario: Cancelar eliminación de documento | Dado el modal de confirmación. Cuando se selecciona Cancelar. Entonces no deberá realizarse ninguna modificación. |
| Escenario: Confirmar eliminación de documento | Dado que se muestra la ventana de confirmación Cuando el Supervisor o Administrador confirma la eliminación Entonces el documento seleccionado deberá dejar de aparecer en las consultas activas del proyecto y deberán conservarse privadamente su registro y archivo original. |
| Escenario: Operador no elimina documentos | Dado un usuario con rol Operador Cuando intenta eliminar un documento del Proyecto Entonces no deberá poder eliminarlo por su rol base. |
| Prioridad: | Media. |
| Dependencias: | RF-59, RF-60, RF-61, RF-73; RNF-1, RNF-4, RNF-5 y RNF-6. |


## US-036 — Consultar las facturas de venta y compra asociadas al proyecto

Figura 40. Mockup US-036 — Consultar las facturas de venta y compra asociadas al proyecto

Tabla 46. Historia de usuario US-036 — Consultar las facturas de venta y compra asociadas al proyecto


| COMO: | Como Operador, Supervisor o Administrador con acceso autorizado al Proyecto según su rol. |
|---|---|
| QUIERO: | Quiero consultar separadamente las Facturas de venta y compra asociadas al Proyecto y acceder a sus respectivos PDF originales. |
| PARA: | Para disponer de la documentación administrativa y la información económica registrada para ese trabajo. |
| DESCRIPCIÓN: | Desde la ficha del Proyecto, el usuario autorizado consulta por separado sus Facturas de venta y de compra activas, con sus datos y PDF originales privados. Supervisor o Administrador podrán iniciar el registro manual y eliminar lógicamente Facturas; Operador solo podrá consultar. No habrá edición, reasignación ni listados generales de Facturas. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Las dos secciones de Facturas deberán mostrar exclusivamente comprobantes activos de la categoría correspondiente asociados al Proyecto abierto. Cada Factura activa pertenecerá exactamente a un Proyecto; no se consultarán Facturas activas sin asociación. Los datos mostrados incluirán identificador interno, Tipo, Serie, Número real, Fecha, Cliente o Proveedor, Importe y Moneda. El PDF original de cada comprobante podrá visualizarse o descargarse exclusivamente con autorización del lado del servidor mediante RF-83. Solo Supervisor y Administrador podrán registrar Facturas y eliminarlas lógicamente; Operador no tendrá esas autorizaciones por su rol. No existen permisos individuales. Una Factura eliminada lógicamente no se presentará en las consultas activas; su PDF permanecerá privado y conservado. La consulta no modificará la Factura. No se ofrecerán acciones para editar datos ni reasignar Facturas a otro Proyecto. Las acciones y los cinco indicadores económicos respetarán las reglas de RF-77, RF-78, RF-103 y RF-104. |
| Requisitos relacionados: | RF: RF-54, RF-73, RF-77, RF-78, RF-82, RF-83, RF-103, RF-104. RNF: RNF-4, RNF-5, RNF-6, RNF-10, RNF-14. |
| Criterios de aceptación |  |
| Escenario: consultar ventas activas | Dado un Proyecto con Facturas de venta activas y un usuario autorizado Cuando abre Facturas de venta desde la ficha Entonces ve solo las Facturas de venta activas de ese Proyecto. |
| Escenario: consultar compras activas | Dado un Proyecto con Facturas de compra activas y un usuario autorizado Cuando abre Facturas de compra desde la ficha Entonces ve solo las Facturas de compra activas de ese Proyecto. |
| Escenario: consultar y descargar PDF privado | Dada una Factura activa de ese Proyecto y un usuario autorizado Cuando selecciona visualizar o descargar su PDF original Entonces se proporciona exclusivamente el PDF de esa Factura después de verificar la autorización del lado del servidor. |
| Escenario: sección sin Facturas | Dado un Proyecto sin Facturas activas de una categoría Cuando el usuario consulta esa sección Entonces se presenta vacía sin incluir comprobantes de otros Proyectos. |
| Escenario: registrar desde Proyecto según rol | Dado un Supervisor o Administrador con acceso autorizado a un Proyecto Cuando inicia el registro manual desde la sección de Facturas correspondiente Entonces se abre el formulario con el Proyecto fijado y el PDF original obligatorio. |
| Escenario: Operador no registra | Dado un usuario Operador Cuando intenta registrar o eliminar una Factura Entonces se rechaza la operación desde el servidor sin cambiar datos. |
| Escenario: Factura eliminada no aparece | Dada una Factura eliminada lógicamente Cuando se consultan las Facturas activas del Proyecto Entonces no aparece en los resultados ni sus contadores, aunque se conservan el registro y PDF privado. |
| Escenario: prohibir edición y reasignación | Dada una Factura ya registrada Cuando el usuario consulta su detalle Entonces no dispone de acciones para modificar sus datos ni cambiarla de Proyecto. |
| Escenario: Proyecto único | Dada una Factura activa registrada válidamente Cuando se consulta su asociación Entonces pertenece exactamente a un Proyecto y no figura simultáneamente en otro. |
| Escenario: denegar acceso directo al PDF | Dada una Factura con PDF original privado y un usuario sin acceso a su Proyecto Cuando solicita directamente el archivo al servidor Entonces se deniega la visualización o descarga sin exponer el PDF. |
| Prioridad: | Alta. |
| Dependencias: | US-028, US-048 y US-068; RF-82 y permisos según RF-73; gestión de Facturas activas y PDF privados. |


## US-037 — Registrar una tarea en el proyecto

Figura 41. Mockup US-037 — Registrar una tarea en el proyecto

Tabla 47. Historia de usuario US-037 — Registrar una tarea en el proyecto


| COMO: | Como Operador Responsable de su Proyecto, Supervisor o Administrador, conforme a los permisos fijos por rol. |
|---|---|
| QUIERO: | Quiero registrar tareas dentro del proyecto |
| PARA: | Para planificar y realizar seguimiento de las actividades necesarias para su ejecución. |
| DESCRIPCIÓN: | Por sus permisos base, el Operador podrá registrar Tareas solo si es Responsable del Proyecto; Supervisor y Administrador podrán hacerlo en cualquier Proyecto. Al crear una Tarea serán obligatorios exclusivamente Nombre, Responsable, Fecha de inicio y Horas estimadas, y el Responsable deberá ser participante del Proyecto. Cada tarea podrá registrar Nombre, Descripción, Responsable, Fecha de inicio, Fecha de fin, Horas estimadas, Horas registradas y Porcentaje de avance. El Porcentaje de avance permitirá representar el nivel actual de ejecución de la tarea mediante un valor comprendido entre 0 % y 100 %. La tarea no contará con campos Estado ni Prioridad. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Toda tarea estará asociada al proyecto actual. Se contemplarán: Nombre. Descripción. Responsable. Fecha inicio. Fecha fin (opcional). Horas estimadas. Horas registradas. Porcentaje de avance. El Porcentaje de avance deberá estar comprendido entre 0 % y 100 %, ambos inclusive. No se permitirán valores inferiores a 0 % ni superiores a 100 %. Un Porcentaje de avance de 0 % representará una tarea todavía no iniciada; 100 %, una tarea completamente terminada; y los valores intermedios, avance parcial. La tarea no tendrá campos Estado ni Prioridad. Al crearla serán obligatorios exclusivamente Nombre, Responsable, Fecha de inicio y Horas estimadas; Descripción, Fecha de fin, Horas registradas y Porcentaje de avance no serán obligatorios, salvo las reglas funcionales vigentes. El Responsable se seleccionará únicamente entre participantes del Proyecto, sin incorporar automáticamente a nadie como participante. El Operador podrá registrar Tareas en su Proyecto si es Responsable por sus permisos base. Supervisor y Administrador podrán hacerlo en cualquier Proyecto por sus permisos base. |
| Requisitos relacionados: | RF: RF-62. RNF: RNF-10, RNF-18, RNF-4. |
| Criterios de aceptación |  |
| Escenario: Iniciar registro de tarea | Dado que el usuario se encuentra en Cronograma Cuando selecciona Agregar tarea Entonces deberá acceder al formulario correspondiente. |
| Escenario: Registrar tarea | Dado que un Operador Responsable del Proyecto, Supervisor o Administrador completó Nombre, Responsable participante, Fecha de inicio y Horas estimadas Y, si registró Porcentaje de avance, este es válido Cuando confirma el registro Entonces la Tarea deberá quedar asociada al Proyecto correspondiente, aun sin Fecha de fin, disponible para consulta y gestión y en «Tareas sin fecha de finalización» hasta que se indique una Fecha de fin válida. |
| Escenario: registrar porcentaje mínimo | Dado que el usuario está registrando una tarea Cuando ingresa 0 % como Porcentaje de avance Entonces el sistema deberá aceptar el valor. |
| Escenario: registrar porcentaje máximo | Dado que el usuario está registrando una tarea Cuando ingresa 100 % como Porcentaje de avance Entonces el sistema deberá aceptar el valor. |
| Escenario: porcentaje inferior al mínimo | Dado que el usuario está registrando una tarea Cuando ingresa un Porcentaje de avance inferior a 0 % Entonces el sistema deberá rechazar el valor Y no deberá guardar la tarea con dicho porcentaje. |
| Escenario: porcentaje superior al máximo | Dado que el usuario está registrando una tarea Cuando ingresa un Porcentaje de avance superior a 100 % Entonces el sistema deberá rechazar el valor Y no deberá guardar la tarea con dicho porcentaje. |
| Escenario: campos obligatorios al crear Tarea | Dado que un usuario autorizado crea una Tarea Cuando falta Nombre, Responsable, Fecha de inicio u Horas estimadas Entonces no deberá poder completarse correctamente el registro. |
| Escenario: Responsable de Tarea debe participar en Proyecto | Dado un Usuario que no es participante del Proyecto Cuando se intenta seleccionarlo como Responsable de una Tarea Entonces no deberá ser posible asignarlo ni incorporarlo automáticamente como participante. |
| Escenario: Operador no Responsable no registra Tareas | Dado un Operador que no es Responsable del Proyecto Cuando intenta registrar una Tarea Entonces no podrá registrarla por sus permisos base; en todos los casos el Responsable de la Tarea deberá ser participante del Proyecto. Supervisor y Administrador podrán registrarla en cualquier Proyecto. |
| Prioridad: | Alta |
| Dependencias: | Gestión de usuarios y participantes; RF-56. |


## US-038 — Modificar o eliminar una tarea

Figura 42. Mockup US-038 — Modificar o eliminar una tarea

Tabla 48. Historia de usuario US-038 — Modificar o eliminar una tarea


| COMO: | Como Operador asignado a una Tarea, Operador Responsable de su Proyecto, Supervisor o Administrador, según las acciones permitidas por el rol. |
|---|---|
| QUIERO: | Quiero modificar o eliminar tareas del proyecto. |
| PARA: | Para mantener el cronograma alineado con la realidad de la ejecución. |
| DESCRIPCIÓN: | Un Operador asignado podrá actualizar descripción, horas registradas y Porcentaje de avance de su Tarea; si es Responsable del Proyecto, podrá gestionar la planificación, modificar fechas y asignar o cambiar Responsables de Tareas únicamente entre participantes del Proyecto. Supervisor y Administrador podrán modificar cualquier Tarea y su planificación. Un Operador no podrá eliminar Tareas por su rol base; Supervisor y Administrador podrán eliminarlas previa confirmación. No habrá campos inmodificables por el Estado de la Tarea o del Proyecto. Una Tarea podrá actualizarse sin generar un registro nuevo. Entre los datos modificables se encontrará el Porcentaje de avance, que podrá actualizarse a medida que progrese el trabajo. También podrá eliminarse, previa confirmación, si fue creada por error o dejó de corresponder. |
| Permisos aplicables: | El Operador no podrá eliminar Tareas por su rol base; no se admiten excepciones individuales. Supervisor y Administrador podrán eliminarlas. La eliminación afecta únicamente a la Tarea seleccionada. |
| Reglas de negocio: | La modificación se realizará sobre la misma tarea. El Porcentaje de avance podrá actualizarse sin generar una tarea nueva. Los cambios deberán reflejar su situación actual. Los permisos de modificación dependerán del usuario: el Operador asignado podrá actualizar descripción, horas registradas y Porcentaje de avance solo de su Tarea; el Operador Responsable del Proyecto podrá modificar fechas y planificación y asignar o cambiar al Responsable únicamente por un participante; Supervisor y Administrador podrán modificar cualquier Tarea y su planificación. No se admitirán excepciones individuales para ampliar las acciones de planificación del Operador; el Responsable de una Tarea deberá ser participante del Proyecto. No existirán restricciones adicionales por el Estado de la Tarea o del Proyecto. Todo Porcentaje de avance registrado deberá encontrarse entre 0 % y 100 %, ambos inclusive; no se guardarán valores fuera de ese rango. La modificación no deberá incorporar campos Estado ni Prioridad a la tarea. Eliminar requiere confirmación. Tras eliminarla, no deberá continuar formando parte del cronograma. |
| Requisitos relacionados: | RF: RF-63, RF-64. RNF: RNF-1, RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: Modificar tarea | Dado una tarea existente Cuando un usuario con permiso modifica información válida y guarda Entonces deberá actualizarse esa tarea sin crear otra; una Fecha de fin válida la incorporará al Gantt, y su ausencia no impedirá guardar ni gestionarla. |
| Escenario: actualizar Porcentaje de avance | Dado que existe una tarea registrada Cuando el usuario modifica su Porcentaje de avance por un valor comprendido entre 0 % y 100 % Y guarda los cambios Entonces deberá actualizarse la misma tarea Y deberá conservarse el nuevo Porcentaje de avance. |
| Escenario: impedir porcentaje inválido al modificar | Dado que existe una tarea registrada Cuando el usuario intenta guardar un Porcentaje de avance inferior a 0 % o superior a 100 % Entonces el sistema deberá impedir guardar dicho valor. |
| Escenario: Solicitar eliminación de tarea | Dado una tarea existente Cuando un Supervisor o Administrador selecciona eliminar Entonces deberá solicitarse confirmación. |
| Escenario: Confirmar eliminación de tarea | Dado una tarea existente Cuando un Supervisor o Administrador confirma la eliminación Entonces la tarea deberá dejar de formar parte del proyecto. |
| Escenario: Cancelar eliminación. | Dado una tarea existente Cuando se cancela la eliminación Entonces la tarea deberá seguir existiendo y no debe haber ningún cambio. |
| Escenario: Operador asignado modifica solo sus datos de ejecución | Dado un Operador participante con una Tarea asignada Cuando modifica descripción, horas registradas o Porcentaje de avance de esa Tarea Entonces podrá guardar el cambio; no podrá planificar otras Tareas solo por ser participante. |
| Escenario: cambiar Responsable de Tarea por participante | Dado un Operador Responsable del Proyecto, Supervisor o Administrador Cuando cambia el Responsable de una Tarea Entonces solo podrá seleccionar a un participante del Proyecto, sin incorporarlo automáticamente. |
| Escenario: Operador no elimina Tareas | Dado un Operador por su rol Cuando intenta eliminar una Tarea Entonces no deberá poder eliminarla por su rol base; Supervisor y Administrador sí podrán eliminarla con confirmación previa. |
| Prioridad: | Alta. |
| Dependencias: | RF-62. |


## US-039 — Consultar y filtrar tareas del proyecto

Figura 43. Mockup US-039 — Consultar y filtrar tareas del proyecto

Tabla 49. Historia de usuario US-039 — Consultar y filtrar tareas del proyecto


| COMO: | Como Operador, Supervisor o Administrador con acceso al Proyecto. |
|---|---|
| QUIERO: | Quiero localizar las tareas del proyecto |
| PARA: | Para comprender y seguir la planificación del proyecto. |
| DESCRIPCIÓN: | Operador, Supervisor y Administrador podrán consultar las Tareas. Las Tareas podrán buscarse por Nombre y filtrarse por Responsable, Fecha de inicio y Fecha de fin. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Los filtros afectan únicamente a las tareas del proyecto actual. Los filtros y la búsqueda no modifican las tareas. Se podrá buscar por Nombre de tarea. Se podrá filtrar por: Responsable. Fecha de inicio. Fecha de fin. No existirán filtros por Estado ni Prioridad, ya que la tarea no contará con estos campos. No se define un filtro por Porcentaje de avance ni otros criterios adicionales. |
| Requisitos relacionados: | RF: RF-65. RNF: RNF-14, RNF-4. |
| Criterios de aceptación |  |
| Escenario: buscar tareas | Dado un proyecto con tareas registradas Cuando el usuario ingresa parte o la totalidad del Nombre de una tarea Entonces deberán mostrarse las tareas correspondientes al criterio ingresado. |
| Escenario: Filtrar tareas | Dado un proyecto con tareas registradas Cuando el usuario aplica uno o más filtros entre Responsable, Fecha de inicio y Fecha de fin Entonces el listado deberá mostrar únicamente las tareas que cumplan los criterios seleccionados. |
| Prioridad: | Media |
| Dependencias: | RF-62. |


## US-040 — Consultar el catálogo de materiales

Figura 44. Mockup US-040 — Consultar el catálogo de materiales

Tabla 50. Historia de usuario US-040 — Consultar el catálogo de materiales


| COMO: | Como Operador, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero consultar los materiales registrados y sus referencias de precios. |
| PARA: | Para conocer los materiales disponibles y los precios registrados para cada proveedor. |
| DESCRIPCIÓN: | Por sus permisos base, Operador, Supervisor y Administrador podrán acceder al catálogo centralizado de EOP, buscar y filtrar Materiales y consultar sus referencias sin ser Responsables de un Presupuesto; se aplicarán únicamente los permisos fijos por rol y las restricciones correspondientes de RF-73. Desde el catálogo, la acción Editar de un Material solo se mostrará a los usuarios con permiso efectivo de edición conforme a US-071 y RF-108; el acceso al catálogo no otorga permiso de modificación. Para cada material deberán poder visualizarse las referencias de precios asociadas a sus distintos proveedores, incluyendo el precio actual, la moneda (UYU o USD) y la fecha de última actualización. El catálogo permitirá buscar por Nombre del Material y filtrar por Unidad y Proveedor; podrán combinarse los criterios y solo se mostrarán los Materiales que cumplan todos los aplicados. Los precios y sus fechas de actualización corresponderán al mantenimiento manual realizado en el catálogo; no habrá sincronización automática con Fivisa. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Un material podrá estar asociado a distintos proveedores. Cada combinación material-proveedor mantendrá su propio precio actual y su moneda UYU o USD. Cada combinación material-proveedor mantendrá su propia fecha de última actualización. La información mostrada deberá corresponder a los datos actualmente registrados. Se podrá buscar por Nombre del Material y filtrar por Unidad y Proveedor, de forma individual o combinada; solo se mostrarán Materiales que satisfagan simultáneamente todos los criterios aplicados. Precio, moneda y fecha de actualización serán información consultable, no criterios de búsqueda o filtrado. No existirá un indicador de sincronización automática de precios, puesto que todas las referencias del catálogo se mantienen manualmente, incluso las del proveedor Fivisa. |
| Requisitos relacionados: | RF: RF-67, RF-73. RNF: RNF-14, RNF-4. |
| Criterios de aceptación |  |
| Escenario: Consultar catálogo | Dado que existen materiales registrados Cuando el usuario accede al catálogo de EOP Entonces deberá visualizar los materiales disponibles. |
| Escenario: Consultar precios por proveedor | Dado un material asociado a uno o más proveedores Cuando el usuario consulta dicho material Entonces deberá poder visualizar el proveedor, precio actual, moneda UYU/USD y fecha de última actualización correspondientes a cada relación. |
| Escenario: consultar referencia de Fivisa sin sincronización | Dado un material con una referencia de precio del proveedor Fivisa registrada en el catálogo Cuando un usuario autorizado consulta sus referencias Entonces visualiza el proveedor, el precio, la moneda y la fecha de actualización registrados manualmente Y no se ofrece ni indica una sincronización automática con Fivisa. |
| Escenario: Buscar por Nombre | Dado que existen Materiales registrados Cuando el usuario ingresa un Nombre o parte de él Entonces deberán mostrarse los Materiales correspondientes. |
| Escenario: Filtrar por Unidad | Dado que existen Materiales con diferentes Unidades Cuando el usuario selecciona una Unidad Entonces deberán mostrarse únicamente los Materiales asociados a dicha Unidad. |
| Escenario: Filtrar por Proveedor | Dado que existen Materiales relacionados con diferentes Proveedores Cuando el usuario selecciona un Proveedor Entonces deberán mostrarse únicamente los Materiales relacionados con dicho Proveedor. |
| Escenario: Combinar criterios | Dado que el usuario aplica más de un criterio entre Nombre, Unidad y Proveedor Cuando ejecuta la búsqueda o filtrado Entonces deberán mostrarse únicamente los Materiales que cumplan simultáneamente todos los criterios seleccionados. |
| Prioridad: | Alta. |
| Dependencias: | Catálogo de materiales; gestión de proveedores. |


## US-041 — Registrar material nuevo exclusivamente desde catálogo

Figura 45. Mockup US-041 — Registrar un material desde el catálogo

Tabla 51. Historia de usuario US-041 — Registrar material nuevo exclusivamente desde catálogo


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero registrar manualmente un nuevo material desde el catálogo. |
| PARA: | Para incorporarlo sin necesidad de encontrarme elaborando un presupuesto. |
| DESCRIPCIÓN: | Desde Catálogo de Materiales, Supervisor o Administrador podrá registrar un Material nuevo con nombre y unidad fija, seleccionando un Proveedor previamente registrado y anotando manualmente precio mayor que cero y moneda UYU/USD. El Material y su primera relación válida se guardarán en una sola operación. No se crearán unidades de medida ni proveedores desde este formulario. Los materiales nuevos no podrán registrarse desde un presupuesto y no habrá extracción o integración automática de precios con Fivisa. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Nombre obligatorio y único por denominación completa normalizada; Unidad obligatoria de la lista fija. Proveedor existente obligatorio; si falta, registrarlo antes desde Proveedores. Primera relación material-proveedor con precio manual mayor que cero y moneda UYU/USD; registro atómico e incorporación de fecha de actualización. No se permite guardar Material sin primera relación válida. Un material creado queda disponible para futuras selecciones. El Operador no puede crear Materiales mediante permisos individuales. |
| Requisitos relacionados: | RF: RF-22, RF-23, RF-67, RF-68, RF-73. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: registrar material válido | Dado un Supervisor o Administrador en el catálogo Cuando ingresa un nombre nuevo, selecciona unidad fija, Proveedor existente, precio mayor que cero y moneda válida Entonces se registran Material y primera relación material-proveedor juntos. |
| Escenario: no registrar desde presupuesto | Dado que un usuario elabora un Presupuesto y no encuentra un Material Cuando intenta agregarlo desde el Presupuesto Entonces no aparece un alta de Material allí Y deberá registrarse previamente desde el catálogo. |
| Escenario: proveedor inexistente | Dado que el Proveedor requerido no está registrado Cuando el usuario carga un nuevo Material Entonces debe registrar primero ese Proveedor desde su propio módulo y no puede hacerlo en la pantalla de Materiales. |
| Escenario: unidad fija | Dado la pantalla de alta de Material Cuando se selecciona Unidad Entonces solo aparecen unidades de medida fijas y no existe una acción de crear unidades. |
| Escenario: nombre duplicado | Dado que ya existe un Material con el mismo nombre normalizado Cuando se intenta confirmar el alta Entonces se rechaza la duplicación y no se registra otra relación parcial. |
| Escenario: precio inválido | Dado un precio igual a cero, negativo o ausente Cuando se confirma el alta Entonces se rechaza la operación sin crear un Material incompleto. |
| Escenario: Fivisa manual | Dado que el Proveedor existente elegido es Fivisa Cuando se registra el Material Entonces el precio se ingresa manualmente, sin vínculos con productos externos. |
| Escenario: Operador sin permiso | Dado un Operador consultando el catálogo Cuando intenta el alta de Material Entonces el servidor rechaza la acción sin cambiar datos. |
| Prioridad: | Alta. |
| Dependencias: | US-040 (catálogo); US-074 (Proveedores existentes); US-010B (nuevas referencias posteriores). |


## US-042 — Iniciar y cerrar sesión

Figura 46. Mockup US-042 — Iniciar y cerrar sesión

Tabla 52. Historia de usuario US-042 — Iniciar y cerrar sesión


| COMO: | Como usuario habilitado de EOP |
|---|---|
| QUIERO: | Quiero iniciar sesión de forma segura en la aplicación |
| PARA: | Para acceder a las funcionalidades para las cuales tengo autorización. |
| DESCRIPCIÓN: | La autenticación se realizará mediante Microsoft Entra ID. La aplicación no gestionará directamente las contraseñas del usuario. El acceso deberá completar además el mecanismo de autenticación multifactor configurado mediante el servicio externo de identidad. Una vez autenticado, el sistema identificará al usuario para aplicar sus permisos. En el primer acceso de un usuario registrado en EOP que todavía no se encuentre vinculado a una identidad de Microsoft Entra ID, el sistema deberá establecer la vinculación cuando la dirección de correo corporativo registrada en EOP coincida con la identidad autenticada. Una vez establecida la vinculación, los accesos posteriores deberán identificar al usuario correspondiente mediante la identidad externa previamente vinculada, incluso si posteriormente se actualiza el Correo corporativo almacenado en EOP conforme a RF-76. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Solo los usuarios habilitados podrán acceder. No existirá registro público de usuarios. Las credenciales serán validadas por Microsoft Entra ID. La aplicación no almacenará contraseñas. El acceso requerirá completar la autenticación multifactor. Un usuario Inactivo no podrá acceder. El usuario podrá cerrar su sesión. |
| Requisitos relacionados: | RF: RF-69, RF-71. RNF: RNF-2, RNF-3, RNF-4, RNF-9, RNF-11. |
| Criterios de aceptación |  |
| Escenario: Iniciar sesión correctamente | Dado un usuario habilitado Cuando completa correctamente la autenticación requerida por Microsoft Entra ID y el segundo factor Entonces deberá acceder a la aplicación con los permisos correspondientes. |
| Escenario: No completar MFA | Dado que el usuario inició la autenticación Cuando no completa correctamente el segundo factor requerido Entonces no deberá acceder a las funcionalidades protegidas. |
| Escenario: Usuario inactivo | Dado un usuario registrado en estado Inactivo Cuando intenta acceder a la aplicación Entonces no deberá poder utilizar sus funcionalidades. |
| Escenario: Cerrar sesión | Dado un usuario autenticado Cuando selecciona cerrar sesión Entonces deberá finalizar su sesión activa. |
| Escenario: Vincular identidad en el primer acceso | Dado un usuario Activo registrado en EOP que todavía no se encuentra vinculado a una identidad externa Cuando completa correctamente la autenticación mediante Microsoft Entra ID y MFA Y la identidad autenticada coincide con el correo corporativo registrado en EOP Entonces el sistema deberá vincular la identidad autenticada con el usuario registrado Y deberá permitirle acceder a la aplicación con los permisos correspondientes. |
| Escenario: Identidad no registrada en EOP | Dado un usuario que completa correctamente la autenticación mediante Microsoft Entra ID Cuando no existe en EOP un usuario habilitado correspondiente a dicha identidad Entonces no deberá permitirse el acceso a las funcionalidades protegidas de la aplicación. |
| Prioridad: | Alta. |
| Dependencias: | Microsoft Entra ID; gestión de usuarios. |


## US-043 — Registrar un usuario de EOP desde la administración

Figura 47. Mockup US-043 — Registrar un usuario de EOP desde la administración

Tabla 53. Historia de usuario US-043 — Registrar un usuario de EOP desde la administración


| COMO: | Como Administrador |
|---|---|
| QUIERO: | Quiero registrar usuarios de EOP desde la administración del sistema |
| PARA: | Para habilitar a integrantes de EOP a utilizar la aplicación. |
| DESCRIPCIÓN: | No existe auto-registro. La identidad utilizada para acceder a la aplicación deberá existir previamente en Microsoft Entra ID y su creación se realizará fuera de la aplicación EOP. El Administrador deberá registrar al usuario desde la gestión de usuarios de EOP, indicando Nombre completo y Correo corporativo, y asignándole uno de los roles base disponibles: Administrador, Supervisor u Operador. No se exigirán otros datos personales ni se seleccionará manualmente el Estado. La aplicación EOP no deberá crear ni modificar la identidad de Microsoft Entra ID. Durante el primer inicio de sesión, el sistema deberá vincular el usuario registrado en EOP con la identidad autenticada cuando exista coincidencia entre el correo corporativo registrado y la identidad autenticada. Una vez establecida la vinculación, los accesos posteriores deberán identificar al usuario mediante la identidad externa previamente vinculada. Para el alta, el Administrador registrará Nombre completo, Correo corporativo y Rol base. El Estado se asignará automáticamente como Activo y no se seleccionará manualmente. |
| Permisos aplicables: | Solo un Administrador podrá registrar usuarios. |
| Reglas de negocio: | Los usuarios no podrán registrarse a sí mismos. La identidad utilizada para acceder deberá existir previamente en Microsoft Entra ID. Nombre completo y Correo corporativo serán obligatorios para registrar el Usuario en EOP, junto con la asignación de un Rol base. El Correo corporativo será único entre Usuarios de EOP: antes del alta se verificará si ya está registrado para otro Usuario y, en tal caso, se impedirá el alta y se informará la duplicación. EOP no creará ni modificará la identidad de Microsoft Entra ID. Todo usuario deberá poseer uno de los roles base Administrador, Supervisor u Operador. Todo usuario registrado quedará automáticamente en estado Activo, sin que el Administrador seleccione manualmente ese Estado durante el alta. La aplicación no conocerá ni almacenará la contraseña utilizada para autenticarse. La vinculación entre el usuario de EOP y la identidad externa se realizará durante el primer acceso cuando corresponda. No se requerirán otros datos personales para el alta. |
| Requisitos relacionados: | RF: RF-70, RF-72. RNF: RNF-2, RNF-4, RNF-10, RNF-11, RNF-18. |
| Criterios de aceptación |  |
| Escenario: Registrar usuario correctamente | Dado un Administrador Cuando registra Nombre completo y Correo corporativo, selecciona uno de los roles Administrador, Supervisor u Operador y confirma el alta Entonces deberá crearse el usuario dentro de EOP Y quedar inicialmente en estado Activo Y quedar preparado para su vinculación con la identidad externa durante el primer acceso. |
| Escenario: Impedir alta con Correo corporativo duplicado | Dado un Administrador que registra un Usuario en EOP Y el Correo corporativo indicado ya está registrado para otro Usuario de EOP Cuando intenta confirmar el alta Entonces el sistema deberá impedir la creación del nuevo Usuario Y deberá informar que el Correo corporativo ya se encuentra registrado. |
| Escenario: Vincular identidad en el primer acceso | Dado un usuario registrado y Activo en EOP que todavía no se encuentra vinculado Cuando inicia sesión correctamente mediante Microsoft Entra ID y MFA Y el correo de la identidad autenticada coincide con el correo corporativo registrado en EOP Entonces deberá establecerse la vinculación entre ambas identidades Y el usuario deberá acceder a la aplicación con los permisos correspondientes. |
| Escenario: Usuario no administrador | Dado un usuario sin permisos de administración Cuando intenta crear un usuario Entonces el sistema deberá impedir la operación. |
| Escenario: Registro público | Dada una persona que no posee acceso administrativo al sistema Cuando intenta registrarse por cuenta propia sin intervención de un Administrador Entonces no existe una opción de registro público y la aplicación impide el alta de usuario. |
| Prioridad: | Alta. |
| Dependencias: | US-042; Microsoft Entra ID. |


## US-044 — Gestionar roles y permisos

Figura 48. Mockup US-044 — Gestionar roles y permisos

Tabla 54. Historia de usuario US-044 — Gestionar roles y permisos


| COMO: | Como Administrador |
|---|---|
| QUIERO: | Quiero asignar a cada usuario uno de los tres roles fijos y asegurar que el sistema aplique sus permisos. |
| PARA: | Para controlar de forma consistente las acciones autorizadas según el rol y, cuando corresponda, la responsabilidad o participación del usuario. |
| DESCRIPCIÓN: | Todo usuario habilitado tendrá exactamente uno de los roles fijos Administrador, Supervisor u Operador. Solamente el Administrador podrá asignar o modificar el rol de un usuario; no se podrán crear roles nuevos ni configurar permisos individuales. Las acciones permitidas se determinarán mediante la matriz fija de RF-73 y las restricciones por responsabilidad o participación aplicables al presupuesto, proyecto o tarea. El Administrador tendrá las autorizaciones previstas para administrar la aplicación; ningún permiso evitará el cumplimiento de reglas de negocio, validaciones o condiciones de los datos. La autorización se comprobará también del lado del servidor y no dependerá únicamente de ocultar botones u opciones. Se impedirá cambiar el rol del último Administrador activo si eso dejara al sistema sin Administradores activos. |
| Permisos aplicables: | Solo el Administrador asigna o modifica roles. Los permisos son fijos por rol; no se admiten concesiones, denegaciones ni excepciones individuales. |
| Reglas de negocio: | Todo usuario habilitado deberá tener exactamente un rol fijo: Administrador, Supervisor u Operador. Solo el Administrador podrá asignar o modificar el rol de un usuario. No se podrán crear, editar o eliminar roles desde la aplicación. No se podrán configurar permisos individuales, excepciones, concesiones ni denegaciones personalizadas. Las acciones autorizadas se determinarán por el rol fijo y las condiciones aplicables de responsabilidad o participación, conforme a RF-73. Ser Responsable o participante no ampliará por sí solo los permisos previstos para el rol. El acceso a las funciones de administración de usuarios quedará reservado al Administrador. El sistema no permitirá dejar a la aplicación sin al menos un Administrador activo. Los controles de autorización deberán aplicarse también del lado del servidor, incluidos los intentos de acceso directo. La autorización no exceptuará reglas de negocio, restricciones de integridad, validaciones ni condiciones funcionales. Las acciones excluidas del alcance (por ejemplo, reasignación o edición de facturas y administración de catálogos fijos) no formarán parte de los permisos disponibles. |
| Requisitos relacionados: | RF: RF-72, RF-73. RNF: RNF-4, RNF-10. |
| Criterios de aceptación |  |
| Escenario: asignar un rol fijo | Dado un Administrador y un usuario registrado Cuando asigna o modifica su rol Entonces solo puede elegir Administrador, Supervisor u Operador. |
| Escenario: aplicar permisos del rol | Dado un usuario autenticado con un rol fijo Cuando solicita una operación Entonces el sistema deberá evaluar los permisos correspondientes a su rol Y, cuando corresponda, las restricciones de responsabilidad o participación. |
| Escenario: impedir permisos individuales | Dado un Administrador que gestiona un usuario Cuando intenta conceder o denegar un permiso individual o crear una excepción Entonces el sistema no deberá ofrecer ni admitir esa operación. |
| Escenario: impedir creación de roles | Dado un Administrador Cuando intenta crear o configurar un cuarto rol Entonces el sistema deberá impedir la operación Y conservar los tres roles fijos. |
| Escenario: participación insuficiente | Dado un Operador que participa en un proyecto Y una acción reservada a Supervisor o Administrador por RF-73 Cuando intenta ejecutarla Entonces el servidor deberá denegarla, aunque sea participante. |
| Escenario: acceso no autorizado | Dado un usuario sin autorización para una acción Cuando intenta ejecutarla por interfaz o mediante una solicitud directa Entonces el servidor deberá denegarla sin modificar datos. |
| Escenario: restricciones de negocio | Dado un usuario con autorización de su rol para gestionar tareas Cuando intenta asignar una tarea a alguien que no participa en el proyecto Entonces el sistema deberá impedir el cambio por incumplir las reglas de negocio. |
| Escenario: cambio de rol | Dado un Administrador y un usuario con rol Operador Cuando cambia válidamente su rol a Supervisor Entonces las autorizaciones posteriores del usuario deberán responder al rol Supervisor y sus condiciones aplicables Y no a excepciones individuales. |
| Escenario: proteger último Administrador | Dado que existe un único Administrador activo Cuando se intenta cambiar su rol a Supervisor u Operador Entonces el sistema deberá rechazar el cambio. |
| Escenario: registro de factura por rol | Dado un Operador sin autorización de registro de facturas en la matriz fija Cuando intenta registrar una factura de compra o venta Entonces el servidor deberá denegar la operación. |
| Escenario: distinguir factura y documento | Dado un Operador autorizado a subir documentación general por participar en un proyecto Cuando intenta editar o reasignar una factura existente Entonces el sistema deberá denegar esa operación Y no deberá confundir los permisos de documentos con los de facturas. |
| Escenario: consultar economía sin editar costos | Dado un Operador con acceso de consulta a un proyecto Cuando consulta los indicadores económicos e intenta editar un costo adicional Entonces podrá consultar los indicadores Y la edición del costo deberá denegarse por falta de permiso del rol. |
| Escenario: catálogos fijos | Dado un usuario autenticado de cualquier rol Cuando intenta crear o eliminar un tipo de proyecto, tipo de documento o unidad de medida Entonces el sistema no deberá ofrecer ni permitir la administración de esos catálogos. |
| Escenario: registro desde módulos permitidos | Dado un Supervisor autorizado a registrar proveedores Cuando utiliza el módulo Proveedores para registrar un proveedor válido Entonces podrá guardarlo Y el sistema no ofrecerá alta de proveedores desde el formulario de facturas o presupuestos. |
| Prioridad: | Alta. |
| Dependencias: | US-043; RF-72 y matriz fija de autorizaciones definida en RF-73. Se mantiene la relación con RNF-4 y RNF-10. |


## US-045 — Consultar listado de usuarios

Figura 49. Mockup US-045 — Consultar, buscar y filtrar usuarios

Tabla 56. Historia de usuario US-045 — Consultar listado de usuarios


| COMO: | Como Administrador. |
|---|---|
| QUIERO: | Quiero consultar el listado administrativo de usuarios registrados. |
| PARA: | Para identificar los usuarios y acceder a las acciones administrativas que correspondan. |
| DESCRIPCIÓN: | La administración dispondrá de un listado sencillo de usuarios con Nombre completo, Correo corporativo, Rol y Estado Activo/Inactivo. El listado no incluirá buscador ni filtros. Desde un usuario seleccionado, el Administrador podrá consultar y modificar su rol mediante US-044, editar sus datos mediante US-047 y modificar su estado mediante US-046. |
| Permisos aplicables: | Solo el Administrador podrá acceder al listado de usuarios. No habrá delegación de esta facultad. |
| Reglas de negocio: | Se mostrarán Nombre completo, Correo corporativo, Rol y Estado Activo/Inactivo. El listado administrativo no tendrá buscador ni filtros. Consultar usuarios no modificará información. Las acciones administrativas estarán disponibles únicamente al Administrador y deberán respetar las US-044, US-046 y US-047. |
| Requisitos relacionados: | RF: RF-74. RNF: RNF-4, RNF-14. |
| Criterios de aceptación |  |
| Escenario: Consultar usuarios | Dado un Administrador Cuando accede a la gestión de usuarios Entonces deberá visualizar los usuarios registrados con su información principal. |
| Escenario: listado sin búsqueda ni filtros | Dado un Administrador que consulta el listado de usuarios Cuando revisa la pantalla de usuarios Entonces la aplicación deberá mostrar la información principal de los usuarios registrados Y no deberá ofrecer buscador ni controles de filtrado. |
| Escenario: acceder a acciones administrativas | Dado un Administrador que localizó un usuario en el listado Cuando selecciona su acción administrativa Entonces puede consultar y modificar el rol mediante US-044 Y acceder a modificar datos por US-047 o estado por US-046 según corresponda. |
| Prioridad: | Alta. |
| Dependencias: | US-043, US-044, US-046 y US-047. |


## US-046 — Activar o inactivar un usuario

Figura 50. Mockup US-046 — Activar o inactivar un usuario

Tabla 55. Historia de usuario US-046 — Activar o inactivar un usuario


| COMO: | Como Administrador |
|---|---|
| QUIERO: | Quiero modificar el estado Activo/Inactivo de un usuario |
| PARA: | Para controlar quién puede utilizar la aplicación sin eliminar información histórica. |
| DESCRIPCIÓN: | Los usuarios no deberán eliminarse cuando dejan de utilizar el sistema. Un Administrador podrá inactivarlos y posteriormente reactivarlos. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Inactivar no elimina al usuario. Se conservarán sus relaciones históricas. Un usuario Inactivo no podrá acceder. La inactivación requerirá confirmación. Un usuario podrá reactivarse. Un responsable de proyectos no podrá inactivarse hasta reasignar dichos proyectos. Sus tareas no serán eliminadas automáticamente. El último Administrador activo no podrá inactivarse. |
| Requisitos relacionados: | RF: RF-75. RNF: RNF-1, RNF-4, RNF-10. |
| Criterios de aceptación |  |
| Escenario: Inactivar usuario | Dado un usuario que puede ser inactivado Cuando el Administrador selecciona Inactivar Entonces deberá solicitarse confirmación Y, al confirmarla, el usuario pasará a Inactivo sin eliminar su información. |
| Escenario: Responsable de proyecto | Dado un usuario asignado como responsable de un proyecto Cuando se intenta inactivarlo Entonces la operación deberá impedirse hasta que sus proyectos sean reasignados. |
| Escenario: Último Administrador | Dado el último Administrador activo Cuando se intenta inactivarlo Entonces el sistema deberá impedir la operación. |
| Escenario: Reactivar | Dado un usuario Inactivo Cuando el Administrador lo reactiva Entonces deberá volver al estado Activo. |
| Prioridad: | Alta. |
| Dependencias: | US-043, US-045; gestión de proyectos. |


## US-047 — Modificar datos de un usuario

Figura 51. Mockup US-047 — Modificar datos de un usuario

Tabla 56. Historia de usuario US-047 — Modificar datos de un usuario


| COMO: | Como Administrador |
|---|---|
| QUIERO: | Quiero modificar los datos registrados de un usuario |
| PARA: | Para mantener actualizada su información sin perder sus relaciones existentes. |
| DESCRIPCIÓN: | Desde la administración de usuarios, el Administrador podrá modificar la información del usuario seleccionado. |
| Permisos aplicables: | Solo un Administrador podrá modificar Usuarios; esta administración no podrá delegarse a otros roles. |
| Reglas de negocio: | Se modificará el mismo registro, sin crear uno nuevo. Se conservarán sus relaciones con proyectos, tareas y otros registros. Los cambios de rol deberán respetar RF-72. Los cambios de estado deberán respetar RF-75. El Correo corporativo registrado en EOP deberá ser único entre Usuarios de EOP. Al modificarlo, se verificará que el nuevo valor no esté registrado para otro Usuario; si lo está, se impedirá la modificación y se informará la duplicación. Los campos modificables son: Nombre completo → editable Correo corporativo → editable por el Administrador para reflejar un cambio realizado previamente en Microsoft Entra ID Rol → administrado mediante US-044 Estado → gestionado mediante US-046 El Administrador actualizará manualmente el Correo corporativo almacenado en EOP solo después de que cambie en Microsoft Entra ID. EOP no modificará la identidad externa; se conservarán la vinculación ya establecida, el Rol, el Estado y las relaciones del mismo Usuario. Antes del primer acceso se usará el Correo corporativo vigente en EOP para verificar la coincidencia. |
| Requisitos relacionados: | RF: RF-76. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: Modificar usuario | Dado un Administrador Cuando modifica datos válidos y guarda Entonces deberá actualizarse el mismo usuario Y conservarse sus relaciones existentes. |
| Escenario: Usuario no autorizado | Dado un usuario sin permisos de administración Cuando intenta modificar otro usuario Entonces el sistema deberá impedir la operación. |
| Escenario: actualizar Correo corporativo luego de un cambio en Microsoft Entra ID | Dado un Usuario registrado en EOP Y su Correo corporativo fue modificado previamente en Microsoft Entra ID Cuando el Administrador actualiza manualmente en EOP el Correo corporativo del mismo Usuario Entonces deberá actualizarse el Correo almacenado en EOP Y no deberá crearse un nuevo Usuario Y deberán conservarse sus relaciones, Rol y Estado Y EOP no deberá modificar la identidad externa de Microsoft Entra ID Y, si el Usuario ya se encontraba vinculado con dicha identidad externa, la vinculación deberá conservarse. |
| Escenario: Impedir modificación a Correo corporativo duplicado | Dado un Usuario registrado en EOP Y el nuevo Correo corporativo ya está registrado para otro Usuario de EOP Cuando el Administrador intenta actualizarlo en el mismo Usuario Entonces el sistema deberá impedir la modificación Y deberá informar que el Correo corporativo ya se encuentra registrado. |
| Prioridad: | Alta. |
| Dependencias: | US-043, US-044, US-045, US-046; Microsoft Entra ID. |


## US-048 — Registrar facturas de venta manualmente con PDF original

Figura 52. Mockup US-048 — Registrar facturas de venta mediante PDF y extracción asistida por IA

Tabla 59. Historia de usuario US-048 — Registrar facturas de venta manualmente con PDF original


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero registrar manualmente una Factura de venta desde un Proyecto, adjuntando su PDF original. |
| PARA: | Para centralizar los comprobantes de venta y mantenerlos correctamente asociados al Proyecto y a su información económica. |
| DESCRIPCIÓN: | El usuario autorizado registra manualmente una Factura de venta desde la ficha de un Proyecto. Selecciona el Tipo fijo, carga el PDF obligatorio, completa Fecha de emisión, Serie, Número real, Importe y Moneda; el Proyecto, Cliente y RUT correspondientes se obtienen automáticamente. Antes de guardar se controlan duplicados entre activas y la cotización histórica necesaria. No se utiliza OCR ni Azure AI Document Intelligence. La única confirmación definitiva de los datos se realiza al pulsar Guardar factura. |
| Permisos aplicables: | Solo Supervisor y Administrador autorizados pueden registrar Facturas de venta; el Operador solo puede consultar las Facturas de los Proyectos a los que tenga acceso. |
| Reglas de negocio: | Solo Supervisor o Administrador podrán registrar Facturas de venta conforme a RF-73. El Proyecto de origen se obtendrá desde su ficha y no se volverá a seleccionar. El usuario seleccionará uno de los seis Tipos fijos: e-Factura (DGI 111), e-Ticket (101), Nota de crédito de e-Factura (112), Nota de crédito de e-Ticket (102), Nota de débito de e-Factura (113) y Nota de débito de e-Ticket (103). No se administrarán Tipos desde el formulario. El usuario podrá revisar o corregir el Tipo antes de guardar y adjuntar el PDF sin confirmar previamente el tipo. El PDF original será obligatorio y válido, podrá adjuntarse mediante selección o arrastrar y soltar, y se guardará privadamente. Proyecto, Cliente del Proyecto, Emisor EOP, RUT emisor de EOP y RUT receptor del Cliente serán automáticos y de solo lectura. Fecha de emisión, Serie, Número real, Importe y Moneda se ingresarán manualmente. El sistema no efectuará extracción, OCR ni precarga asistida. El Importe será positivo y representará el total del comprobante, incluidos impuestos y redondeos. La Moneda será UYU o USD. Una Nota de crédito conservará importe positivo, aplicando signo negativo solo al indicador. El control de duplicados comparará solo Facturas de venta activas de igual RUT emisor normalizado de EOP, Tipo, Serie y Número real. Una eliminada podrá registrarse nuevamente como nueva Factura. El RUT receptor normalizado del Cliente en el momento de la carga se conservará como dato histórico propio de la Factura, sin actualización retroactiva. Cuando la operación requiera conversión histórica se obtendrá la cotización válida de datosUruguay para la Fecha de emisión o última anterior publicada. Si no existe y no se obtiene, se rechazará Guardar factura sin dejar conversión pendiente. El registro válido almacenará los datos estructurados y PDF privado en Blob Storage, vinculados exclusivamente al Proyecto de origen; el PDF no será documento genérico del Proyecto. Se asignará un identificador FV-#### único, separado del Número real y de FC-####, con secuencia global FV-0001 hasta FV-9999 y luego FV-10000, sin reutilización de códigos. La Factura guardada no podrá editarse ni reasignarse; solo se admitirá eliminación lógica. Una venta activa contribuye exclusivamente a Ingresos facturados con signo según Tipo de comprobante, sin afectar costos. Una cancelación, validación fallida o falta de cotización histórica requerida no generará una Factura definitiva incompleta. |
| Requisitos relacionados: | RF: RF-77, RF-73, RF-78, RF-83, RF-103. RNF: RNF-4, RNF-5, RNF-6, RNF-10, RNF-11, RNF-12, RNF-13, RNF-18. |
| Criterios de aceptación |  |
| Escenario: Supervisor o Administrador inicia registro | Dado un Proyecto existente y un Supervisor o Administrador autorizado Cuando selecciona Registrar factura de venta Entonces se abre un formulario manual vinculado a ese Proyecto. |
| Escenario: Operador no autorizado | Dado un usuario Operador Cuando intenta registrar una Factura de venta Entonces el servidor rechaza la operación sin crear registros. |
| Escenario: tipo fijo obligatorio sin confirmación intermedia | Dado un usuario autorizado iniciando una Factura de venta Cuando intenta guardar sin seleccionar uno de los seis Tipos fijos Entonces no puede completar el registro. |
| Escenario: PDF obligatorio | Dado un formulario con datos válidos pero sin PDF original válido Cuando se intenta guardar Entonces se rechaza el registro y se solicita el archivo. |
| Escenario: carga completamente manual | Dado un tipo seleccionado y un PDF adjunto Cuando el usuario completa Fecha, Serie, Número real, Importe y Moneda Entonces los datos son ingresados manualmente, sin extracción ni precarga por IA. |
| Escenario: datos de identidad automáticos | Dado un Proyecto asociado a un Cliente válido Cuando se abre el registro de venta Entonces Proyecto, Cliente y RUT normalizados de EOP y del Cliente aparecen como datos de solo lectura. |
| Escenario: importe no positivo | Dado Importe cero o negativo Cuando el usuario intenta guardar Entonces el sistema impide registrar la Factura. |
| Escenario: moneda inválida | Dada una Moneda diferente de UYU o USD Cuando el usuario intenta guardar Entonces el sistema rechaza el valor. |
| Escenario: duplicado activo | Dada una Factura de venta activa con igual RUT emisor normalizado, Tipo, Serie y Número real Cuando se intenta guardar otro comprobante con esa misma clave Entonces se rechaza el duplicado aunque sus separadores sean diferentes. |
| Escenario: reingresar comprobante eliminado | Dada una Factura de venta eliminada lógicamente con esa misma clave fiscal y ninguna activa equivalente Cuando el usuario registra manualmente el comprobante con su PDF y datos válidos Entonces puede guardarse como nueva Factura con un identificador FV diferente, sin restaurar el registro anterior. |
| Escenario: cotización histórica obligatoria | Dada una Factura que necesita conversión histórica y carece de cotización válida Cuando datosUruguay no proporciona una cotización utilizable Entonces se rechaza Guardar factura y se informa el motivo, sin conversión pendiente. |
| Escenario: guardar con PDF e identificador | Dados Tipo seleccionado, PDF original y datos válidos, sin duplicado y con cotización histórica requerida disponible Cuando se confirma Guardar factura Entonces se asocia exclusivamente al Proyecto, se guarda PDF privado, RUT receptor histórico y código FV-#### único. |
| Escenario: numeración después de 9999 | Dado que el último código de venta asignado fue FV-9999 Cuando se registra otra Factura válida Entonces recibe FV-10000 y no modifica la secuencia FC. |
| Escenario: nota de crédito y resumen económico | Dada una Nota de crédito de venta con Importe original positivo Cuando se guarda válidamente Entonces su contribución resta una vez de Ingresos facturados sin afectar los indicadores de costos. |
| Escenario: factura inmutable | Dada una Factura de venta guardada Cuando se consulta su detalle Entonces no es posible editar los datos, PDF, Cliente ni Proyecto; solo solicitar su eliminación lógica con autorización. |
| Escenario: cancelación sin Factura incompleta | Dada una carga iniciada pero no completada válidamente Cuando se cancela o una validación falla Entonces no se registra ninguna Factura definitiva incompleta. |
| Escenario: conservación del RUT receptor histórico | Dada una Factura de venta registrada correctamente para un Proyecto y un Cliente Cuando se modifica posteriormente el RUT del Cliente en la sección Clientes Entonces la Factura conserva el RUT receptor normalizado registrado al momento de su alta, sin modificar el comprobante histórico. |
| Escenario: nota de débito suma una sola vez en ingresos | Dada una Nota de débito de venta registrada con Importe positivo Cuando se calcula Ingresos facturados del Proyecto Entonces su Importe contribuye positivamente una sola vez, sin alterar Costo según facturas de compra. |
| Escenario: rechazar alta sin cotización sin registros parciales | Dada una Factura de venta nueva que requiere conversión histórica Cuando no existe una cotización válida almacenada ni el servicio proporciona una aplicable Entonces el guardado se rechaza, se informa el motivo y no se crea Factura definitiva ni conversión pendiente. |
| Prioridad: | Alta. |
| Dependencias: | Proyecto existente, Cliente asociado, permisos fijos RF-73, cotización histórica cuando sea necesaria y PDF original privado. |


## US-049 — Eliminar lógicamente una factura de compra o venta

Figura 53. Mockup US-049 — Modificar o eliminar una factura de compra o venta

Tabla 60. Historia de usuario US-049 — Eliminar lógicamente una factura de compra o venta


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero eliminar lógicamente una Factura de compra o venta registrada. |
| PARA: | Para mantener información administrativa correcta, sin duplicar comprobantes ni conservar Facturas registradas por error. |
| DESCRIPCIÓN: | El usuario autorizado identifica una Factura existente y solicita su eliminación. Tras confirmar, el sistema la marca como eliminada, conserva su registro, código y PDF original privado, y la excluye de las consultas activas y los indicadores económicos. No se permitirá modificar ni reasignar una Factura guardada. |
| Permisos aplicables: | Solo Supervisor y Administrador autorizados pueden eliminar lógicamente Facturas. No hay edición ni reasignación; el Operador únicamente consulta conforme a su rol. |
| Reglas de negocio: | Solo Supervisor o Administrador podrán eliminar Facturas conforme a RF-73, con autorización del lado del servidor. Operador no podrá hacerlo mediante permisos individuales. Una Factura registrada es inmutable: no se editarán Tipo, Fecha, Serie, Número real, Importe, Moneda, RUT históricos, Proyecto, Cliente, Proveedor ni PDF original; tampoco podrá reasignarse. La operación Eliminar identificará la Factura y requerirá confirmación explícita; cancelar no alterará datos ni archivo. La eliminación lógica conservará registro, RUT histórico, identificador, relaciones y PDF original privado, sin eliminar el Proyecto, Cliente ni Proveedor. La Factura eliminada dejará de mostrarse en listados y contadores activos y no contribuirá a los indicadores económicos del Proyecto. Un comprobante eliminado lógicamente no bloqueará un registro nuevo de la misma categoría y clave fiscal; la nueva carga requiere PDF y genera otro código interno. No habrá restauración automática ni reutilización del identificador eliminado. Eliminar una compra recalculará Costo según facturas de compra y Costo real total; eliminar una venta recalculará Ingresos facturados conforme a RF-103. El sistema no informará una eliminación finalizada si el comprobante continúa figurando o computándose como activo. |
| Requisitos relacionados: | RF: RF-78, RF-77, RF-104, RF-73, RF-103. RNF: RNF-1, RNF-4, RNF-5, RNF-6, RNF-8, RNF-10. |
| Criterios de aceptación |  |
| Escenario: eliminación requiere confirmación | Dada una Factura activa y un Supervisor o Administrador autorizado Cuando solicita Eliminar Entonces se muestra una confirmación que identifica el comprobante. |
| Escenario: cancelar eliminación | Dada una confirmación pendiente Cuando el usuario cancela Entonces se conservan el registro, asociación y PDF original sin cambios. |
| Escenario: eliminar compra | Dada una Factura de compra activa y eliminación confirmada Cuando finaliza correctamente la operación Entonces se conserva registro y PDF, la Factura se marca eliminada, desaparece de consultas activas y se recalculan los costos. |
| Escenario: eliminar venta | Dada una Factura de venta activa y eliminación confirmada Cuando finaliza correctamente la operación Entonces se conserva registro y PDF, la Factura se marca eliminada, desaparece de consultas activas y se recalculan los ingresos. |
| Escenario: Operador no elimina | Dado un usuario Operador Cuando intenta eliminar una Factura Entonces se rechaza la operación sin modificar el comprobante. |
| Escenario: edición prohibida | Dada una Factura guardada Cuando cualquier usuario intenta modificar sus datos, Proveedor, Cliente o PDF Entonces el sistema no permite efectuar la edición. |
| Escenario: reasignación prohibida | Dada una Factura guardada asociada al Proyecto A Cuando se intenta asociarla después al Proyecto B Entonces el sistema rechaza la reasignación y conserva el Proyecto A. |
| Escenario: reingreso tras baja lógica | Dada una Factura eliminada lógicamente y ninguna activa con su misma clave fiscal Cuando se vuelve a registrar el comprobante con nuevo PDF y datos válidos Entonces se crea un nuevo registro y código interno, sin restaurar ni reutilizar el anterior. |
| Escenario: conservar identificador y PDF | Dada una Factura eliminada lógicamente Cuando se comprueba su almacenamiento histórico Entonces permanecen su identificador original, datos históricos y PDF privado, sin contarse como Factura activa. |
| Escenario: baja lógica excluida de consultas y contadores | Dada una Factura activa asociada a un Proyecto Cuando un Supervisor o Administrador confirma su eliminación lógica Entonces deja de figurar en las consultas y contadores activos del Proyecto, pero se conserva su registro y PDF privado para trazabilidad histórica. |
| Prioridad: | Alta. |
| Dependencias: | US-048 o US-068 según categoría; RF-78, RF-103; Facturas activas, confirmación y roles fijos. |


## US-050 — Consultar y filtrar facturas de venta

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 54. Mockup US-050 — Consultar y filtrar facturas de venta

Tabla 57. Historia de usuario US-050 — Consultar y filtrar facturas de venta


| COMO: | Como Operador, Supervisor o Administrador con acceso efectivo de consulta conforme a RF-73. |
|---|---|
| QUIERO: | Quiero consultar, buscar y filtrar Facturas de venta registradas y acceder a sus PDF originales. |
| PARA: | Para localizar comprobantes de venta y conocer sus datos, Cliente y Proyecto asociado sin modificar la información. |
| DESCRIPCIÓN: | La sección general Facturas tendrá apartados independientes para venta y compra. El listado de Facturas de venta mostrará únicamente registros activos creados conforme a US-048, cada uno con FV-####, Tipo de comprobante, Serie, Número real, Fecha de emisión, Cliente, Importe registrado, Moneda y Proyecto asociado. Todas las Facturas activas tendrán un Proyecto y un PDF original accesible para visualización o descarga a usuarios autorizados conforme a RF-83. |
| Permisos aplicables: | Por permisos efectivos conforme a RF-73, Operador, Supervisor y Administrador podrán consultar Facturas de venta activas registradas mediante US-048. |
| Reglas de negocio: | El apartado exclusivo de venta mostrará FV-####, Tipo de comprobante, Serie, Número real, Fecha de emisión, Cliente, Importe registrado, Moneda y Proyecto asociado; toda Factura activa tendrá un Proyecto. No se mezclarán compras. Se podrá buscar por identificador interno FV-#### o Número real de comprobante y filtrar exclusivamente por Fecha, Cliente y Proyecto asociado. Búsqueda y filtros podrán combinarse entre sí; los criterios de compra serán independientes. El usuario autorizado podrá abrir la información de una Factura de venta y visualizar o descargar su PDF original conforme a RF-83. Consultar, buscar, filtrar, visualizar o descargar el PDF no alterará la Factura, el documento ni su asociación. |
| Requisitos relacionados: | RF: RF-79, RF-80, RF-83. RNF: RNF 5, RNF-4, RNF-14. |
| Criterios de aceptación |  |
| Escenario: Listado de ventas | Dado un usuario autorizado y Facturas de venta activas registradas Cuando abre el apartado Facturas de venta Entonces ve FV-####, Tipo, Serie, Número real, Fecha, Cliente, Importe registrado, Moneda y Proyecto asociado por Factura. |
| Escenario: Listado vacío | Dado un usuario autorizado y ninguna Factura de venta activa Cuando abre el apartado Facturas de venta Entonces se muestra un listado vacío sin presentar Facturas de compra. |
| Escenario: Buscar identificador interno | Dadas Facturas de venta activas con identificadores diferentes Cuando busca por un FV-#### concreto Entonces se muestran únicamente las Facturas coincidentes. |
| Escenario: Buscar número real | Dadas Facturas de venta con números reales distintos Cuando busca un Número real concreto Entonces se muestran las Facturas de venta coincidentes sin confundirlo con FV-####. |
| Escenario: Filtrar Fecha | Dadas Facturas de venta emitidas en fechas diferentes Cuando aplica el filtro por Fecha Entonces se muestran únicamente las que cumplen la fecha seleccionada. |
| Escenario: Filtrar Cliente | Dadas Facturas de venta de Clientes diferentes Cuando filtra por Cliente Entonces se muestran únicamente las del Cliente seleccionado. |
| Escenario: Filtrar Proyecto | Dadas Facturas de venta de Proyectos diferentes Cuando filtra por Proyecto asociado Entonces se muestran únicamente las actualmente asociadas al Proyecto indicado. |
| Escenario: Combinar criterios | Dadas Facturas de venta con distintas Fechas, Clientes, Proyectos, códigos y números Cuando combina búsqueda por FV-#### o Número real con los filtros vigentes Entonces se muestran solo las Facturas que cumplen simultáneamente los criterios elegidos. |
| Escenario: Separación de categorías | Dadas Facturas de compra y venta activas Cuando abre el apartado Facturas de venta Entonces no aparecen comprobantes de compra ni se comparten sus criterios de búsqueda. |
| Escenario: PDF original autorizado | Dada una Factura de venta registrada con su PDF y un usuario autorizado Cuando abre la Factura y selecciona Ver o Descargar PDF Entonces visualiza o descarga el PDF original correspondiente a esa Factura conforme a RF-83. |
| Escenario: Consulta sin modificación | Dada una Factura de venta con datos, PDF y Proyecto asociado Cuando el usuario la consulta, filtra o descarga su PDF Entonces se mantienen sin cambios datos, PDF y asociación. |
| Escenario: Solo Facturas activas con Proyecto | Dado un listado de Facturas de venta activas Cuando el usuario lo consulta Entonces cada registro presentado tiene un Proyecto asociado y no se ofrece Estado de asociación. |
| Prioridad: | Alta. |
| Dependencias: | US-048; registro de Facturas de venta; gestión de permisos. |


## US-051 — Reasignar una factura de venta a otro proyecto

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 55. Mockup US-051 — Reasignar una factura de venta a otro proyecto

Tabla 58. Historia de usuario US-051 — Reasignar una factura de venta a otro proyecto


| COMO: | Como Supervisor o Administrador, o como Operador con el permiso individual específico de reasignación de Facturas de venta conforme a RF-73. |
|---|---|
| QUIERO: | Quiero cambiar el Proyecto al que pertenece una Factura de venta ya registrada. |
| PARA: | Para corregir la asociación de un comprobante sin volver a cargar su PDF ni registrar otra Factura. |
| DESCRIPCIÓN: | La Factura de venta ya está asociada a un Proyecto desde su registro mediante US-048. El usuario autorizado podrá buscar por Nombre un Proyecto de destino del mismo Cliente y confirmar su reasignación. La operación conservará datos e identificador FV-#### y PDF original, trasladará la única asociación y actualizará listados, contadores e Ingresos facturados en origen y destino conforme a RF-81 y RF-103. |
| Permisos aplicables: | Reasignar una Factura de venta ya asociada requerirá permiso efectivo para esta acción: Supervisor y Administrador por permisos base u Operador con permiso individual específico conforme a RF-73. |
| Reglas de negocio: | El usuario podrá buscar por Nombre y seleccionar exclusivamente un Proyecto registrado del mismo Cliente de la Factura; no podrá elegir un Proyecto de otro Cliente. El Estado Pendiente de inicio, En ejecución, En pausa, Finalizado o Cancelado del Proyecto de destino no será por sí solo una restricción para seleccionarlo. Se solicitará confirmación explícita. Al completarse, la Factura dejará el Proyecto de origen y pertenecerá exclusivamente al de destino; no se ofrecerá Desvincular ni se admitirá dejarla sin Proyecto. La operación conservará FV-####, el mismo Cliente, RUT receptor histórico, Tipo de comprobante, Serie, Número real, Fecha de emisión, Importe registrado, Moneda y PDF original; no actualizará ese RUT con el RUT actual del Cliente, no creará otra Factura ni exigirá volver a cargar el PDF. La Factura desaparecerá del listado de ventas del Proyecto de origen y aparecerá en el destino; disminuirá el contador del origen y aumentará el del destino. Se recalcularán los Ingresos facturados de ambos Proyectos según RF-103 sin contar la Factura dos veces. Si se cancela o falla la operación, la Factura conservará íntegramente su Proyecto original, datos y PDF, sin cambios parciales. |
| Requisitos relacionados: | RF: RF-81, RF-77, RF-73, RF-103. RNF: RNF 1, RNF-4, RNF-10. |
| Criterios de aceptación |  |
| Escenario: Buscar por Nombre del mismo Cliente | Dada una Factura de venta del Cliente C asociada al Proyecto A Cuando el usuario autorizado busca por Nombre un Proyecto de destino Entonces se presentan como seleccionables los Proyectos registrados del Cliente C. |
| Escenario: Seleccionar mismo Cliente | Dado un Proyecto B del mismo Cliente de la Factura Cuando el usuario lo selecciona como destino Entonces se permite continuar a la confirmación de la reasignación. |
| Escenario: Rechazar otro Cliente | Dado un Proyecto de un Cliente diferente al de la Factura de venta Cuando el usuario intenta seleccionarlo como destino Entonces el sistema impide la selección o la reasignación. |
| Escenario: Proyecto Finalizado | Dado un Proyecto Finalizado del mismo Cliente Cuando se busca y selecciona como destino Entonces su Estado no impide confirmar la reasignación. |
| Escenario: Proyecto Cancelado | Dado un Proyecto Cancelado del mismo Cliente Cuando se busca y selecciona como destino Entonces su Estado no impide confirmar la reasignación. |
| Escenario: Confirmación expresa | Dada una Factura de venta y un Proyecto de destino válido Cuando el usuario selecciona Reasignar Entonces se solicita confirmación explícita antes de cambiar su Proyecto. |
| Escenario: Cancelación | Dada una reasignación iniciada de Proyecto A a Proyecto B Cuando el usuario cancela la confirmación Entonces la Factura y su PDF permanecen sin cambios y asociados a A. |
| Escenario: Reasignación de A a B | Dada una Factura de venta actualmente asociada a A y un destino B del mismo Cliente Cuando un usuario autorizado confirma la reasignación y esta finaliza correctamente Entonces la Factura deja A y queda exclusivamente asociada a B, sin nuevo registro. |
| Escenario: Conservar identidad y documento | Dada una Factura de venta con FV-####, Cliente, RUT receptor histórico almacenado, datos y PDF original, cuyo Cliente puede tener un RUT actual diferente Cuando un usuario autorizado completa la reasignación a otro Proyecto del mismo Cliente Entonces conserva FV-####, Cliente, Tipo, Serie, Número, Fecha, Importe, Moneda, el RUT receptor histórico sin sustituirlo por el RUT actual del Cliente y el mismo PDF sin volver a subirlo; queda asociada exclusivamente al nuevo Proyecto. |
| Escenario: Listados y contadores | Dada una Factura de venta asociada a A que se reasigna a B Cuando se completa la operación Entonces desaparece del listado de A y aparece en B, disminuyendo el contador de A y aumentando el de B. |
| Escenario: Recalcular ingresos en ambos | Dados dos Proyectos del mismo Cliente con Ingresos facturados calculados Cuando una Factura de venta se reasigna de A a B Entonces se recalculan Ingresos facturados de ambos sin contarla simultáneamente en los dos. |
| Escenario: Nunca sin Proyecto | Dada una Factura de venta activa asociada a un Proyecto Cuando se intenta dejarla sin destino o ejecutar una desvinculación independiente Entonces no se permite la acción y permanece asociada a un único Proyecto. |
| Escenario: Operador sin permiso | Dado un Operador sin permiso individual para reasignar Facturas de venta Cuando intenta reasignar una Factura Entonces la acción se deniega sin alterar la asociación. |
| Escenario: Operador con permiso | Dado un Operador con autorización efectiva para reasignar Facturas de venta Cuando confirma un destino válido del mismo Cliente Entonces se permite la reasignación con las mismas restricciones de Cliente y conservación. |
| Prioridad: | Alta. |
| Dependencias: | US-048; US-050; Proyectos registrados; gestión de Clientes; permisos efectivos. |


## US-052 — Consultar, buscar y filtrar clientes

Figura 56. Mockup US-052 — Consultar, buscar y filtrar clientes

Tabla 59. Historia de usuario US-052 — Consultar, buscar y filtrar clientes


| COMO: | Como Operador, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero consultar, buscar y filtrar los clientes registrados |
| PARA: | Para localizar rápidamente al cliente con el que necesito trabajar. |
| DESCRIPCIÓN: | Por permisos base, Operador, Supervisor y Administrador podrán consultar, buscar y filtrar Clientes, según los permisos fijos de RF-73, sin excepciones individuales. La sección Clientes deberá mostrar los clientes registrados. La información consultable de cada Cliente incluirá al menos nombre, RUT, Correo de contacto y Correo de facturación cuando estén informados, número de WhatsApp y estado Activo/Inactivo; no se inventará un valor para un Correo ausente. Se podrá buscar por nombre o RUT y filtrar por estado. |
| Permisos aplicables: | Operador, Supervisor y Administrador pueden consultar y filtrar Clientes con los permisos fijos de RF-73. |
| Reglas de negocio: | Los clientes permanecerán registrados para conservar su información histórica. Se podrá consultar nombre, RUT, Correo de contacto, Correo de facturación, WhatsApp y estado; cuando falte uno de los Correos, no se inventará su valor. Se podrá buscar por nombre y RUT. Se podrá filtrar por estado Activo/Inactivo. Los criterios podrán utilizarse conjuntamente. Buscar o filtrar no modificará información. |
| Requisitos relacionados: | RF: RF-84, RF-85. RNF: RNF-4, RNF-14. |
| Criterios de aceptación |  |
| Escenario: Consultar clientes | Dado que un Operador, Supervisor o Administrador consulta Clientes registrados por sus permisos base Cuando el usuario accede a Clientes Entonces deberá visualizar su información principal. |
| Escenario: Buscar cliente | Dado que existen clientes registrados Cuando el usuario busca por nombre o RUT Entonces deberán mostrarse los clientes correspondientes. |
| Escenario: Filtrar por estado | Dado que existen clientes Activos e Inactivos Cuando el usuario selecciona un estado Entonces deberán mostrarse únicamente los clientes correspondientes. |
| Prioridad: | Alta. |
| Dependencias: | Gestión de clientes. |


## US-053 — Registrar un cliente

Figura 57. Mockup US-053 — Registrar un cliente

Tabla 60. Historia de usuario US-053 — Registrar un cliente


| COMO: | Como Operador, Supervisor o Administrador |
|---|---|
| QUIERO: | Quiero registrar un nuevo cliente |
| PARA: | Para utilizarlo en presupuestos, proyectos y demás funcionalidades relacionadas. |
| DESCRIPCIÓN: | Por permisos base, Operador, Supervisor y Administrador podrán registrar un Cliente desde la sección Clientes, según los permisos fijos de RF-73, sin excepciones individuales. El mismo proceso podrá iniciarse durante la creación de un presupuesto o proyecto cuando el cliente necesario no exista. Además de Nombre, RUT y WhatsApp obligatorios, el Cliente dispondrá de Correo de contacto y Correo de facturación; deberá informarse al menos uno de ellos y validarse el formato de cada Correo informado. |
| Permisos aplicables: | Operador, Supervisor y Administrador pueden registrar Clientes conforme a sus permisos fijos, sin excepciones individuales. |
| Reglas de negocio: | Serán obligatorios nombre, RUT y número de WhatsApp. Se registrarán Correo de contacto y Correo de facturación como campos diferenciados; deberá informarse al menos uno, pudiendo existir solo uno o ambos. El registro se impedirá cuando ambos estén vacíos. El RUT uruguayo deberá validarse en estructura y dígito verificador, almacenarse solo con dígitos tras normalizar separadores habituales y ser único por su valor normalizado. Antes de registrar deberá verificarse que el RUT normalizado no exista, aun si se ingresó con otra representación. El nuevo cliente quedará inmediatamente disponible para presupuestos y proyectos. Todo nuevo cliente quedará inicialmente Activo. Todo Correo de contacto o Correo de facturación informado deberá tener formato válido de Correo electrónico; la ausencia de uno no es un error si el otro es válido. El número de WhatsApp se almacenará en formato internacional compatible con E.164, con + y código de país, admitiendo números internacionales válidos y normalizando separadores; su formato se validará antes de usarlo en enlaces de WhatsApp. |
| Requisitos relacionados: | RF: RF-89. RNF: RNF-10, RNF-18, RNF-4. |
| Criterios de aceptación |  |
| Escenario: registrar con ambos Correos | Dado que un Operador, Supervisor o Administrador completa correctamente Nombre, RUT y WhatsApp por sus permisos base y registra Correo de contacto y Correo de facturación válidos Y los separadores habituales del RUT y del WhatsApp se normalizan, el RUT uruguayo tiene dígito verificador válido, su valor normalizado no se encuentra registrado y el WhatsApp internacional cumple E.164 Cuando confirma Entonces deberá crearse el Cliente en estado Activo y conservarse ambos Correos. |
| Escenario: registrar solo con Correo de contacto | Dado que el usuario completó los demás datos obligatorios y un Correo de contacto válido, sin informar Correo de facturación Cuando confirma Entonces deberá permitirse crear el Cliente. |
| Escenario: registrar solo con Correo de facturación | Dado que el usuario completó los demás datos obligatorios y un Correo de facturación válido, sin informar Correo de contacto Cuando confirma Entonces deberá permitirse crear el Cliente. |
| Escenario: impedir registro sin Correos | Dado que el usuario completó los demás datos obligatorios pero dejó vacíos Correo de contacto y Correo de facturación Cuando intenta confirmar Entonces el sistema impedirá el registro e informará que debe existir al menos uno de los dos Correos. |
| Escenario: formato inválido de Correo de contacto | Dado que el usuario ingresó un Correo de contacto con formato inválido Cuando intenta confirmar Entonces se impedirá el registro y se identificará el dato inválido. |
| Escenario: formato inválido de Correo de facturación | Dado que el usuario ingresó un Correo de facturación con formato inválido Cuando intenta confirmar Entonces se impedirá el registro y se identificará el dato inválido. |
| Escenario: RUT duplicado o inválido | Dado que ya existe un Cliente con el mismo RUT normalizado aunque se ingrese con separadores distintos, o que el RUT ingresado no supera la validación de estructura uruguaya y dígito verificador Cuando se intenta registrar otro Entonces el sistema deberá impedir el alta. |
| Escenario: WhatsApp inválido | Dado que el número de WhatsApp no cumple el formato internacional compatible con E.164 con + y código de país, una vez normalizados los separadores habituales Cuando el usuario intenta registrar el cliente Entonces no deberá completarse el alta Y deberán indicarse los datos incorrectos. |
| Escenario: Alta durante presupuesto o proyecto | Dado que el cliente necesario no existe Cuando el usuario lo registra desde la creación de un presupuesto o proyecto Entonces deberá quedar disponible inmediatamente para seleccionarlo en el proceso actual. |
| Escenario: crear Cliente sin asociar Facturas automáticamente | Dado que un usuario autorizado registra un nuevo Cliente con datos válidos Cuando confirma su alta Entonces el Cliente queda disponible para Presupuestos y Proyectos Y el alta no genera registros ni asociaciones automáticas de Facturas. |
| Prioridad: | Alta. |
| Dependencias: | Gestión de clientes. |


## US-054 — Modificar información de un cliente

Figura 58. Mockup US-054 — Modificar información de un cliente

Tabla 61. Historia de usuario US-054 — Modificar información de un cliente


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero modificar la información de un cliente |
| PARA: | Para mantener actualizados o corregir sus datos registrados. |
| DESCRIPCIÓN: | Por los permisos fijos de RF-73, Supervisor y Administrador podrán modificar nombre, RUT, Correo de contacto, Correo de facturación y número de WhatsApp; Operador no podrá modificar Clientes. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Se modificará el mismo cliente. No se eliminarán relaciones con presupuestos, proyectos o facturas; modificar el RUT en la ficha del Cliente no modificará los RUT receptores históricos de Facturas de venta anteriores ni sus PDF originales, ni creará, eliminará o reasignará Facturas. Las nuevas Facturas de venta utilizarán el RUT actualizado al registrarse. Podrán modificarse nombre, RUT, Correo de contacto, Correo de facturación y WhatsApp; podrá agregarse un Correo anteriormente ausente. El RUT modificado se normalizará para conservar únicamente dígitos, validando la estructura uruguaya y su dígito verificador; su unicidad se comparará por el valor normalizado, aun si se ingresó con separadores distintos. Al guardar deberá existir al menos uno entre Correo de contacto y Correo de facturación, sin exigir ambos. Podrá vaciarse uno si el otro permanece informado y válido; todo Correo informado deberá tener formato válido. El número de WhatsApp continuará siendo obligatorio, almacenado como texto internacional compatible con E.164 con + y código de país, admitiendo números internacionales válidos y normalizando separadores antes de validar y de utilizar enlaces. |
| Requisitos relacionados: | RF: RF-86. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: Modificar cliente | Dado un Cliente existente y un Supervisor o Administrador con permiso base para modificarlo Cuando un usuario autorizado modifica datos válidos y guarda Entonces deberá actualizarse el mismo cliente Y conservarse sus relaciones existentes. |
| Escenario: Operador no puede modificar Clientes | Dado un usuario Operador Cuando intenta modificar un Cliente existente Entonces el sistema deberá impedir la modificación conforme a los permisos fijos del rol. |
| Escenario: Supervisor o Administrador modifica Cliente | Dado un Supervisor o Administrador autorizado Cuando modifica datos válidos de un Cliente existente y guarda Entonces se actualiza el mismo Cliente, conservando sus relaciones y sus datos históricos de Facturas. |
| Escenario: RUT duplicado o inválido | Dado que otro Cliente posee el mismo RUT normalizado, aunque el ingresado use separadores distintos, o que el RUT ingresado no supera la validación de estructura uruguaya y dígito verificador Cuando se intenta guardar la modificación Entonces el sistema deberá impedirla. |
| Escenario: impedir guardar sin WhatsApp | Dado un cliente existente Cuando el usuario elimina el número de WhatsApp e intenta guardar los cambios Entonces el sistema deberá impedir la modificación Y deberá indicar que el número de WhatsApp es obligatorio. |
| Escenario: formato de WhatsApp inválido | Dado un cliente existente Cuando el usuario ingresa un número de WhatsApp que, tras normalizar separadores, no cumple el formato internacional E.164 con + y código de país e intenta guardar Entonces el sistema deberá impedir la modificación Y deberá indicar qué dato no cumple con el formato requerido. |
| Escenario: Modificar RUT de un cliente con facturas asociadas | Dado un Cliente que posee Proyectos con Facturas de venta asociadas Cuando un usuario autorizado modifica su RUT por otro valor uruguayo válido, con dígito verificador correcto y único tras normalizarlo a dígitos, y guarda Entonces se actualiza el mismo registro de Cliente Y se conservan sus Proyectos y las Facturas asociadas a esos Proyectos Y no se crea, elimina ni reasigna ninguna Factura por esa modificación Y no se modifica el contenido de los PDF originales. Y los RUT receptores históricos conservados en las Facturas de venta anteriores no cambian. Y las nuevas Facturas de venta utilizarán el RUT actualizado del Cliente. |
| Escenario: conservar solamente Correo de contacto | Dado un Cliente con ambos Correos Cuando el usuario elimina el Correo de facturación, mantiene un Correo de contacto válido y guarda Entonces deberá permitirse la modificación. |
| Escenario: conservar solamente Correo de facturación | Dado un Cliente con ambos Correos Cuando el usuario elimina el Correo de contacto, mantiene un Correo de facturación válido y guarda Entonces deberá permitirse la modificación. |
| Escenario: impedir eliminar ambos Correos | Dado un Cliente registrado Cuando el usuario deja vacíos Correo de contacto y Correo de facturación e intenta guardar Entonces el sistema impedirá la modificación e informará que al menos uno de los dos Correos es obligatorio. |
| Escenario: modificar ambos Correos | Dado un Cliente registrado Cuando el usuario modifica correctamente Correo de contacto y Correo de facturación y guarda Entonces ambos nuevos valores quedarán registrados en el mismo Cliente. |
| Escenario: Correo informado con formato inválido | Dado que el usuario modifica uno de los Correos Cuando ingresa un valor con formato inválido e intenta guardar Entonces el sistema impedirá la modificación e identificará el Correo inválido. |
| Prioridad: | Alta. |
| Dependencias: | US-052, US-053. |


## US-055 — Activar o inactivar un cliente

Figura 59. Mockup US-055 — Activar o inactivar un cliente

Tabla 62. Historia de usuario US-055 — Activar o inactivar un cliente


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero modificar el estado Activo/Inactivo de un cliente |
| PARA: | Para dejar de utilizar clientes actuales sin perder su información histórica. |
| DESCRIPCIÓN: | Por los permisos fijos de RF-73, Supervisor y Administrador podrán inactivar y reactivar Clientes; Operador no podrá cambiar su estado. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | Inactivar no eliminará al cliente. Se conservarán presupuestos, proyectos, facturas y demás relaciones históricas. Un cliente Inactivo podrá volver a Activo. La inactivación requerirá confirmación. Un cliente Inactivo no estará disponible para nuevos presupuestos. Un cliente Inactivo no estará disponible para nuevos proyectos. |
| Requisitos relacionados: | RF: RF-87. RNF: RNF-1, RNF-4, RNF-10. |
| Criterios de aceptación |  |
| Escenario: Inactivar cliente | Dado un Cliente Activo y un Supervisor o Administrador con permiso base para inactivarlo Cuando un usuario autorizado selecciona Inactivar Entonces deberá mostrarse una confirmación. |
| Escenario: Operador no cambia estado de Clientes | Dado un usuario Operador Cuando intenta activar o inactivar un Cliente Entonces el sistema deberá rechazar la acción por su rol. |
| Escenario: Supervisor o Administrador cambia estado de Clientes | Dado un Supervisor o Administrador autorizado y un Cliente existente Cuando confirma válidamente su activación o inactivación Entonces el Cliente cambia de estado sin eliminar sus relaciones históricas. |
| Escenario: Confirmar inactivación | Dado que se muestra la confirmación Cuando el usuario confirma Entonces el cliente pasará a Inactivo Y se conservará toda su información histórica. |
| Escenario: Cancelar inactivación | Dado que se muestra la confirmación Cuando el usuario cancela Entonces el cliente permanecerá Activo. |
| Escenario: Utilizar cliente inactivo | Dado un cliente Inactivo Cuando se crea un nuevo presupuesto o proyecto Entonces dicho cliente no deberá encontrarse disponible para selección. |
| Escenario: Reactivar cliente | Dado un cliente Inactivo Cuando un usuario autorizado lo activa nuevamente Entonces deberá pasar a estado Activo. |
| Prioridad: | Alta. |
| Dependencias: | US-052, US-053. |


## US-056 — Vincular un producto de Fivisa para actualización automática

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 60. Mockup US-056 — Vincular un producto de Fivisa para actualización automática

Tabla 63. Historia de usuario US-056 — Vincular un producto de Fivisa para actualización automática


| COMO: | Como Supervisor o Administrador, o como Operador con el permiso individual específico correspondiente conforme a RF-73. |
|---|---|
| QUIERO: | Quiero vincular una relación material-proveedor con el producto correspondiente de Fivisa. |
| PARA: | Para utilizar su precio publicado y permitir que dicho precio pueda mantenerse actualizado automáticamente. |
| DESCRIPCIÓN: | Cuando el proveedor seleccionado para un material sea Fivisa, el Supervisor o Administrador por permisos base, o el Operador con el permiso individual específico correspondiente conforme a RF-73, podrá ingresar la URL correspondiente al producto publicado por Fivisa y solicitar su validación. El sistema consultará la fuente digital correspondiente y deberá intentar identificar el producto, recuperar su código o referencia y obtener el precio normal publicado del producto, sin utilizar descuentos, promociones, ofertas ni precios especiales. Si la validación es satisfactoria, el sistema mostrará al usuario el producto identificado y el precio recuperado para que pueda confirmar la vinculación. Al confirmar una vinculación validada correctamente, la URL del producto, su código o referencia externa, el precio normal publicado recuperado (estrictamente mayor que cero), la moneda USD o UYU seleccionada obligatoriamente por el usuario y la fecha de actualización deberán quedar asociados a la relación material-proveedor dentro del flujo que originó la vinculación. La moneda no se obtendrá ni seleccionará automáticamente desde la página de Fivisa; quien confirma la vinculación será responsable de elegirla correctamente. El nombre y la unidad del Material continuarán siendo los definidos dentro de EOP. Cuando la relación material-proveedor ya se encuentre registrada definitivamente en el catálogo, la confirmación de la vinculación deberá habilitar su participación en la actualización automática semanal definida en US-024. Cuando la vinculación se realice al agregar Fivisa como nuevo proveedor de un material ya existente durante la elaboración de un presupuesto mediante US-010B, los datos obtenidos deberán permanecer pendientes junto con dicha relación hasta que el usuario seleccione Confirmar datos y continuar. Recién entonces la relación quedará registrada definitivamente en el catálogo y habilitada para la actualización automática. |
| Permisos aplicables: | Por permisos base, Supervisor y Administrador podrán ingresar y validar la URL y confirmar la vinculación; el Operador solo con el permiso individual específico correspondiente conforme a RF-73. El usuario autorizado deberá proporcionar la URL del producto que desea vincular. |
| Reglas de negocio: | La vinculación solo podrá realizarse cuando el proveedor seleccionado sea Fivisa. El sistema deberá validar la URL y comprobar que permite identificar un producto de Fivisa. La vinculación no podrá confirmarse cuando no se pueda identificar el producto, recuperar un precio normal estrictamente mayor que cero o el usuario no haya seleccionado USD o UYU para ese precio. Una validación correcta deberá permitir obtener y conservar un código o referencia externa del producto. La URL y el código o referencia externa deberán conservarse para permitir localizar nuevamente el producto. El precio recuperado deberá ser el precio normal publicado y utilizarse como precio de la relación material-proveedor dentro del flujo en curso. Si también se muestra un precio promocional o descontado, se utilizará el normal y no se usarán descuentos, promociones, ofertas ni precios especiales. La fecha de actualización deberá registrarse automáticamente. La información obtenida desde Fivisa no deberá modificar el nombre ni la unidad de medida del Material registrado en EOP. La confirmación de una vinculación válida deberá dejar preparada la relación para actualización automática. Dicha actualización solo deberá quedar habilitada cuando la relación material-proveedor se encuentre registrada definitivamente en el catálogo. Cuando la vinculación se realice al agregar Fivisa como nuevo proveedor de un material ya existente mediante US-010B, los datos de vinculación deberán permanecer pendientes junto con dicha relación hasta que el usuario seleccione Confirmar datos y continuar. La vinculación de un producto no deberá provocar la importación automática de otros productos de Fivisa. Una vez confirmada la vinculación y habilitada la actualización automática, el precio de la relación material-proveedor no podrá ser modificado manualmente. El precio deberá mantenerse a partir del precio normal publicado recuperado y validado desde Fivisa, tanto al vincular como en las actualizaciones posteriores. |
| Requisitos relacionados: | RF-92, RF-90. |
| Criterios de aceptación |  |
| Escenario: mostrar vinculación Fivisa | Dado que el usuario seleccionó Fivisa como proveedor Cuando continúa con la asociación del proveedor al material Entonces deberá disponer de la opción para ingresar la URL del producto de Fivisa y validarla. |
| Escenario: validar producto correctamente | Dado que el usuario ingresó la URL de un producto válido de Fivisa Cuando selecciona Validar producto Entonces el sistema deberá consultar la fuente digital de Fivisa Y deberá identificar el producto correspondiente Y deberá recuperar un código o referencia externa Y deberá obtener el precio normal publicado del producto Y deberá mostrar el resultado al usuario antes de confirmar, solicitando además que seleccione expresamente USD o UYU sin asignar automáticamente la moneda desde la página de Fivisa. |
| Escenario: obtener precio normal | Dado que el usuario ingresó la URL de un producto válido de Fivisa Y el producto posee un precio normal publicado Cuando el sistema valida el producto Entonces deberá recuperar el precio normal publicado Y mostrar dicho precio al usuario antes de confirmar la vinculación. |
| Escenario: producto con descuento | Dado que un producto de Fivisa muestra un precio normal Y además muestra un precio promocional, descuento u oferta Cuando el sistema valida el producto Entonces deberá recuperar y utilizar el precio normal Y no utilizar el precio promocional o descontado como precio de la relación material-proveedor. |
| Escenario: actualización posterior | Dado que una relación material-Fivisa se encuentra vinculada y habilitada para actualización automática Cuando se ejecuta nuevamente la actualización de precios Entonces el sistema deberá obtener el precio normal publicado vigente del producto Y no utilizar descuentos, promociones u ofertas; si el nuevo precio es positivo, actualizar únicamente precio y fecha, conservando la moneda registrada sin conversión, y si es cero o negativo, conservar los valores anteriores. |
| Escenario: confirmar vinculación sobre una relación ya registrada | Dado que el producto fue validado correctamente Y la relación material-proveedor ya se encuentra registrada definitivamente en el catálogo Cuando el usuario confirma la vinculación Entonces deberán conservarse la URL y el código o referencia externa del producto Y el precio normal positivo recuperado y la moneda USD o UYU seleccionada expresamente por el usuario deberán conservarse en MaterialProveedor como precio actual y moneda de la relación Y deberá registrarse la fecha de actualización Y la relación deberá quedar habilitada para actualización automática. |
| Escenario: confirmar vinculación durante la creación de una nueva relación para un material existente en un presupuesto | Dado que el usuario está agregando Fivisa como nuevo proveedor de un material ya existente durante la elaboración de un presupuesto Y el producto de Fivisa fue validado correctamente Cuando el usuario confirma la vinculación Entonces los datos de vinculación deberán quedar asociados a la relación pendiente Y la relación todavía no deberá quedar registrada definitivamente en el catálogo Y todavía no deberá participar en la actualización automática. |
| Escenario: confirmar definitivamente la nueva relación | Dado que existe una nueva relación material-Fivisa pendiente con una vinculación validada Cuando el usuario selecciona Confirmar datos y continuar Entonces la relación material-proveedor deberá registrarse definitivamente en el catálogo Y deberán conservarse la URL, código o referencia externa, precio positivo, moneda USD o UYU seleccionada por el usuario y fecha de actualización Y la relación deberá quedar habilitada para la actualización automática definida en US-024. |
| Escenario: conservar datos propios del material | Dado que un producto de Fivisa fue vinculado correctamente Cuando se confirma la vinculación Entonces el nombre y la unidad de medida del Material deberán conservar los valores definidos por el usuario en EOP. |
| Escenario: validación fallida | Dado que la URL no permite identificar correctamente un producto de Fivisa, el precio normal recuperado es cero o negativo, o el usuario no selecciona una moneda válida USD o UYU Cuando intenta validar la URL o confirmar la vinculación Entonces la vinculación no deberá confirmarse Y no deberá reemplazarse ningún precio previamente registrado. |
| Escenario: bloquear modificación manual después de vincular | Dado que el usuario confirmó correctamente la vinculación de una relación material-proveedor con un producto de Fivisa Cuando la relación queda habilitada para actualización automática Entonces el precio deberá quedar como un valor no editable manualmente Y deberá mantenerse mediante la integración con Fivisa. |
| Prioridad: | Media |
| Dependencias: | US-008, US-010B o US-041 según el flujo desde el cual se inicie. |


## US-057 — Registrar un tipo de proyecto

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 61. Mockup US-057 — Registrar un tipo de proyecto

Tabla 64. Historia de usuario US-057 — Registrar un tipo de proyecto


| COMO: | Como Administrador |
|---|---|
| QUIERO: | Quiero registrar nuevos Tipos de proyecto |
| PARA: | Para mantener disponible el catálogo de tipos que puede utilizar EOP en presupuestos y proyectos. |
| DESCRIPCIÓN: | Desde la administración de Tipos de proyecto, el Administrador podrá registrar un nuevo Tipo de proyecto ingresando únicamente su Nombre. Antes de guardar, el sistema deberá comprobar que no exista otro Tipo de proyecto con el mismo nombre. Cuando el registro se complete correctamente, el nuevo Tipo de proyecto quedará inmediatamente disponible para ser seleccionado en las funcionalidades que requieran indicar un Tipo de proyecto. |
| Permisos aplicables: | Solo un Administrador podrá registrar Tipos de proyecto. |
| Reglas de negocio: | Cada Tipo de proyecto tendrá únicamente un Nombre. El Nombre será obligatorio. El Nombre deberá ser único. No deberá permitirse registrar dos Tipos de proyecto con el mismo nombre. Si el nombre ya existe, deberá impedirse el registro e informarse al usuario. Un Tipo de proyecto creado correctamente deberá quedar inmediatamente disponible para su utilización en presupuestos y proyectos. |
| Requisitos relacionados: | RF: RF-93. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: registrar Tipo de proyecto | Dado un Administrador Y un nombre de Tipo de proyecto que no se encuentra registrado Cuando ingresa el nombre y confirma Entonces deberá crearse el nuevo Tipo de proyecto Y deberá quedar disponible para su selección en las funcionalidades correspondientes. |
| Escenario: nombre obligatorio | Dado un Administrador Cuando intenta registrar un Tipo de proyecto sin ingresar Nombre Entonces el sistema deberá impedir el registro Y deberá indicar que el Nombre es obligatorio. |
| Escenario: nombre duplicado | Dado que ya existe un Tipo de proyecto con determinado Nombre Cuando el Administrador intenta registrar otro con el mismo Nombre Entonces el sistema deberá impedir la creación Y deberá informar que ya existe un Tipo de proyecto con ese Nombre. |
| Escenario: usuario no Administrador | Dado un usuario que no posee rol Administrador Cuando intenta acceder a la creación de Tipos de proyecto Entonces el sistema deberá impedir la operación. |
| Prioridad: | Alta. |
| Dependencias: | Gestión de usuarios, roles y permisos. |


## US-058 — Consultar y buscar tipos de proyecto

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 62. Mockup US-058 — Consultar y buscar tipos de proyecto

Tabla 65. Historia de usuario US-058 — Consultar y buscar tipos de proyecto


| COMO: | Como Administrador |
|---|---|
| QUIERO: | Quiero consultar y buscar los Tipos de proyecto registrados |
| PARA: | Para localizar y administrar fácilmente los tipos disponibles en la aplicación. |
| DESCRIPCIÓN: | La administración deberá disponer de una sección Tipos de proyecto. Desde esta sección, el Administrador podrá visualizar los Tipos de proyecto registrados y buscar un tipo por su Nombre. Para cada registro deberá mostrarse el Nombre y, cuando corresponda, la acción Eliminar definida en US-059. No será necesario incorporar filtros adicionales. |
| Permisos aplicables: | Solo un Administrador podrá acceder a la administración de Tipos de proyecto. |
| Reglas de negocio: | El listado deberá mostrar todos los Tipos de proyecto registrados. Para cada Tipo de proyecto deberá mostrarse su Nombre. El Administrador podrá buscar por Nombre. No será necesario incorporar filtros adicionales. Buscar o consultar no deberá modificar la información registrada. La disponibilidad de la acción Eliminar deberá respetar US-059. |
| Requisitos relacionados: | RF: RF-94; RF-95 solo para la acción condicional Eliminar (US-059, prioridad Baja). RNF: RNF-4, RNF-14. |
| Criterios de aceptación |  |
| Escenario: consultar Tipos de proyecto | Dado un Administrador Y existen Tipos de proyecto registrados Cuando accede a la administración de Tipos de proyecto Entonces deberá visualizar el listado de Tipos de proyecto existentes Y deberá mostrarse el Nombre de cada uno. |
| Escenario: buscar por Nombre | Dado que existen varios Tipos de proyecto registrados Cuando el Administrador ingresa un Nombre o parte de él en la búsqueda Entonces deberán mostrarse únicamente los Tipos de proyecto correspondientes al criterio ingresado. |
| Escenario: usuario no Administrador | Dado un usuario que no posee rol Administrador Cuando intenta acceder a la administración de Tipos de proyecto Entonces el sistema deberá impedir el acceso. |
| Prioridad: | Alta. |
| Dependencias: | US-057. |


## US-059 — Eliminar un tipo de proyecto

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 63. Mockup US-059 — Eliminar un tipo de proyecto

Tabla 66. Historia de usuario US-059 — Eliminar un tipo de proyecto


| COMO: | Como Administrador |
|---|---|
| QUIERO: | Quiero eliminar un Tipo de proyecto creado por error cuando todavía no haya sido utilizado |
| PARA: | Para mantener el catálogo correctamente organizado sin afectar información histórica. |
| DESCRIPCIÓN: | Desde la administración de Tipos de proyecto, el Administrador podrá solicitar la eliminación de un Tipo de proyecto. Antes de eliminarlo, el sistema deberá solicitar confirmación y verificar que el Tipo de proyecto no se encuentre asociado a ningún presupuesto ni proyecto. Si se encuentra en uso, la eliminación deberá impedirse. |
| Permisos aplicables: | Solo un Administrador podrá eliminar Tipos de proyecto. |
| Reglas de negocio: | La eliminación deberá requerir confirmación. Un Tipo de proyecto podrá eliminarse únicamente cuando no se encuentre utilizado por ningún presupuesto ni proyecto. La comprobación deberá considerar la información histórica existente. Si el Tipo de proyecto se encuentra en uso, la eliminación deberá impedirse. Cuando la eliminación sea impedida, deberá informarse al usuario que el Tipo de proyecto se encuentra utilizado. Cancelar la confirmación no deberá modificar información. La eliminación de un Tipo de proyecto no deberá afectar presupuestos ni proyectos existentes. |
| Requisitos relacionados: | RF: RF-95. RNF: RNF-1, RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: solicitar eliminación | Dado un Tipo de proyecto registrado Cuando el Administrador selecciona Eliminar Entonces el sistema deberá solicitar confirmación antes de continuar. |
| Escenario: eliminar Tipo de proyecto sin uso | Dado un Tipo de proyecto que no se encuentra asociado a ningún presupuesto ni proyecto Y el Administrador solicitó su eliminación Cuando confirma la operación Entonces el Tipo de proyecto deberá eliminarse del catálogo. |
| Escenario: impedir eliminación de Tipo de proyecto utilizado por un presupuesto | Dado un Tipo de proyecto asociado a uno o más presupuestos Cuando el Administrador intenta eliminarlo Entonces el sistema deberá impedir la eliminación Y deberá informar que el Tipo de proyecto se encuentra en uso. |
| Escenario: impedir eliminación de Tipo de proyecto utilizado por un proyecto | Dado un Tipo de proyecto asociado a uno o más proyectos Cuando el Administrador intenta eliminarlo Entonces el sistema deberá impedir la eliminación Y deberá informar que el Tipo de proyecto se encuentra en uso. |
| Escenario: cancelar eliminación | Dado que se muestra la confirmación de eliminación Cuando el Administrador selecciona Cancelar Entonces no deberá realizarse ninguna modificación. |
| Escenario: usuario no Administrador | Dado un usuario que no posee rol Administrador Cuando intenta eliminar un Tipo de proyecto Entonces el sistema deberá impedir la operación. |
| Prioridad: | Baja. |
| Dependencias: | US-058; gestión de presupuestos; gestión de proyectos. |


## US-060 — Registrar un tipo de documento

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 64. Mockup US-060 — Registrar un tipo de documento

Tabla 67. Historia de usuario US-060 — Registrar un tipo de documento


| COMO: | Como Supervisor o Administrador, o como Operador con el permiso individual específico correspondiente conforme a RF-73 |
|---|---|
| QUIERO: | Quiero registrar nuevos Tipos de documento |
| PARA: | Para disponer de las clasificaciones necesarias al cargar documentación en los proyectos. |
| DESCRIPCIÓN: | Desde la sección Tipos de documento, el usuario autorizado podrá registrar un nuevo Tipo de documento ingresando únicamente su Nombre. El sistema deberá validar que el Nombre sea obligatorio y único. Una vez registrado correctamente, deberá quedar inmediatamente disponible para clasificar nuevos documentos. |
| Permisos aplicables: | Por permisos base, Supervisor y Administrador podrán registrar Tipos de documento; el Operador solo con el permiso individual específico correspondiente conforme a RF-73. |
| Reglas de negocio: | Cada Tipo de documento tendrá únicamente Nombre. El Nombre será obligatorio. El Nombre será único. No podrán registrarse dos Tipos de documento con el mismo Nombre. Si el Nombre ya existe, deberá impedirse la creación e informarse al usuario. Un nuevo Tipo de documento deberá quedar inmediatamente disponible al cargar documentos. |
| Requisitos relacionados: | RF: RF-96. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: registrar Tipo de documento | Dado un usuario autorizado Y un Nombre de Tipo de documento que no se encuentra registrado Cuando ingresa el Nombre y confirma Entonces deberá crearse el Tipo de documento Y deberá quedar inmediatamente disponible para clasificar documentos. |
| Escenario: Nombre obligatorio | Dado un usuario autorizado Cuando intenta registrar un Tipo de documento sin ingresar Nombre Entonces el sistema deberá impedir el registro Y deberá indicar que el Nombre es obligatorio. |
| Escenario: Nombre duplicado | Dado que ya existe un Tipo de documento con determinado Nombre Cuando el usuario intenta registrar otro con el mismo Nombre Entonces el sistema deberá impedir la creación Y deberá informar que ya existe un Tipo de documento con ese Nombre. |
| Escenario: usuario sin autorización | Dado un usuario sin autorización para administrar Tipos de documento Cuando intenta registrar uno Entonces el sistema deberá impedir la operación. |
| Prioridad: | Alta. |
| Dependencias: | Gestión de usuarios, roles y permisos. |


## US-061 — Consultar tipos de documento

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 65. Mockup US-061 — Consultar tipos de documento

Tabla 68. Historia de usuario US-061 — Consultar tipos de documento


| COMO: | Como Supervisor o Administrador, o como Operador con el permiso individual específico correspondiente conforme a RF-73 |
|---|---|
| QUIERO: | Quiero consultar los Tipos de documento registrados |
| PARA: | Para conocer y administrar las clasificaciones disponibles para la documentación de los proyectos. |
| DESCRIPCIÓN: | La aplicación deberá disponer de una sección denominada Tipos de documento. Desde esta sección, el usuario autorizado podrá consultar los Tipos de documento registrados. Para cada Tipo deberá mostrarse su Nombre y, cuando corresponda, la acción Eliminar definida en US-062. |
| Permisos aplicables: | Supervisor y Administrador podrán acceder a la administración de Tipos de documento por permisos base; el Operador solo con el permiso individual específico correspondiente conforme a RF-73. |
| Reglas de negocio: | El listado deberá mostrar los Tipos de documento registrados. Para cada registro deberá mostrarse el Nombre. Cuando corresponda deberá encontrarse disponible la acción Eliminar. Consultar el listado no deberá modificar información. No se requiere búsqueda ni filtros dentro de este catálogo. |
| Requisitos relacionados: | RF: RF-97; RF-98 solo para la acción condicional Eliminar (US-062, prioridad Baja). RNF: RNF-4, RNF-14. |
| Criterios de aceptación |  |
| Escenario: consultar Tipos de documento | Dado un usuario autorizado Y existen Tipos de documento registrados Cuando accede a la sección Tipos de documento Entonces deberá visualizar el listado de Tipos de documento existentes Y deberá mostrarse el Nombre de cada uno. |
| Escenario: usuario sin autorización | Dado un usuario sin autorización para administrar Tipos de documento Cuando intenta acceder a la sección Entonces el sistema deberá impedir el acceso. |
| Prioridad: | Alta. |
| Dependencias: | US-060. |


## US-062 — Eliminar un tipo de documento

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 66. Mockup US-062 — Eliminar un tipo de documento

Tabla 69. Historia de usuario US-062 — Eliminar un tipo de documento


| COMO: | Como Supervisor o Administrador, o como Operador con el permiso individual específico correspondiente conforme a RF-73 |
|---|---|
| QUIERO: | Quiero eliminar un Tipo de documento creado por error cuando todavía no se encuentre utilizado |
| PARA: | Para mantener organizado el catálogo sin perder la clasificación histórica de documentos existentes. |
| DESCRIPCIÓN: | Desde la sección Tipos de documento, el usuario autorizado podrá solicitar la eliminación de un Tipo de documento. Antes de realizarla, el sistema deberá solicitar confirmación y verificar que dicho Tipo no se encuentre asociado a ningún documento existente. Si se encuentra en uso, la eliminación deberá impedirse. |
| Permisos aplicables: | Supervisor y Administrador podrán eliminar Tipos de documento por permisos base; el Operador solo con el permiso individual específico correspondiente conforme a RF-73. |
| Reglas de negocio: | La eliminación deberá requerir confirmación. Un Tipo de documento podrá eliminarse únicamente cuando no esté asociado a ningún documento. La verificación deberá contemplar todos los documentos existentes que utilicen dicho Tipo. Si se encuentra utilizado, deberá impedirse la eliminación. Cuando la eliminación sea impedida, deberá informarse al usuario. No deberá perderse ni modificarse la clasificación histórica de documentos existentes. Cancelar la confirmación no deberá modificar información. |
| Requisitos relacionados: | RF: RF-98. RNF: RNF-1, RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: solicitar eliminación | Dado un Tipo de documento registrado Cuando un usuario autorizado selecciona Eliminar Entonces el sistema deberá solicitar confirmación. |
| Escenario: eliminar Tipo de documento sin uso | Dado un Tipo de documento que no se encuentra asociado a ningún documento Cuando el usuario autorizado confirma su eliminación Entonces deberá eliminarse del catálogo. |
| Escenario: impedir eliminación de Tipo utilizado | Dado un Tipo de documento asociado a uno o más documentos existentes Cuando el usuario autorizado intenta eliminarlo Entonces el sistema deberá impedir la eliminación Y deberá informar que el Tipo de documento se encuentra en uso. |
| Escenario: cancelar eliminación | Dado que se muestra la confirmación Cuando el usuario selecciona Cancelar Entonces no deberá realizarse ninguna modificación. |
| Escenario: usuario sin autorización | Dado un usuario sin autorización para administrar Tipos de documento Cuando intenta eliminar un Tipo de documento Entonces el sistema deberá impedir la operación. |
| Prioridad: | Baja. |
| Dependencias: | US-061; gestión documental de proyectos. |


## US-063 — Registrar una unidad

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 67. Mockup US-063 — Registrar una unidad

Tabla 70. Historia de usuario US-063 — Registrar una unidad


| COMO: | Como Supervisor o Administrador, o como Operador con el permiso individual específico de administración de Unidades conforme a RF-73 |
|---|---|
| QUIERO: | Quiero registrar nuevas Unidades de medida |
| PARA: | Para poder utilizar las Unidades necesarias al registrar materiales en el catálogo de EOP. |
| DESCRIPCIÓN: | La aplicación dispondrá inicialmente de las Unidades m, m², m³, kg y litro. Cuando EOP necesite una Unidad que todavía no existe, un usuario autorizado podrá registrarla desde la sección Unidades ingresando únicamente su Nombre. El Nombre deberá ser obligatorio y único. Una vez creada correctamente, la nueva Unidad deberá quedar inmediatamente disponible para nuevos materiales. |
| Permisos aplicables: | Supervisor y Administrador podrán registrar Unidades por permisos base; el Operador solo con el permiso individual específico de administración de Unidades conforme a RF-73. |
| Reglas de negocio: | La aplicación dispondrá inicialmente de m, m², m³, kg y litro. Cada Unidad tendrá únicamente Nombre. El Nombre será obligatorio. El Nombre será único. No podrán registrarse dos Unidades con el mismo Nombre. Si el Nombre ya existe, deberá impedirse la creación e informarse al usuario. Una nueva Unidad deberá quedar inmediatamente disponible para registrar materiales. |
| Requisitos relacionados: | RF: RF-99. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: disponer de Unidades iniciales | Dado que la aplicación se encuentra disponible Cuando un usuario accede a una funcionalidad de registro de materiales Entonces deberán encontrarse disponibles para selección las Unidades m, m², m³, kg y litro. |
| Escenario: registrar nueva Unidad | Dado un usuario autorizado Y un Nombre de Unidad que todavía no existe Cuando ingresa el Nombre y confirma Entonces deberá registrarse la nueva Unidad Y deberá quedar inmediatamente disponible para seleccionar en nuevos materiales. |
| Escenario: Nombre obligatorio | Dado un usuario autorizado Cuando intenta registrar una Unidad sin Nombre Entonces el sistema deberá impedir el registro Y deberá indicar que el Nombre es obligatorio. |
| Escenario: Nombre duplicado | Dado que existe una Unidad con determinado Nombre Cuando el usuario intenta registrar otra con el mismo Nombre Entonces el sistema deberá impedir la creación Y deberá informar que la Unidad ya existe. |
| Escenario: usuario sin autorización | Dado un usuario sin autorización para administrar Unidades Cuando intenta registrar una nueva Unidad Entonces el sistema deberá impedir la operación. |
| Prioridad: | Alta. |
| Dependencias: | Gestión de usuarios, roles y permisos. |


## US-064 — Consultar unidades

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 68. Mockup US-064 — Consultar unidades

Tabla 71. Historia de usuario US-064 — Consultar unidades


| COMO: | Como Supervisor o Administrador, o como Operador con el permiso individual específico de administración de Unidades conforme a RF-73 |
|---|---|
| QUIERO: | Quiero consultar las Unidades registradas |
| PARA: | Para conocer las Unidades disponibles para la creación de materiales. |
| DESCRIPCIÓN: | La aplicación dispondrá de una sección Unidades. Desde dicha sección, el usuario autorizado podrá consultar todas las Unidades registradas, incluyendo las Unidades iniciales y las creadas posteriormente. Para cada Unidad deberá mostrarse su Nombre y, cuando corresponda, la acción Eliminar. |
| Permisos aplicables: | Supervisor y Administrador podrán acceder a la administración de Unidades por permisos base; el Operador solo con el permiso individual específico de administración de Unidades conforme a RF-73. |
| Reglas de negocio: | El listado deberá incluir todas las Unidades registradas. Deberán mostrarse también m, m², m³, kg y litro. Para cada Unidad deberá mostrarse su Nombre. Cuando corresponda deberá encontrarse disponible la acción Eliminar. Consultar el listado no deberá modificar información. No se requiere búsqueda ni filtros dentro de la sección Unidades. |
| Requisitos relacionados: | RF: RF-100; RF-101 solo para la acción condicional Eliminar (US-065, prioridad Baja). RNF: RNF-4, RNF-14. |
| Criterios de aceptación |  |
| Escenario: consultar Unidades | Dado un usuario autorizado Cuando accede a la sección Unidades Entonces deberá visualizar las Unidades registradas Y el listado deberá incluir las Unidades iniciales y las creadas posteriormente. |
| Escenario: usuario sin autorización | Dado un usuario sin autorización para administrar Unidades Cuando intenta acceder a la sección Entonces el sistema deberá impedir el acceso. |
| Prioridad: | Alta. |
| Dependencias: | US-063. |


## US-065 — Eliminar una unidad

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 69. Mockup US-065 — Eliminar una unidad

Tabla 72. Historia de usuario US-065 — Eliminar una unidad


| COMO: | Como Supervisor o Administrador, o como Operador con el permiso individual específico de administración de Unidades conforme a RF-73 |
|---|---|
| QUIERO: | Quiero eliminar una Unidad, precargada o creada posteriormente, cuando no esté asociada a ningún Material ni referenciada por un Concepto de Presupuesto |
| PARA: | Para mantener correctamente organizado el catálogo sin afectar materiales existentes. |
| DESCRIPCIÓN: | Desde la sección Unidades, un usuario autorizado podrá solicitar la eliminación de una Unidad precargada o creada posteriormente, si no está asociada a ningún Material ni referenciada por un Concepto de Presupuesto. Antes de completarla, el sistema deberá solicitar confirmación y verificar que la Unidad no esté asociada a Materiales registrados ni referenciada por Conceptos de Presupuesto. |
| Permisos aplicables: | Supervisor y Administrador podrán eliminar Unidades por permisos base; el Operador solo con el permiso individual específico de administración de Unidades conforme a RF-73. |
| Reglas de negocio: | La eliminación deberá requerir confirmación. Cualquier Unidad, precargada o creada posteriormente, podrá eliminarse únicamente cuando no esté asociada a ningún Material ni referenciada por un Concepto de Presupuesto. Si la Unidad está asociada a un Material o referenciada por un Concepto de Presupuesto, deberá impedirse su eliminación. Cuando la eliminación sea impedida, deberá informarse al usuario. La eliminación no deberá dejar Materiales existentes ni Conceptos de Presupuesto con referencias inválidas a la Unidad. Cancelar la confirmación no deberá producir modificaciones. Las Unidades precargadas m, m², m³, kg y litro podrán eliminarse si no están asociadas a ningún Material ni referenciadas por ningún Concepto de Presupuesto, sin protección especial por su origen. |
| Requisitos relacionados: | RF: RF-101. RNF: RNF-1, RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: solicitar eliminación | Dada una Unidad registrada Cuando un usuario autorizado selecciona Eliminar Entonces el sistema deberá solicitar confirmación. |
| Escenario: eliminar Unidad sin uso | Dada una Unidad, precargada o creada posteriormente Y dicha Unidad no se encuentra asociada a ningún Material ni referenciada por un Concepto de Presupuesto Cuando el usuario autorizado confirma su eliminación Entonces la Unidad deberá eliminarse. |
| Escenario: impedir eliminación de Unidad utilizada | Dada una Unidad asociada a uno o más Materiales, o referenciada por un Concepto de Presupuesto aun sin Materiales asociados Cuando el usuario autorizado intenta eliminarla Entonces el sistema deberá impedir la eliminación Y deberá informar que la Unidad se encuentra en uso. |
| Escenario: Eliminar Unidad precargada sin uso | Dada una Unidad precargada Y dicha Unidad no está asociada a ningún Material ni referenciada por un Concepto de Presupuesto Y el usuario posee autorización para eliminar Unidades Cuando solicita su eliminación y confirma Entonces la Unidad deberá eliminarse. |
| Escenario: Impedir eliminación de Unidad precargada utilizada | Dada una Unidad precargada asociada a uno o más Materiales Cuando un usuario autorizado intenta eliminarla Entonces el sistema deberá impedir la operación Y deberá informar que la Unidad está en uso. |
| Escenario: cancelar eliminación | Dado que se muestra la confirmación de eliminación Cuando el usuario selecciona Cancelar Entonces no deberá realizarse ninguna modificación. |
| Escenario: usuario sin autorización | Dado un usuario sin autorización para administrar Unidades Cuando intenta eliminar una Unidad Entonces el sistema deberá impedir la operación. |
| Prioridad: | Baja. |
| Dependencias: | US-064; catálogo de materiales. |


## US-066 — Agregar cuentas bancarias a un proveedor existente

Figura 70. Mockup US-066 — Agregar cuentas bancarias a un proveedor existente

Tabla 73. Historia de usuario US-066 — Agregar cuentas bancarias a un proveedor existente


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero agregar una nueva Cuenta bancaria a un Proveedor ya registrado |
| PARA: | Para mantener actualizada su información bancaria sin crear un nuevo Proveedor. |
| DESCRIPCIÓN: | El Supervisor o Administrador podrá seleccionar un Proveedor existente desde el módulo Proveedores y agregar una Cuenta bancaria. Cada Cuenta deberá registrar Banco, Número de cuenta, Titular y Moneda, y quedará asociada al mismo Proveedor. Banco se seleccionará de la lista fija; Moneda será UYU o USD. No se permitirá al Operador agregar Cuentas mediante permisos individuales. |
| Permisos aplicables: | Solo Supervisor y Administrador podrán agregar Cuentas a un Proveedor por permisos fijos de RF-73, con control del lado del servidor. Operador podrá consultar cuando corresponda pero no agregarlas. |
| Reglas de negocio: | Un Proveedor podrá tener múltiples Cuentas bancarias. La nueva Cuenta quedará asociada al Proveedor seleccionado. Agregarla no creará un nuevo Proveedor ni eliminará ni reemplazará las Cuentas existentes. Banco será obligatorio y se seleccionará de la lista hardcodeada de RF-23. Número de cuenta y Titular serán obligatorios. Moneda será obligatoria y será UYU o USD. No podrá guardarse una Cuenta incompleta. |
| Requisitos relacionados: | RF: RF-102. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: agregar Cuenta bancaria | Dado un Proveedor previamente registrado Y un usuario autorizado Cuando completa Banco, Número de cuenta, Titular y Moneda y confirma Entonces la nueva Cuenta quedará asociada al Proveedor. |
| Escenario: agregar segunda Cuenta | Dado un Proveedor que ya posee una Cuenta bancaria Cuando se agrega correctamente una segunda Cuenta Entonces ambas Cuentas permanecerán asociadas al mismo Proveedor. |
| Escenario: Cuenta incompleta | Dado que se intenta agregar una Cuenta Cuando falta Banco, Número de cuenta, Titular o Moneda Entonces el sistema impedirá guardar la Cuenta. |
| Escenario: Banco válido | Dado que se agrega una Cuenta Cuando se selecciona Banco Entonces solamente podrá seleccionarse una opción del listado hardcodeado de RF-23. |
| Escenario: Moneda válida | Dado que se agrega una Cuenta Cuando se selecciona Moneda Entonces únicamente estarán disponibles UYU y USD. |
| Escenario: usuario sin autorización | Dado un usuario sin permiso para realizar la operación Cuando intenta agregar una Cuenta bancaria Entonces el sistema impedirá la acción. |
| Prioridad: | Media. |
| Dependencias: | Gestión de Proveedores y permisos. |


## US-067A — Consultar y calcular el resumen económico del proyecto

Figura 71. Mockup US-067A — Consultar y calcular el resumen económico del proyecto

Tabla 74. Historia de usuario US-067A — Consultar y calcular el resumen económico del proyecto


| COMO: | Como Operador, Supervisor o Administrador con acceso efectivo al Proyecto. |
|---|---|
| QUIERO: | Quiero consultar los indicadores económicos del Proyecto y sus desgloses, calculados a partir del Presupuesto asociado, las Facturas y los costos adicionales registrados. |
| PARA: | Para comparar las estimaciones presupuestadas con los costos e ingresos registrados durante la ejecución y mantener actualizado su seguimiento económico. |
| DESCRIPCIÓN: | La ficha del Proyecto presenta cinco indicadores económicos calculados: Costo presupuestado sin margen, Precio de venta presupuestado, Costo según facturas de compra, Costo real total e Ingresos facturados, usando el Presupuesto vinculado, Facturas activas y costos adicionales. Los importes en UYU y USD se expresan mediante cotizaciones históricas válidas, sin conversiones pendientes ni modificación de Facturas guardadas. |
| Permisos aplicables: | Operador, Supervisor y Administrador con acceso efectivo al Proyecto podrán consultar indicadores y desgloses. Consultar no concederá permiso para agregar, modificar o eliminar costos adicionales, que se rige por US-067B; el servidor verificará autorizaciones. |
| Reglas de negocio: | El acceso a indicadores será de consulta para Operador, Supervisor y Administrador cuando tengan acceso al Proyecto. Los valores calculados no serán editables directamente. Costo presupuestado sin margen y Precio de venta presupuestado se obtendrán del Presupuesto vinculado; el primero no incluye Margen y el segundo incluye su efecto, en las monedas previstas. Sin Presupuesto, ambos aparecerán no disponibles. Bajo Costo presupuestado sin margen se mostrará una tabla con los conceptos y subtotales de costo del Presupuesto asociado, sin incluir el Margen ni modificar el Presupuesto. Costo según facturas de compra se calculará exclusivamente con Facturas de compra activas; Ingresos facturados, exclusivamente con Facturas de venta activas. Las e-Facturas, e-Tickets y Notas de débito sumarán; las Notas de crédito restarán una sola vez. Todos los Importes originales se conservarán positivos. Costo real total = Costo según facturas de compra + costos adicionales del Proyecto. Los costos adicionales se administrarán únicamente por Supervisor o Administrador mediante US-067B. Las Facturas eliminadas lógicamente no participarán en los indicadores ni contadores. Ninguna Factura guardada podrá editarse ni reasignarse a otro Proyecto. Cada Factura y costo adicional conservará Importe, Moneda y, cuando corresponda, cotización histórica y fecha aplicadas. Las conversiones USD/UYU se basarán en la Fecha de emisión o Fecha del costo y última cotización publicada anterior solo cuando ese día no exista publicación; una falla temporal del servicio no habilita usar una cotización de otra fecha. Las nuevas operaciones que requieran una cotización histórica válida no se guardarán si esta no está disponible ni puede obtenerse de datosUruguay; no existirán conversiones pendientes. Los registros con cotización histórica válida guardada reutilizarán ese valor y fecha en consultas posteriores, incluso si el servicio externo está temporalmente indisponible. Costo según facturas de compra e Ingresos facturados se consolidarán en UYU. Costo real total se consultará en UYU y USD sumando componentes con sus cotizaciones históricas respectivas, sin convertir el total con la cotización actual. El alta de una Factura activa y la eliminación lógica de otra actualizarán los indicadores que correspondan. El mantenimiento de costos adicionales también recalculará Costo real total, sin alterar el Presupuesto. Bajo Costo real total habrá un desglose de Facturas de compra y de costos adicionales, con sus importes y datos originales. La ausencia de costos registrados no impedirá por sí sola finalizar el Proyecto. Los indicadores no sustituirán una contabilidad de IVA, cobros, pagos ni saldos. Los importes resultantes se mostrarán con dos decimales, redondeando únicamente el resultado final y conservando precisión en cálculos intermedios. |
| Requisitos relacionados: | RF: RF-103, RF-54, RF-73, RF-77, RF-78, RF-104. RNF: RNF-4, RNF-10, RNF-12, RNF-13, RNF-14. |
| Criterios de aceptación |  |
| Escenario: consultar cinco indicadores | Dado un usuario autorizado y un Proyecto Cuando abre Resumen económico Entonces visualiza los cinco indicadores establecidos por RF-103, no editables directamente. |
| Escenario: presupuesto sin margen y con margen | Dado un Proyecto con Presupuesto asociado con costos y Margen Cuando consulta indicadores presupuestados Entonces Costo presupuestado sin margen no incorpora el Margen y Precio de venta presupuestado sí, conservando sus importes por moneda. |
| Escenario: desglosar el costo presupuestado | Dado un Presupuesto asociado con Materiales, Mano de obra y Margen Cuando el usuario consulta la tabla bajo Costo presupuestado sin margen Entonces ve los conceptos y subtotales de costo del Presupuesto Y el Margen no se incluye en esa tabla ni se modifica el Presupuesto. |
| Escenario: Proyecto sin Presupuesto | Dado un Proyecto sin Presupuesto asociado Cuando consulta Resumen económico Entonces los dos indicadores presupuestados figuran como no disponibles y los costos o ingresos registrados siguen consultables. |
| Escenario: Factura de compra aumenta costos | Dada una Factura de compra activa válida con cotización necesaria resuelta Cuando se registra correctamente Entonces aumenta Costo según facturas de compra y Costo real total, sin incrementar Ingresos facturados. |
| Escenario: Factura de venta aumenta ingresos | Dada una Factura de venta activa válida Cuando se registra correctamente Entonces aumenta Ingresos facturados sin afectar los costos. |
| Escenario: Nota de crédito descuenta una vez | Dada una Nota de crédito activa con Importe positivo Cuando se calcula el indicador de su categoría Entonces su contribución se resta exactamente una vez. |
| Escenario: Nota de débito suma una vez | Dada una Nota de débito activa con Importe positivo Cuando se calcula el indicador de su categoría Entonces su contribución se suma exactamente una vez. |
| Escenario: costos reales no modifican Presupuesto | Dado un Proyecto con Presupuesto y Facturas activas Cuando se registra o elimina lógicamente una Factura o se gestiona un costo adicional Entonces se actualizan los indicadores reales aplicables sin cambiar los importes presupuestados. |
| Escenario: baja lógica descuenta Factura | Dada una Factura de compra activa previamente contabilizada Cuando se elimina lógicamente Entonces se excluye de Costo según facturas de compra y Costo real total, sin reasignarla ni editar sus datos. |
| Escenario: conversión histórica de USD a UYU | Dada una Factura en USD con cotización histórica válida para su Fecha de emisión Cuando se calcula el costo o ingreso en UYU Entonces se multiplica el Importe original por UYU/USD sin alterar su Importe ni Moneda. |
| Escenario: última cotización anterior publicada | Dada una Fecha de emisión sin publicación ese día y con cotización anterior válida Cuando se obtiene el valor histórico Entonces se utiliza la última cotización publicada anterior y no una cotización futura. |
| Escenario: cotización inexistente impide nueva operación | Dada una Factura o costo adicional nuevo que requiere conversión y no dispone de cotización válida Cuando datosUruguay no la proporciona Entonces el guardado se rechaza y no queda conversión pendiente. |
| Escenario: reutilizar cotización histórica | Dado un registro económico previamente guardado con cotización histórica válida Cuando el servicio está indisponible y se consulta el resumen Entonces se reutilizan el valor y fecha almacenados, sin consultar una cotización actual. |
| Escenario: costo real total desglosado | Dado un Proyecto con Facturas de compra y costos adicionales válidos Cuando consulta Costo real total Entonces ve una fila de costos de compra y otra por cada costo adicional, con sus datos originales. |
| Escenario: costo real total en dos monedas | Dados costos con conversiones históricas válidas Cuando consulta el Costo real total en UYU y USD Entonces cada expresión suma sus componentes ya expresados en esa moneda, sin convertir el total con cotización actual. |
| Escenario: redondear al final | Dados cálculos con valores intermedios de más de dos decimales Cuando se muestran los indicadores Entonces se redondean los resultados finales a dos decimales sin cambiar importes originales. |
| Escenario: sin datos reales no bloquea estado | Dado un Proyecto sin Facturas ni costos adicionales Cuando un usuario autorizado lo marca Finalizado Entonces la ausencia de costos no bloquea por sí sola el cambio de estado. |
| Escenario: mostrar costos adicionales sin presupuesto | Dado un Proyecto sin Presupuesto asociado pero con costos adicionales válidamente registrados Cuando un usuario autorizado consulta el Resumen económico Entonces los indicadores presupuestados figuran como no disponibles y los costos adicionales participan del Costo real total con su cotización histórica correspondiente. |
| Escenario: consulta sin alterar registros históricos | Dado un Proyecto con Presupuesto, Facturas activas y costos adicionales previamente guardados Cuando un usuario autorizado consulta o actualiza la visualización del Resumen económico Entonces se recalculan los indicadores para su presentación sin modificar importes, fechas, cotizaciones históricas ni PDF de los registros originales. |
| Prioridad: | Alta. |
| Dependencias: | US-028, US-048, US-049, US-067B, US-068; Presupuesto asociado cuando exista; datosUruguay API para cotizaciones históricas. |


## US-067B — Gestionar costos adicionales del proyecto

Figura 72. Mockup US-067B — Gestionar costos adicionales del proyecto

Tabla 75. Historia de usuario US-067B — Gestionar costos adicionales del proyecto


| COMO: | Como Supervisor o Administrador autorizado para gestionar costos adicionales del Proyecto. |
|---|---|
| QUIERO: | Quiero agregar, consultar, modificar y eliminar los costos adicionales del Proyecto para los que tenga autorización. |
| PARA: | Para registrar los costos del Proyecto que no estén incluidos en las Facturas de compra asociadas y mantener actualizado el Costo real total de su seguimiento económico. |
| DESCRIPCIÓN: | Desde el Resumen económico del Proyecto se pueden registrar, modificar y eliminar costos adicionales manuales, además de consultarlos. Son costos no incluidos en las Facturas de compra. Toda operación que necesite conversión histórica debe tener una cotización válida antes de guardarse; no existirán conversiones pendientes. Operador podrá consultar, pero no gestionar costos. |
| Permisos aplicables: | Solo Supervisor y Administrador con acceso efectivo al Proyecto podrán agregar, modificar y eliminar costos adicionales. Operador podrá consultar el resumen, pero no gestionar costos. Todas las autorizaciones se verificarán del lado del servidor conforme a RF-73. |
| Reglas de negocio: | Únicamente Supervisor o Administrador podrán agregar, modificar o eliminar costos adicionales; Operador solo podrá consultar, sin concesiones individuales de permisos. Cada costo adicional pertenecerá a un solo Proyecto y requerirá Nombre no vacío, Importe numérico positivo, Moneda UYU o USD y Fecha del costo. El formulario Agregar costo mostrará la advertencia informativa de registrar solamente costos no incluidos en Facturas de compra para evitar doble contabilización. Al guardar un nuevo costo se conservarán sus valores originales y se incorporará a Costo real total, sin crear ni modificar Facturas. La cotización histórica UYU/USD se aplicará usando la Fecha del costo, o última cotización válida publicada anterior si ese día no existe publicación, conforme a RF-103; una falla temporal de datosUruguay no se interpretará como ausencia de publicación. Si un costo nuevo requiere conversión histórica y no se dispone de cotización válida ni datosUruguay la proporciona, se rechazará el guardado y se informará el motivo; no se guardarán conversiones pendientes. El usuario autorizado podrá modificar Nombre, Importe, Moneda o Fecha del costo. Si cambia Fecha o Moneda, se verificará nuevamente la cotización; si solo cambia Importe, podrá reutilizarse la cotización válida para Fecha y Moneda inalteradas. Si una modificación necesita otra cotización y no se consigue una válida, se rechazará la modificación y se conservarán los datos anteriores. Cancelar una edición tampoco los alterará. Eliminar un costo adicional requerirá confirmación y lo retirará del desglose y Costo real total sin alterar Facturas; cancelar la eliminación no producirá cambios. Los costos adicionales conservarán los valores originales y cotizaciones históricas efectivamente aplicadas en su propio registro, sin tabla independiente de cotizaciones. Las operaciones sobre costos adicionales actualizarán automáticamente Costo real total. Los cinco indicadores son calculados y no editables directamente. |
| Requisitos relacionados: | RF: RF-103, RF-54, RF-73. RNF: RNF-1, RNF-4, RNF-10, RNF-12, RNF-13, RNF-14, RNF-18. |
| Criterios de aceptación |  |
| Escenario: abrir formulario costo adicional | Dado un Supervisor o Administrador autorizado en Resumen económico Cuando selecciona Agregar costo Entonces se muestra un formulario con Nombre, Importe, Moneda y Fecha del costo, y la advertencia de doble contabilización. |
| Escenario: validar campos obligatorios | Dado un costo con Nombre vacío, Importe no positivo, Moneda inválida o Fecha ausente Cuando se intenta guardar Entonces el sistema impide el registro y señala los campos incorrectos. |
| Escenario: registrar costo válido | Dado un costo adicional con datos válidos y cotización histórica necesaria disponible Cuando el usuario lo guarda Entonces queda asociado al Proyecto y se recalcula Costo real total. |
| Escenario: cotización no disponible | Dado un nuevo costo en USD o UYU que necesita conversión histórica y no dispone de cotización válida Cuando datosUruguay no la proporciona Entonces el guardado se rechaza sin registrar costo ni conversión pendiente. |
| Escenario: convertir UYU a USD | Dado un costo en UYU con cotización histórica UYU/USD válida Cuando se calcula su equivalente USD Entonces se divide el Importe UYU por esa cotización, conservando el original. |
| Escenario: editar costo con nueva fecha | Dado un costo registrado y un Supervisor o Administrador autorizado Cuando modifica la Fecha del costo y se obtiene la nueva cotización histórica válida Entonces se guardan los cambios y se recalcula el total. |
| Escenario: rechazar edición sin nueva cotización | Dado un costo registrado y un cambio de Fecha o Moneda que requiere otra cotización Cuando el servicio no proporciona una cotización válida Entonces el sistema rechaza la edición y conserva el costo anterior. |
| Escenario: editar solo importe | Dado un costo con cotización histórica válida y Fecha y Moneda constantes Cuando se modifica únicamente el Importe Entonces se utiliza la cotización ya almacenada y se actualiza el Costo real total. |
| Escenario: cancelar edición | Dado un costo cuyo formulario tiene cambios sin guardar Cuando el usuario cancela Entonces se mantienen sus datos previamente registrados. |
| Escenario: eliminar costo | Dado un costo registrado y un Supervisor o Administrador autorizado Cuando confirma su eliminación Entonces el costo deja de contribuir a Costo real total y ninguna Factura ni su PDF se altera. |
| Escenario: cancelar eliminación | Dada una solicitud de eliminación de costo Cuando el usuario cancela Entonces el costo continúa registrado sin modificaciones. |
| Escenario: Operador sin mantenimiento | Dado un usuario Operador con acceso a Resumen económico Cuando intenta agregar, modificar o eliminar un costo adicional Entonces la aplicación rechaza la operación del lado del servidor. |
| Escenario: advertencia para evitar contabilizar dos veces | Dado un Supervisor o Administrador autorizado para registrar un costo adicional Cuando abre el formulario Agregar costo Entonces el sistema muestra la advertencia de no registrar nuevamente costos ya incluidos en Facturas de compra. |
| Escenario: falla temporal del servicio de cotizaciones | Dado un nuevo costo adicional en USD sin cotización almacenada para su Fecha del costo Cuando datosUruguay no responde Entonces se rechaza el guardado y se informa el motivo Y no se utiliza una cotización correspondiente a otra fecha. |
| Prioridad: | Alta. |
| Dependencias: | US-067A y US-028; cotización histórica válida cuando sea requerida; datosUruguay API y permisos fijos. |


## US-068 — Registrar facturas de compra manualmente con PDF original

Figura 73. Mockup US-068 — Registrar facturas de compra mediante PDF y extracción asistida por IA

Tabla 80. Historia de usuario US-068 — Registrar facturas de compra manualmente con PDF original


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero registrar manualmente una Factura de compra desde un Proyecto, con su PDF original y un Proveedor existente. |
| PARA: | Para centralizar los comprobantes de compra, relacionarlos con sus Proveedores y calcular los costos registrados del Proyecto. |
| DESCRIPCIÓN: | Desde la ficha de un Proyecto, el usuario autorizado registra manualmente una Factura de compra, selecciona el Tipo fijo, adjunta el PDF original, elige un Proveedor registrado y completa Fecha de emisión, Serie, Número real, Importe y Moneda. El Proyecto y EOP se obtienen automáticamente. No se utiliza OCR ni alta de Proveedores desde este formulario. El guardado exige cotización histórica válida cuando corresponda. La única confirmación definitiva de los datos se realiza al pulsar Guardar factura. |
| Permisos aplicables: | Solo Supervisor y Administrador autorizados pueden registrar Facturas de compra; el Operador solo consulta conforme a su rol. |
| Reglas de negocio: | Solo Supervisor y Administrador podrán registrar Facturas de compra. Operador tendrá únicamente consulta según los permisos fijos de RF-73. El registro se iniciará desde Facturas de compra de un Proyecto. Este se asignará automáticamente sin permitir elegir o cambiar Proyecto en el formulario. El Tipo de comprobante será uno de los seis valores fijos: e-Factura (DGI 111), e-Ticket (101), Nota de crédito de e-Factura (112), Nota de crédito de e-Ticket (102), Nota de débito de e-Factura (113) y Nota de débito de e-Ticket (103). El usuario podrá revisar o corregir el Tipo antes de guardar y podrá adjuntar el PDF sin confirmación previa del Tipo. El PDF original será obligatorio, válido y privado; podrá cargarse mediante selección del archivo o arrastrar y soltar. El formulario mostrará Receptor EOP, su RUT normalizado y Proyecto de origen automáticamente. El usuario seleccionará manualmente un Proveedor ya registrado; no podrá darlo de alta desde Facturas. Si el Proveedor necesario no existe, deberá registrarse previamente desde el módulo Proveedores de acuerdo con RF-23; la Factura no se guardará hasta seleccionar uno existente. El usuario ingresará manualmente Fecha de emisión, Serie, Número real del comprobante, Importe y Moneda. No se realizará extracción, OCR ni precarga por IA. Importe será positivo y representará el total final, con impuestos y redondeos correspondientes; Moneda admitirá únicamente UYU y USD. Los importes de Notas de crédito también serán positivos, aplicando el signo solo en indicadores. Se conservará el RUT emisor normalizado y validado del Proveedor seleccionado como dato histórico de la Factura, sin alterarlo ante cambios posteriores del Proveedor. Se impedirá el duplicado únicamente contra Facturas de compra activas de igual RUT emisor histórico normalizado, Tipo, Serie y Número real. Una eliminada lógicamente permitirá un registro nuevo con otro FC-####. La operación que requiera conversión histórica válida utilizará datosUruguay para la Fecha de emisión o última publicación anterior válida. Si falta cotización utilizable y el servicio no la obtiene, el sistema rechazará el guardado y no almacenará conversiones pendientes. El PDF original se conservará privadamente en Azure Blob Storage, con referencia en la base de datos y acceso autorizado; no se incorporará automáticamente a los documentos genéricos del Proyecto. El identificador FC-#### será correlativo global independiente de FV-####, empezando en FC-0001 y continuando después de FC-9999 con FC-10000, sin reinicios ni reutilización de códigos. Una Factura activa estará asociada exclusivamente al Proyecto de origen, sin edición posterior, cambio de Proveedor, PDF ni reasignación. Solo podrá eliminarse lógicamente mediante RF-78. Las compras activas contribuirán a Costo según facturas de compra y Costo real total, con signo del Tipo aplicado una vez; no contribuirán a Ingresos facturados. Una cancelación, datos inválidos o falta de cotización requerida impedirán crear una Factura definitiva incompleta. |
| Requisitos relacionados: | RF: RF-104, RF-23, RF-73, RF-78, RF-83, RF-103. RNF: RNF-4, RNF-5, RNF-6, RNF-10, RNF-11, RNF-12, RNF-13, RNF-18. |
| Criterios de aceptación |  |
| Escenario: iniciar compra por rol autorizado | Dado un Proyecto existente y un Supervisor o Administrador autorizado Cuando el usuario inicia Registrar factura de compra Entonces se abre el formulario manual con el Proyecto fijado. |
| Escenario: Operador sin registro | Dado un usuario Operador Cuando intenta registrar una Factura de compra Entonces el servidor deniega la acción sin crear registros. |
| Escenario: tipo fijo obligatorio sin confirmación intermedia | Dado un registro de compra iniciado Cuando el usuario intenta guardar sin seleccionar uno de los seis Tipos fijos Entonces no puede completar el registro. |
| Escenario: PDF obligatorio | Dado un formulario con datos válidos pero sin PDF original Cuando intenta guardar Entonces la aplicación impide el registro y solicita el PDF. |
| Escenario: entrada manual sin OCR | Dado un Tipo seleccionado y un PDF original adjunto Cuando el usuario ingresa Fecha, Serie, Número real, Importe y Moneda Entonces todos los datos se completan manualmente sin extracción ni precarga automática. |
| Escenario: Proyecto y EOP automáticos | Dado un Proyecto existente Cuando el usuario abre la carga de compra Entonces se muestran el Proyecto y EOP/RUT receptor configurados como solo lectura. |
| Escenario: selección Proveedor existente | Dado un Proveedor registrado Cuando el usuario lo busca y selecciona manualmente Entonces el comprobante queda asociado a ese Proveedor y conserva su RUT emisor histórico al guardar. |
| Escenario: sin alta de Proveedor dentro de Facturas | Dado un Proveedor no registrado Cuando el usuario intenta crear uno desde la Factura Entonces no se ofrece esa operación; deberá registrarlo desde la sección Proveedores antes de completar la Factura. |
| Escenario: Proveedor obligatorio | Dado un formulario de compra sin Proveedor existente seleccionado Cuando intenta guardar Entonces se rechaza el registro. |
| Escenario: importe inválido | Dado un Importe cero o negativo Cuando intenta guardar la Factura Entonces se rechaza el registro y se solicita un importe positivo. |
| Escenario: moneda inválida | Dada Moneda distinta de UYU o USD Cuando intenta guardar Entonces se rechaza el registro. |
| Escenario: duplicado activo | Dada una Factura de compra activa con igual RUT emisor histórico normalizado, Tipo, Serie y Número real Cuando se registra el mismo comprobante Entonces se rechaza el duplicado. |
| Escenario: reingreso de compra eliminada | Dada una Factura de compra eliminada lógicamente con esa misma clave y ninguna activa equivalente Cuando se registra nuevamente con PDF y datos válidos Entonces se crea un registro nuevo con otro FC-#### sin restaurar el anterior. |
| Escenario: cotización histórica requerida | Dada una Factura de compra que necesita conversión y no tiene cotización válida Cuando datosUruguay no obtiene la cotización histórica utilizable Entonces se rechaza Guardar factura sin dejar conversión pendiente. |
| Escenario: PDF privado y Proyecto único | Dada una Factura de compra válida, con Proveedor, PDF y cotización necesaria Cuando se confirma su registro Entonces se guarda su PDF privado, su RUT emisor histórico y su relación con un solo Proyecto. |
| Escenario: identificador posterior a 9999 | Dado que el último código de compra fue FC-9999 Cuando se registra otra Factura válida Entonces recibe FC-10000 y no se modifica la secuencia FV. |
| Escenario: nota de crédito en costo | Dada una Nota de crédito de compra con importe positivo Cuando se registra Entonces su contribución resta una sola vez de Costo según facturas de compra y Costo real total. |
| Escenario: Facturas de compra no generan ingresos | Dada una Factura de compra válida Cuando se incorpora al Resumen económico Entonces se actualizan costos y no Ingresos facturados. |
| Escenario: compra no editable después de guardar | Dada una Factura de compra ya guardada Cuando se consulta su detalle Entonces no se puede editar Proveedor, datos, PDF ni Proyecto; solo solicitar eliminación lógica autorizada. |
| Escenario: cancelación sin registro parcial | Dada una carga de compra iniciada y no validada Cuando el usuario cancela o falla una validación Entonces no se crea una Factura definitiva incompleta. |
| Escenario: RUT emisor del Proveedor permanece histórico | Dada una Factura de compra correctamente registrada con un Proveedor existente Cuando se modifica posteriormente el RUT de ese Proveedor, incluido Fivisa Entonces la Factura mantiene su RUT emisor histórico normalizado registrado al momento del alta y el comprobante no se modifica. |
| Escenario: nota de débito contribuye una vez al costo | Dada una Nota de débito de compra válida registrada con Importe positivo Cuando se calcula Costo según facturas de compra del Proyecto Entonces su Importe se suma una sola vez, sin alterar Ingresos facturados. |
| Escenario: rechazo de cotización no genera factura parcial | Dada una Factura de compra nueva que requiere conversión histórica Cuando no existe una cotización válida y datosUruguay no proporciona una aplicable Entonces se rechaza Guardar factura, se informa el motivo y no se crea Factura ni conversión pendiente. |
| Prioridad: | Alta. |
| Dependencias: | Proyecto y Proveedor registrados; RF-23 y RF-73; cotización histórica cuando sea necesaria y PDF original privado. |


## US-069 — Consultar y filtrar facturas de compra

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 74. Mockup US-069 — Consultar y filtrar facturas de compra

Tabla 76. Historia de usuario US-069 — Consultar y filtrar facturas de compra


| COMO: | Como Operador, Supervisor o Administrador con permiso efectivo de consulta conforme a RF-73. |
|---|---|
| QUIERO: | Quiero consultar, buscar y filtrar las Facturas de compra registradas. |
| PARA: | Para localizar comprobantes, identificar sus Proveedores y Proyectos y consultar su información y PDF originales. |
| DESCRIPCIÓN: | El módulo general Facturas tendrá un apartado Facturas de compra independiente del de venta, con únicamente Facturas activas registradas mediante US-068 y accesibles según RF-73. Cada entrada mostrará FC-####, Tipo de comprobante, Serie, Número real, Fecha de emisión, Proveedor, Importe registrado, Moneda y Proyecto asociado. Toda Factura activa tendrá exactamente un Proyecto; desde su información se podrá ver o descargar el PDF original conforme a RF-83. |
| Permisos aplicables: | — |
| Reglas de negocio: | El apartado de compra estará separado del de venta y solo mostrará Facturas de compra activas a las que el usuario tenga acceso efectivo, con autorización comprobada del lado del servidor conforme a RF-73. El listado mostrará al menos FC-####, Tipo de comprobante, Serie, Número real, Fecha de emisión, Proveedor, Importe registrado, Moneda y Proyecto asociado. Toda Factura activa tendrá exactamente un Proyecto; no se mostrarán Estados de asociación. La búsqueda admitirá únicamente FC-#### o Número real del comprobante; los únicos filtros serán Fecha, Proveedor y Proyecto asociado. Búsqueda y filtros podrán combinarse entre sí, independientes de los criterios de ventas. Desde la información de una Factura autorizada se podrá visualizar o descargar su PDF original conforme a RF-83. Consultar, buscar, filtrar, visualizar o descargar no alterará datos, PDF ni Proyecto asociado. El Proveedor de la Factura no constituye una relación de precios Material-Proveedor de Presupuestación ni su consulta la crea. |
| Requisitos relacionados: | RF: RF-105, RF-106, RF-83, RF-73. RNF: RNF 4, RNF 5, RNF-14. |
| Criterios de aceptación |  |
| Escenario: listado de compras | Dado un usuario con acceso a Facturas de compra activas Cuando abre el apartado Facturas de compra Entonces ve FC-####, Tipo, Serie, Número real, Fecha, Proveedor, Importe registrado, Moneda y Proyecto asociado por cada registro. |
| Escenario: listado vacío | Dado un usuario autorizado y ninguna Factura de compra activa accesible Cuando abre Facturas de compra Entonces ve el listado vacío sin Facturas de venta. |
| Escenario: buscar por FC-#### | Dado Facturas de compra con identificadores internos diferentes Cuando el usuario busca un FC-#### determinado Entonces aparecen únicamente las Facturas de compra coincidentes. |
| Escenario: buscar por Número real | Dado Facturas de compra con números reales diferentes Cuando el usuario busca un Número real Entonces aparecen las Facturas coincidentes sin confundirlo con FC-####. |
| Escenario: filtrar por Fecha | Dado Facturas de compra emitidas en Fechas diferentes Cuando el usuario aplica el filtro de Fecha Entonces aparecen únicamente las Facturas que cumplen ese filtro. |
| Escenario: filtrar por Proveedor | Dado Facturas de compra de Proveedores diferentes Cuando el usuario elige un Proveedor en el filtro Entonces aparecen únicamente las Facturas de compra de ese Proveedor. |
| Escenario: filtrar por Proyecto | Dado Facturas de compra asociadas a distintos Proyectos Cuando el usuario filtra por Proyecto asociado Entonces aparecen únicamente las Facturas actualmente asociadas al Proyecto seleccionado. |
| Escenario: combinar búsqueda y filtros | Dado Facturas de compra con códigos, fechas, Proveedores y Proyectos diferentes Cuando combina búsqueda por FC-#### o Número real con Fecha, Proveedor y Proyecto Entonces se muestran solo registros que cumplen simultáneamente los criterios elegidos. |
| Escenario: separar compras y ventas | Dado Facturas activas de compra y venta Cuando el usuario consulta Facturas de compra Entonces no ve Facturas de venta ni usa filtros propios de ventas. |
| Escenario: consulta sin modificación | Dado una Factura de compra registrada con PDF y Proyecto asociado Cuando el usuario autorizado consulta sus datos Entonces permanecen sin cambio los datos, el PDF y la asociación. |
| Escenario: visualizar PDF autorizado | Dado una Factura de compra activa y un usuario autorizado a consultar su PDF Cuando selecciona Ver PDF desde la información de la Factura Entonces visualiza el PDF original correspondiente a esa Factura. |
| Escenario: descargar PDF autorizado | Dado una Factura de compra activa y un usuario autorizado a descargar su PDF Cuando selecciona Descargar PDF Entonces obtiene el PDF original correspondiente sin modificarlo. |
| Escenario: solamente activas con Proyecto | Dado Facturas de compra activas asociadas a Proyectos y Facturas eliminadas fuera de la información activa Cuando el usuario consulta el listado Entonces solo ve Facturas activas cada una con un Proyecto, sin Estado de asociación. |
| Escenario: consulta sin relación Material-Proveedor | Dado una Factura de compra asociada a un Proveedor Cuando un usuario consulta la Factura Entonces no se crea ni modifica una relación Material-Proveedor de precios en Presupuestación. |
| Prioridad: | Alta. |
| Dependencias: | US-068; registro de Facturas de compra; Proveedores, Proyectos y permisos efectivos. |


## US-070 — Reasignar una factura de compra a otro proyecto

> **FUERA DEL ALCANCE de la primera versión.** No implementar.
>
> Ver la tabla de funcionalidades excluidas en `alcance.md`.

Figura 75. Mockup US-070 — Reasignar una factura de compra a otro proyecto

Tabla 77. Historia de usuario US-070 — Reasignar una factura de compra a otro proyecto


| COMO: | Como Supervisor o Administrador, o como Operador con el permiso individual específico de reasignación de Facturas de compra conforme a RF-73. |
|---|---|
| QUIERO: | Quiero cambiar el Proyecto al que pertenece una Factura de compra existente sin volver a registrar el comprobante ni subir su PDF. |
| PARA: | Para corregir la asociación y mantener correctamente calculados los costos de los Proyectos afectados. |
| DESCRIPCIÓN: | Toda Factura de compra se asocia automáticamente a un Proyecto desde el registro de US-068. Un usuario autorizado puede ejecutar Reasignar en una Factura existente, buscar por Nombre cualquier Proyecto registrado independientemente del Cliente, confirmar y trasladar su única asociación. Se conservan código, Proveedor, datos, PDF y cotización histórica aplicable; se actualizan listados, cantidades y costos de los Proyectos de origen y destino conforme a RF-55, RF-103 y RF-107. |
| Permisos aplicables: | Reasignar requiere permiso específico efectivo conforme a RF-73: Supervisor y Administrador por sus permisos base; Operador con permiso individual de reasignación de compras. |
| Reglas de negocio: | Se buscará por Nombre cualquier Proyecto de destino registrado, sin restricción de Cliente; la regla de mismo Cliente de ventas no se aplicará a compras. Podrá elegirse un Proyecto Pendiente de inicio, En ejecución, En pausa, Finalizado o Cancelado. El Estado no será por sí solo una restricción adicional. La reasignación requerirá confirmación explícita: la Factura dejará el Proyecto de origen y pasará exclusivamente al destino. Nunca quedará activa sin Proyecto; no se ofrecerá acción Desvincular independiente. Se conservarán FC-####, Proveedor, Tipo, Serie, Número real, Fecha de emisión, Importe registrado, Moneda y PDF original, sin volver a cargar el PDF ni ejecutar Azure. No se creará otra Factura. La Fecha de emisión y Moneda no cambiarán por Reasignar, por lo que se conservará la cotización histórica válida aplicable. Si la conversión estaba pendiente, seguirá sujeta a RF-103 sin inventar importes equivalentes. La Factura desaparecerá de las compras del Proyecto anterior y aparecerá en las del destino; disminuirá el contador de compras del origen y aumentará el del destino conforme a RF-55. Se recalcularán Costo según facturas de compra y Costo real total de ambos Proyectos conforme a RF-103; el Costo real total incluye además costos adicionales, y la Factura nunca contará en ambos Proyectos a la vez. Si el usuario cancela o la operación falla, seguirá íntegramente asociada al Proyecto original sin cambios parciales en datos, PDF, contadores o costos. |
| Requisitos relacionados: | RF: RF-107, RF-104, RF-103, RF-55, RF-73. RNF: RNF 1, RNF 4, RNF 10. |
| Criterios de aceptación |  |
| Escenario: buscar Proyecto por Nombre | Dada una Factura de compra activa ya asociada al Proyecto A Cuando un usuario autorizado selecciona Reasignar y busca Proyectos por Nombre Entonces puede consultar los Proyectos registrados disponibles para destino. |
| Escenario: seleccionar Proyecto de otro Cliente | Dada una Factura de compra del Proyecto del Cliente A Cuando el usuario autorizado selecciona un Proyecto registrado del Cliente B Entonces el destino es válido y no se aplica la restricción de mismo Cliente propia de ventas. |
| Escenario: seleccionar Proyecto Finalizado | Dada una Factura de compra activa y un Proyecto Finalizado registrado Cuando el usuario autorizado lo busca como destino Entonces puede seleccionarlo, sin bloqueo por su Estado. |
| Escenario: seleccionar Proyecto Cancelado | Dada una Factura de compra activa y un Proyecto Cancelado registrado Cuando el usuario autorizado lo busca como destino Entonces puede seleccionarlo, sin bloqueo por su Estado. |
| Escenario: confirmar reasignación | Dada una Factura de compra asociada al Proyecto A y un destino B válido Cuando el usuario autorizado confirma expresamente Reasignar Entonces la Factura deja A y queda exclusivamente asociada a B. |
| Escenario: conservar identificador y datos | Dada una Factura de compra con FC-####, Proveedor, datos y PDF original Cuando se confirma su reasignación Entonces se conservan el mismo FC-####, Proveedor, Tipo, Serie, Número real, Fecha, Importe, Moneda y PDF sin crear una Factura nueva. |
| Escenario: no volver a cargar PDF | Dada una Factura de compra cuyo PDF original está almacenado Cuando se completa Reasignar Entonces el mismo PDF continúa con la misma Factura, sin nueva carga ni extracción obligatoria de Azure. |
| Escenario: actualizar listados y cantidades | Dada una Factura de compra actualmente asociada a A Cuando se reasigna correctamente al Proyecto B Entonces desaparece del listado de A, aparece en el de B y se actualizan ambos contadores conforme a RF-55. |
| Escenario: recalcular costos de origen y destino | Dada una Factura que contribuye a los costos del Proyecto A Cuando se reasigna correctamente a B Entonces se recalculan Costo según facturas de compra y Costo real total de A y B sin contabilizarla simultáneamente en ambos. |
| Escenario: conservar cotización histórica | Dada una Factura con cotización histórica válida para Fecha de emisión y Moneda Cuando se reasigna sin editar esos datos Entonces conserva la cotización aplicable y no utiliza la fecha de reasignación. |
| Escenario: conservar conversión pendiente | Dada una Factura de compra cuya conversión histórica continúa pendiente Cuando se reasigna a otro Proyecto Entonces conserva Importe y Moneda originales, los indicadores de ambos Proyectos siguen RF-103 y no se inventa una cotización. |
| Escenario: cancelar reasignación | Dada una Factura de compra asociada a A y una reasignación iniciada Cuando el usuario cancela Entonces la Factura permanece asociada a A sin cambios. |
| Escenario: impedir Factura sin Proyecto | Dada una Factura de compra activa asociada a un Proyecto Cuando se consultan las acciones de asociación Entonces no existe Desvincular independiente ni puede quedar activa sin Proyecto. |
| Escenario: denegar operación sin permiso | Dado un Operador sin permiso individual de reasignación de compras Cuando intenta reasignar una Factura Entonces el sistema deniega la operación sin modificar la asociación. |
| Escenario: autorizar Operador con permiso | Dado un Operador con permiso individual efectivo de reasignación de compras Cuando selecciona cualquier destino registrado y confirma Entonces se permite la operación respetando la integridad y recálculos aplicables. |
| Escenario: fallo durante reasignación | Dada una Factura de compra asociada a A y una reasignación iniciada hacia B Cuando ocurre un error que impide completarla Entonces la Factura continúa asociada a A, sin quedar duplicada, sin Proyecto ni parcialmente transferida. |
| Prioridad: | Alta. |
| Dependencias: | US-068, US-069, Proyectos registrados y permisos efectivos. |


## US-071 — Modificar un material y sus referencias de precio desde catálogo

Figura 76. Mockup US-071 — Modificar un material existente y actualizar sus precios por proveedor

Tabla 78. Historia de usuario US-071 — Modificar un material y sus referencias de precio desde catálogo


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero modificar los datos de un material existente y actualizar manualmente los precios de los proveedores asociados. |
| PARA: | Para mantener actualizado el catálogo de materiales de EOP, conservando sus relaciones y la información histórica de los presupuestos. |
| DESCRIPCIÓN: | Desde la edición de un Material existente en el Catálogo de Materiales, el Supervisor o Administrador podrá modificar su Nombre y Unidad fija, y mantener manualmente los precios y monedas de sus relaciones material-proveedor existentes. Al confirmar un cambio económico se actualizará la fecha correspondiente. Las relaciones nuevas se registran aparte mediante US-010B y los proveedores deben existir. La actualización no altera valores históricos de Presupuestos. No existen vínculos automáticos con Fivisa. |
| Permisos aplicables: | Supervisor y Administrador pueden editar Material y referencias existentes; Operador consulta sin modificar. Se comprueban permisos fijos de RF-73 en el servidor. |
| Reglas de negocio: | El Material conserva su identidad y relaciones existentes; Nombre y Unidad obligatorios, Nombre único normalizado y Unidad elegida entre opciones fijas. Cada referencia existente admite cambio manual de precio estrictamente mayor que cero y/o moneda UYU/USD, actualizando fecha al confirmar. Cambios aplicados solo a relación seleccionada; otras relaciones no se modifican. No se habilita creación ni eliminación de relaciones desde esta edición; las nuevas siguen US-010B. No se alteran retroactivamente precios y monedas históricos de los Presupuestos; Las referencias de precio de Fivisa se mantienen manualmente bajo las mismas validaciones que las de cualquier otro Proveedor; no habrá vinculación ni sincronización automática. Fallos no aplican cambios parciales. |
| Requisitos relacionados: | RF: RF-21, RF-22, RF-67, RF-68, RF-73, RF-108. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: modificar Nombre y Unidad | Dado un Material existente y un Supervisor o Administrador autorizado Cuando edita Nombre y Unidad fija con datos válidos Entonces se actualiza el mismo Material y se conservan sus relaciones. |
| Escenario: modificar precio | Dado una referencia material-proveedor existente Cuando el usuario autorizado modifica su precio por uno mayor que cero y confirma Entonces se guarda el nuevo precio y se actualiza la fecha correspondiente. |
| Escenario: modificar moneda | Dado una referencia existente con moneda UYU o USD Cuando el usuario autorizado cambia la moneda a la otra opción válida Entonces se conserva el precio si no fue modificado y se actualiza la fecha de referencia. |
| Escenario: conservar otras relaciones | Dado un Material con varios Proveedores Cuando se cambia el precio de uno de ellos Entonces las demás referencias conservan precio, moneda y fecha previos. |
| Escenario: preservar valores históricos | Dado un Material que ya fue seleccionado en Presupuestos Cuando se modifica su precio de catálogo Entonces los conceptos guardados conservan sus valores históricos. |
| Escenario: unidad inválida o Nombre duplicado | Dado una Unidad que no existe entre opciones fijas o Nombre duplicado normalizado Cuando se intenta guardar Entonces se rechaza la edición sin cambios parciales. |
| Escenario: precio inválido | Dado un precio cero, negativo o inválido Cuando se confirma una modificación Entonces se rechaza y se conserva el valor anterior. |
| Escenario: precio Fivisa manual | Dado una relación material-proveedor cuyo Proveedor es Fivisa Cuando un Supervisor o Administrador modifica el precio Entonces puede actualizarlo manualmente, sin vinculación ni sincronización externa. |
| Escenario: Operador no modifica | Dado un Operador consultando el catálogo Cuando intenta editar Material o referencia Entonces el servidor deniega la operación sin cambiar datos. |
| Prioridad: | Alta. |
| Dependencias: | US-040 (consulta del catálogo); US-010A (referencia seleccionada en presupuesto); US-010B (nuevas relaciones). |


## US-072 — Modificar datos de proveedor, incluido el RUT de Fivisa

Figura 77. Mockup US-072 — Modificar un Proveedor existente

Tabla 79. Historia de usuario US-072 — Modificar datos de proveedor, incluido el RUT de Fivisa


| COMO: | Como Supervisor o Administrador. |
|---|---|
| QUIERO: | Quiero modificar los datos registrados de un Proveedor existente. |
| PARA: | Para corregir su ficha sin duplicar el Proveedor ni perder asociaciones y datos históricos. |
| DESCRIPCIÓN: | Desde la sección Proveedores, Supervisor o Administrador puede corregir Nombre, RUT, Teléfono, Correos y datos de Cuentas bancarias existentes de un Proveedor sin duplicarlo. El RUT es editable también para Fivisa, sujeto exactamente a las validaciones de unicidad, formato y dígito verificador aplicables a todos los proveedores. Las relaciones con Materiales y Facturas se mantienen, conservando datos históricos. La incorporación de Cuentas nuevas se realiza conforme a US-066; no se eliminan Cuentas ni Proveedores. |
| Permisos aplicables: | Solo Supervisor y Administrador pueden modificar Proveedores; Operador podrá consultar y seleccionar existentes, sin permisos individuales ni excepción para Fivisa. |
| Reglas de negocio: | Nombre, RUT y Teléfono obligatorios; Nombre y RUT únicos normalizados. RUT uruguayo válido con dígito verificador, almacenable a dígitos; Teléfono compatible con E.164. Al menos uno de los dos Correos válido y una Cuenta bancaria completa, con Banco de lista fija, Número, Titular y Moneda UYU/USD. Se permite cambiar el RUT de Fivisa con las mismas reglas generales; no existe restricción especial de inmutabilidad. La modificación conserva asociaciones y los valores históricos de facturas. Si una validación falla, no hay cambios parciales. |
| Requisitos relacionados: | RF: RF-23, RF-73, RF-102, RF-109. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: modificar datos válidos | Dado un Supervisor o Administrador y un Proveedor existente Cuando modifica Nombre, RUT, Teléfono, correos o Cuentas existentes con datos válidos Entonces se actualiza el mismo Proveedor conservando sus relaciones. |
| Escenario: modificar RUT de Fivisa | Dado que el Proveedor registrado corresponde a Fivisa Cuando un Supervisor o Administrador ingresa un nuevo RUT válido y único y confirma Entonces se admite el cambio de RUT igual que para los demás Proveedores. |
| Escenario: RUT inválido o duplicado | Dado un RUT inválido, con dígito incorrecto o asignado a otro Proveedor Cuando se intenta guardar el cambio Entonces se rechaza e informa el motivo conservando el valor anterior, incluso si se trata de Fivisa. |
| Escenario: Nombre o contactos inválidos | Dado un Nombre duplicado normalizado, Teléfono inválido o ausencia de Correos válidos Cuando se intenta modificar la ficha Entonces se rechaza sin alterar los datos anteriores. |
| Escenario: cuenta bancaria incompleta | Dado una Cuenta existente que se intenta editar sin Banco, Número, Titular o Moneda válidos Cuando se confirma Entonces se impide guardar y se conserva la Cuenta previa. |
| Escenario: conservar históricos y relaciones | Dado un Proveedor asociado a Materiales y Facturas Cuando se modifica su RUT u otros datos Entonces las asociaciones permanecen y los RUT e importes históricos de facturas no se sobrescriben. |
| Escenario: Operador sin edición | Dado un Operador que puede consultar Proveedores Cuando intenta modificar uno por interfaz o URL Entonces el servidor rechaza la operación. |
| Prioridad: | Alta. |
| Dependencias: | US-009 (alta exclusiva del módulo Proveedores); US-066 (nuevas Cuentas); US-074 (acceso al listado). |


## US-073 — Visualizar cronograma mediante diagrama de Gantt

Figura 78. Mockup US-073 — Visualizar cronograma mediante diagrama de Gantt

Tabla 80. Historia de usuario US-073 — Visualizar cronograma mediante diagrama de Gantt


| COMO: | Como Operador, Supervisor o Administrador con acceso al Proyecto. |
|---|---|
| QUIERO: | Quiero visualizar la distribución temporal de las tareas del proyecto |
| PARA: | Para comprender y seguir la planificación del proyecto. |
| DESCRIPCIÓN: | Operador, Supervisor y Administrador podrán consultar el Gantt dentro de la sección Cronograma de la ficha del proyecto. Las tareas con Fecha de inicio y Fecha de fin alimentarán las barras del diagrama de Gantt dentro de Cronograma; las que no tengan Fecha de fin se mostrarán en la sección independiente «Tareas sin fecha de finalización», sin dejar de poder consultarse y gestionarse. Las modificaciones realizadas sobre sus fechas deberán reflejarse en la representación actualizada. El Porcentaje de avance registrado deberá reflejarse en la visualización. El Gantt será de solo lectura: no se permitirán arrastres, ajustes o ediciones directas de barras. Las fechas y avances se modificarán mediante las operaciones de tareas; tampoco se calcularán dependencias automáticas entre tareas. |
| Permisos aplicables: | Según la descripción y la matriz fija de permisos por rol de RF-73. |
| Reglas de negocio: | El diagrama deberá generarse utilizando las tareas y fechas registradas para el proyecto correspondiente. El Gantt representará como barras solo las tareas con Fecha de inicio y Fecha de fin, reflejando su Porcentaje de avance; las tareas sin Fecha de fin se mostrarán separadamente en «Tareas sin fecha de finalización», sin barra. Los cambios de fechas y Porcentaje de avance realizados mediante la edición de Tareas deberán reflejarse al actualizar el Gantt. No se podrán arrastrar ni modificar directamente las barras, ni se calcularán dependencias automáticas entre Tareas. |
| Requisitos relacionados: | RF: RF-66. RNF: RNF-14, RNF-4. |
| Criterios de aceptación |  |
| Escenario: Visualizar cronograma Gantt | Dado un proyecto con tareas que poseen Fecha de inicio y Fecha de fin registradas Cuando el usuario visualiza el cronograma Entonces deberá generarse el Gantt a partir de dichas fechas Y deberá reflejarse el Porcentaje de avance registrado para cada tarea representada; las tareas del Proyecto sin Fecha de fin deberán aparecer en «Tareas sin fecha de finalización» sin barras, conservando consulta y gestión. |
| Escenario: Actualizar cronograma Gantt | Dado que se modifican las fechas de una tarea Cuando vuelve a visualizarse el Gantt actualizado Entonces la representación deberá reflejar las nuevas fechas; si se asigna una Fecha de fin válida a una tarea sin ella, dejará la sección «Tareas sin fecha de finalización» y pasará a mostrarse como barra en el Gantt. |
| Escenario: actualizar cronograma por avance | Dado que se modifica el Porcentaje de avance de una tarea Cuando vuelve a visualizarse el Gantt actualizado Entonces la representación deberá reflejar el nuevo nivel de avance. |
| Escenario: No editar mediante el Gantt | Dado un usuario que consulta el diagrama de Gantt Cuando intenta arrastrar, redimensionar o modificar directamente una barra Entonces el Gantt no deberá alterar fechas, avance ni otros datos de la Tarea Y los cambios deberán realizarse desde las operaciones de gestión de Tareas. |
| Prioridad: | Baja. |
| Dependencias: | Tareas registradas con Fecha de inicio, Fecha de fin y Porcentaje de avance, conforme a RF-62 (US-037). |


## US-074 — Consultar listado de proveedores

Figura 79. Mockup US-074 — Consultar listado de proveedores

Tabla 81. Historia de usuario US-074 — Consultar listado de proveedores


| COMO: | Como Administrador, Supervisor u Operador |
|---|---|
| QUIERO: | Quiero consultar un listado de los Proveedores registrados desde una sección independiente y acceder desde allí a su alta, a su modificación y a la incorporación de Cuentas bancarias |
| PARA: | Para administrar el catálogo único de Proveedores desde un solo lugar y disponer de ellos al seleccionarlos en presupuestos, facturas de compra y materiales. |
| DESCRIPCIÓN: | El sistema dispondrá de una sección independiente denominada Proveedores, accesible desde el menú principal, que mostrará un listado sencillo de los Proveedores registrados, sin filtros ni buscador. Desde el listado podrá accederse a la opción Registrar proveedor, que utilizará el alta definida en US-009 conforme a las reglas de RF-23. Cada Proveedor del listado dispondrá de la opción Modificar, que se regirá por US-072 y RF-109. Desde la ficha o edición del Proveedor podrá accederse a Agregar cuenta bancaria, que se regirá por US-066 y RF-102. El registro y la modificación de Proveedores se realizarán exclusivamente desde esta sección. En presupuestos, facturas de compra y formularios de materiales solamente podrán seleccionarse Proveedores existentes; si se necesita un Proveedor que todavía no existe, deberá registrarse previamente en la sección Proveedores. Las validaciones de datos, duplicados y autorizaciones del alta, la modificación y la incorporación de Cuentas bancarias se mantienen en las historias y requerimientos referenciados y no se duplican en esta historia. |
| Permisos aplicables: | Se aplicarán exclusivamente los roles fijos Administrador, Supervisor y Operador, con los permisos de la matriz base de RF-73 para Proveedores; no existirán permisos individuales. Operador: consultar el listado de Proveedores y seleccionar Proveedores existentes en las operaciones que lo requieran. Supervisor: consultar el listado, registrar y modificar Proveedores y agregar Cuentas bancarias. Administrador: acceso completo. La autorización se verificará también del lado del servidor. |
| Reglas de negocio: | La sección Proveedores será independiente y estará accesible desde el menú principal. El listado mostrará de forma sencilla los Proveedores registrados; no se implementarán filtros ni buscador. El registro de Proveedores se realizará exclusivamente desde la opción Registrar proveedor de esta sección, mediante el alta definida en US-009 y con las reglas de datos, duplicados y confirmación de RF-23. La modificación de Proveedores se realizará exclusivamente desde la opción Modificar de cada Proveedor del listado, conforme a US-072 y RF-109. La incorporación de Cuentas bancarias se iniciará desde la ficha o edición del Proveedor mediante Agregar cuenta bancaria, conforme a US-066 y RF-102. En presupuestos, facturas de compra y formularios de materiales solamente podrán seleccionarse Proveedores existentes; no podrán registrarse Proveedores nuevos desde esas operaciones. Si un usuario necesita un Proveedor que todavía no existe, deberá registrarlo previamente en la sección Proveedores. Un Proveedor registrado o modificado desde esta sección quedará disponible en el listado y para su selección en las demás operaciones del sistema. Las opciones Registrar proveedor, Modificar y Agregar cuenta bancaria solo estarán disponibles para los roles autorizados por la matriz base de RF-73. Si el rol no está autorizado, el servidor rechazará la operación aunque se intente acceder directamente mediante una URL u otro mecanismo. |
| Requisitos relacionados: | RF: RF-23, RF-73, RF-102, RF-109. RNF: RNF-4, RNF-10, RNF-18. |
| Criterios de aceptación |  |
| Escenario: visualizar listado de Proveedores | Dado que existen Proveedores registrados Y un usuario con rol Administrador, Supervisor u Operador Cuando accede a la sección Proveedores desde el menú principal Entonces el sistema muestra el listado de los Proveedores registrados Y no presenta filtros ni buscador. |
| Escenario: acceder al alta de Proveedor | Dado un Supervisor o Administrador en la sección Proveedores Cuando selecciona Registrar proveedor Entonces el sistema presenta el alta definida en US-009 Y aplica las validaciones de datos y duplicados de RF-23. |
| Escenario: Proveedor registrado disponible | Dado que un Supervisor o Administrador completó correctamente el alta desde la sección Proveedores Cuando confirma el registro Entonces el nuevo Proveedor aparece en el listado Y queda disponible para su selección en presupuestos, facturas de compra y materiales. |
| Escenario: acceder a la modificación | Dado un Proveedor del listado Y un Supervisor o Administrador Cuando selecciona Modificar Entonces el sistema presenta la edición del mismo Proveedor conforme a US-072. |
| Escenario: agregar Cuenta bancaria | Dado un Supervisor o Administrador en la ficha o edición de un Proveedor Cuando selecciona Agregar cuenta bancaria Entonces el sistema presenta la incorporación de una nueva Cuenta conforme a US-066 Y la Cuenta registrada queda asociada al mismo Proveedor. |
| Escenario: rol no autorizado | Dado un usuario con rol Operador en la sección Proveedores Cuando intenta registrar o modificar un Proveedor o agregarle una Cuenta bancaria Entonces el sistema no ofrece la acción Y el servidor rechaza la operación aunque se intente acceder directamente, sin alterar los datos existentes. |
| Escenario: Proveedor inexistente en otra operación | Dado que un usuario elabora un presupuesto, registra una factura de compra o completa un formulario de materiales Y el Proveedor que necesita no está registrado Cuando busca el Proveedor para seleccionarlo Entonces el sistema solo permite seleccionar Proveedores existentes Y no ofrece registrar uno nuevo desde esa operación. |
| Prioridad: | Alta. |
| Dependencias: | US-009 (alta de Proveedores), US-072 (modificación de Proveedores) y US-066 (incorporación de Cuentas bancarias); RF-73 para la matriz base de permisos por rol. |


Evidencia de testing

Manual de deploy

Manual de Usuario

Inventario de recursos utilizados

Tabla 82. Versión de recursos utilizados


- **Aplicación / Herramienta /Framework:** Versión

