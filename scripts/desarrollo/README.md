# ¿Para qué sirve esta carpeta?

Acá guardamos scripts auxiliares para preparar el entorno de desarrollo, no código que se ejecute cada vez que abrimos EOP.

Por ahora está `inicializar-administrador.sql`. Sirve para registrar el primer administrador en una base local de desarrollo, cuando todavía no tenemos uno activo que pueda crear otros usuarios. Antes de ejecutarlo, hay que completar los datos del usuario y comprobar que su cuenta exista en Microsoft Entra ID.

Es exclusivo para desarrollo local. No ejecutarlo en la base real de Azure ni guardar datos personales o credenciales en el repositorio.
