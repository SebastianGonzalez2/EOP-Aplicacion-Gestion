# ¿Para qué sirve esta carpeta?

Acá está `ci.yml`, la receta que usa GitHub Actions para comprobar automáticamente que EOP compila y que pasan las pruebas.

Cuando abrimos un PR hacia main o subimos cambios a main, GitHub puede descargar el código, instalar .NET 10, restaurar dependencias, compilar, levantar un SQL Server temporal y ejecutar las pruebas. También podemos iniciarlo manualmente desde Actions.

No es un despliegue ni utiliza nuestra base real de Azure. Los resultados se ven en Actions o en los checks del PR. Si Actions está deshabilitado, no se ejecuta hasta volver a habilitarlo. No mover esta carpeta: GitHub busca los workflows en esta ubicación.
