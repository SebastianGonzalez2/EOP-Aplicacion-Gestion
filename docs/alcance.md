# Alcance de la primera versión

> Generado desde el anteproyecto corregido. **El documento Word es el original**;
> este archivo es la copia de trabajo del repositorio.

Alcance Funcional incluido

Tabla 9. Alcance funcional incluido


| US incluida | Funcionalidad |  |
|---|---|---|
| Presupuestación y propuestas comerciales |  |  |
| US-001 | Consultar presupuestos | Listado y filtros por estado, cliente y fecha. |
| US-002 | Crear presupuesto | Alta para un cliente registrado (o registrado durante el alta, según US-053), datos básicos y responsable de la elaboración. |
| US-003 | Guardar borradores | Guardar y retomar la elaboración sin perder conceptos ni datos registrados. |
| US-004 | Registrar relevamiento | Introducir texto, grabar audio o subir un archivo de audio previamente grabado. |
| US-005 | Transcribir y revisar | Transcripción asistida por IA; el usuario revisa y corrige el texto. |
| US-006 | Generar tabla desde relevamiento | IA interpreta conceptos; resultados editables, sin vinculación automática al catálogo. |
| US-007 | Vincular materiales | Búsqueda y selección manual de materiales ya registrados. |
| US-009 | Seleccionar proveedores | Elegir proveedores existentes; altas solo desde la sección Proveedores. |
| US-010A | Seleccionar referencia de precio | Tomar proveedor/precio/moneda del catálogo y conservar los valores históricos del presupuesto. |
| US-010B | Crear relación material-proveedor | Alta explícita de nueva relación y precio desde catálogo; solo proveedores existentes. |
| US-011 | Agregar conceptos | Materiales, mano de obra y otros conceptos mediante edición manual de la tabla. |
| US-012 | Calcular importes | Subtotales, márgenes, monedas y conversiones según reglas económicas vigentes. |
| US-013 | Revisar resumen | Verificar importes y confirmar avance sin actualizar precios del catálogo. |
| US-014 | Exportar Excel | Generar y descargar .xlsx cuando el usuario lo solicita; no guardar versiones en la app. |
| US-016 | Cargar PDF comercial | Subir el PDF final elaborado fuera del sistema; reemplazo de la versión vigente. |
| US-017 | Revisión interna | Pasar a revisión con PDF final vigente y condiciones requeridas. |
| US-018 | Registrar envío al cliente | Envío real por canal externo; registrar en la aplicación la acción, medio y fecha. |
| US-019 | Registrar respuesta del cliente | Aprobación, rechazo o solicitud de cambios; creación de proyecto cuando corresponda. |
| US-020 | Vencimientos y reapertura | Comprobar vencimiento al consultar u operar; sin tarea programada específica. |
| US-021 | Cancelar presupuesto | Cancelación controlada conforme a permisos y estado. |
| US-023 | Estadísticas de presupuestos | Consultar las métricas y motivos de rechazo previstos; sin dashboards adicionales. |
| Proyectos, documentos y tareas |  |  |
| US-025 | Consultar proyectos | Listado y filtros de proyectos. |
| US-026 | Crear proyecto | Alta manual con cliente registrado (o registrado durante el alta, según US-053), responsable y datos generales. |
| US-027 | Proyecto desde presupuesto | Crear proyecto una sola vez al aprobar un presupuesto que aún no tiene proyecto. |
| US-028 | Ficha y resumen del proyecto | Datos, estados y accesos a secciones; los submódulos implementan sus propios detalles. |
| US-029 | Editar datos de proyecto | Nombre, tipo y fechas; cambio de responsable bajo restricciones de permisos y tareas. |
| US-030 | Cambiar estado de proyecto | Estados fijos, cambio manual sin motor de transiciones complejo. |
| US-031 | Administrar participantes | Agregar/quitar integrantes autorizados; conservar consistencia con responsables de tareas. |
| US-032 | Presupuesto del proyecto | Consultar presupuesto único vinculado o iniciar uno nuevo si corresponde. |
| US-033 | Subir documentos | Archivos admitidos hasta 100 MB, almacenados de forma privada. |
| US-034 | Consultar documentos | Búsqueda, filtros y descarga; vista previa solo PDF, JPG/JPEG y PNG. |
| US-035 | Eliminar documento | Retirar documentos de la consulta mediante eliminación lógica, conservando registro y archivo original privado. |
| US-036 | Facturas del proyecto | Consultar comprobantes de compra y venta únicamente desde cada proyecto. |
| US-037 | Registrar tarea | Nombre, responsable y planificación básica; otros datos conforme al formulario acordado. |
| US-038 | Modificar/eliminar tareas | Editar tarea según responsabilidad/rol; eliminación controlada por autorización. |
| US-039 | Consultar tareas | Listado de tareas con filtros básicos. |
| US-073 | Visualizar Gantt | Representación de fechas y avance; sin edición por arrastre ni dependencias automáticas. |
| Catálogo de materiales y proveedores |  |  |
| US-040 | Consultar materiales | Listado del catálogo con unidad, proveedor y precios registrados. |
| US-041 | Registrar material | Alta exclusivamente desde catálogo, con primera relación válida material-proveedor. |
| US-066 | Agregar cuenta bancaria | Registrar cuentas adicionales para un proveedor existente. |
| US-071 | Editar material y precios | Modificar nombre/unidad y precio/moneda de proveedores asociados, solo en catálogo. |
| US-072 | Editar proveedor | Corregir datos y cuentas existentes sin romper relaciones; el RUT de Fivisa se modifica con las mismas validaciones que el resto. |
| US-074 | Consultar proveedores | Listado sencillo sin buscador ni filtros; navegación a altas, ediciones y cuentas. |
| Administración y seguridad |  |  |
| US-042 | Iniciar/cerrar sesión | Microsoft Entra ID y MFA [13]; confirmar acceso al tenant/identidades. |
| US-043 | Registrar usuario | Alta administrativa de usuario con rol fijo y estado. |
| US-044 | Roles y permisos | Tres roles fijos; sin permisos individuales ni creación de roles. |
| US-045 | Consultar usuarios | Listado administrativo sencillo, sin buscador ni filtros. |
| US-046 | Activar/inactivar usuario | Cambiar disponibilidad de usuarios; proteger al último administrador. |
| US-047 | Editar usuario | Modificar datos permitidos; el rol y el estado tienen acciones propias. |
| Facturación, clientes y economía |  |  |
| US-048 | Registrar venta | Factura de venta manual con PDF obligatorio; sin OCR; duplicados entre activas. |
| US-049 | Eliminar factura | Únicamente baja lógica; no edición posterior de datos ni reasignación. |
| US-052 | Consultar clientes | Listado, búsqueda y filtros básicos. |
| US-053 | Registrar cliente | Alta en sección Clientes o durante la creación de un presupuesto o proyecto, con datos fiscales y contacto validados. |
| US-054 | Modificar cliente | Correcciones permitidas sin alterar datos históricos de facturas. |
| US-055 | Activar/inactivar cliente | Mantener historial; no utilizar clientes inactivos en nuevas operaciones. |
| US-067A | Resumen económico | Cinco indicadores de costo/ingreso con cotizaciones históricas válidas. |
| US-067B | Costos adicionales | Alta, edición y baja de costos de proyecto, con validación de moneda/cotización. |
| US-068 | Registrar compra | Factura de compra manual con PDF, proveedor existente y sin OCR. |


Funcionalidades excluidas

Tabla 10. Funcionalidades excluidas


| US excluida(s) | Funcionalidad retirada | Alternativa que se mantiene |
|---|---|---|
| US-008 | Alta de materiales desde un presupuesto | El material nuevo se registra desde el catálogo (US-041). |
| US-015 | Generación de propuesta comercial Word mediante IA | EOP elabora la propuesta fuera de la aplicación y sube el PDF final mediante US-016. |
| US-022 | Acceso directo a documentos desde el listado de presupuestos | Los documentos se consultan desde la ficha del presupuesto. |
| US-024 | Actualización automática de precios de proveedores | Precios actualizados manualmente desde el catálogo. |
| US-050 y US-069 | Listados generales de facturas de venta y compra | Se consultan en cada proyecto mediante US-036. |
| US-051 y US-070 | Reasignación de facturas de venta o compra | Eliminación lógica y nueva carga en el proyecto correcto. |
| US-056 | Vinculación de productos y precios con Fivisa | Sin extracción automática ni mantenimiento de enlaces a productos de Fivisa. |
| US-057, US-058 y US-059 | Administración de tipos de proyecto | Tipos predefinidos no administrables. |
| US-060, US-061 y US-062 | Administración de tipos de documento | Tipos predefinidos no administrables. |
| US-063, US-064 y US-065 | Administración de unidades de medida | Unidades predefinidas no administrables. |


Bibliografía
