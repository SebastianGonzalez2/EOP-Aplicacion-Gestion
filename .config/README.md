# ¿Para qué sirve esta carpeta?

Acá está `dotnet-tools.json`, que le dice a .NET qué herramientas necesitamos y en qué versión. Por ahora tenemos `dotnet-ef` (10.0.10), que usamos para las migraciones de Entity Framework Core, es decir, los cambios en la estructura de la base de datos.

Si alguien clona el repo, puede ejecutar `dotnet tool restore` desde la raíz para instalar la misma versión que usamos todos. No hace falta mover esta carpeta ni modificar el archivo a mano.
