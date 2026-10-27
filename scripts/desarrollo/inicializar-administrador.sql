
-- Inicialización controlada del primer administrador.
-- EXCLUSIVO PARA LA BASE DE DATOS LOCAL DE DESARROLLO.
-- La cuenta debe existir previamente en Microsoft Entra ID.

USE [EopGestionDev];
GO

-- Reemplazar estos valores antes de ejecutar el script.
DECLARE @CorreoCorporativo NVARCHAR(254) =
    N'REEMPLAZAR_CORREO_ENTRA';

DECLARE @NombreCompleto NVARCHAR(150) =
    N'REEMPLAZAR_NOMBRE';

-- Evitar ejecutar el script sin configurar los datos.
IF @CorreoCorporativo = N'REEMPLAZAR_CORREO_ENTRA'
   OR @NombreCompleto = N'REEMPLAZAR_NOMBRE'
BEGIN
    THROW 50001, 'Debes configurar los datos del administrador.', 1;
END;

-- Si el usuario ya existe, no lo insertamos nuevamente.
IF NOT EXISTS (
    SELECT 1
    FROM dbo.Usuarios
    WHERE CorreoCorporativo = @CorreoCorporativo
)
BEGIN
    -- No crear otro administrador inicial si ya existe uno activo.
    IF EXISTS (
        SELECT 1
        FROM dbo.Usuarios
        WHERE Rol = 1 AND Activo = 1
    )
    BEGIN
        THROW 50002, 'Ya existe un administrador activo.', 1;
    END;

    INSERT INTO dbo.Usuarios
        (NombreCompleto, CorreoCorporativo, Rol, Activo, EntraObjectId)
    VALUES
        (@NombreCompleto, @CorreoCorporativo, 1, 1, NULL);
END;

-- Consultar el usuario encontrado o creado.
SELECT Id, NombreCompleto, CorreoCorporativo,
       Rol, Activo, EntraObjectId
FROM dbo.Usuarios
WHERE CorreoCorporativo = @CorreoCorporativo;
