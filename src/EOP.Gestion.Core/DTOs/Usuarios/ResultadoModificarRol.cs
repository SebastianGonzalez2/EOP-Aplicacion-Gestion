namespace EOP.Gestion.Core.DTOs.Usuarios;

// Las cuatro formas en las que puede terminar un pedido de cambio de rol.
// Son resultados esperados del negocio, no errores del sistema, por eso se
// devuelven como un valor y no como una excepción. El Controller decide qué
// mensaje mostrar en cada caso.
public enum ResultadoModificarRol
{
    RolModificado,
    RolNoPermitido,
    UsuarioNoEncontrado,
    UltimoAdministradorActivo
}
