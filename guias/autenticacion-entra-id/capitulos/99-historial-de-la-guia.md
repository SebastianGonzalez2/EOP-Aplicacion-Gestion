# Anexo C. Historial y estado de la guía

## Versiones

| Versión | Fecha | Tareas incorporadas | Cambios |
|---|---|---|---|
| 1.0 | 29/10/2026 | S1-T08, S1-T09 | Primera versión: capítulos 1 a 11, glosario y referencias |

## Qué se verificó y qué se reconstruyó

| Contenido | Estado | Cómo |
|---|---|---|
| Código de `Program.cs`, `CuentaController`, `ManejadorFallosInicioSesion`, vistas y pruebas | ✅ Verificado | Leído de `main` (merge `fdc9e78`) |
| Versiones de paquetes y del SDK | ✅ Verificado | `.csproj` y `dotnet --list-sdks` |
| Claves de User Secrets | ✅ Verificado (solo nombres) | `dotnet user-secrets list`, sin copiar valores |
| Parámetros reales del pedido a Microsoft y cookies temporales | ✅ Verificado | Capturados con la app corriendo en local |
| Capturas de la app (login de Microsoft, Sesión cerrada, Error de inicio de sesión) | ✅ Reales | `herramientas/capturar.mjs` |
| Lista y cantidad de pruebas (33; 24 de la T09) | ✅ Verificado | `dotnet test --list-tests` |
| Nombres de menús y botones del portal | 📘 Según documentación oficial | No se navegó el portal (no se usaron credenciales) |
| Pasos exactos en el portal durante la T08 y la T09 | 🧩 Reconstruido | El repositorio no los registra |
| Mecanismo de MFA del tenant (Security defaults o Conditional Access) | 🧩 No verificado | Solo consta que el login pidió MFA en las pruebas manuales |
| Capturas del portal de Azure | ⏳ Pendientes | Se agregan en `imagenes/portal/` (ver README) |

## Tareas previstas que ampliarán esta guía

| Tarea | Tema | Capítulo sugerido |
|---|---|---|
| S1-T10 | Vincular el usuario de EOP con la identidad de Entra ID (`oid` ↔ `EntraObjectId`, por correo corporativo en el primer acceso) | 12. Vinculación de identidades |
| S1-T11 | Rechazar identidades no registradas o Inactivas; verificar MFA desde la app | 13. Restricciones de acceso |
| S1-T12 / S1-T13 | Roles fijos y autorización por rol en el servidor | 14. Roles y permisos |
| S1-T30 | Pruebas de autenticación y permisos | Ampliar el capítulo 9 |
