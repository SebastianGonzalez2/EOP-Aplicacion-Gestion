namespace EOP.Gestion.Core.DTOs.Usuarios;

// Las formas en las que puede terminar un pedido de cambio de estado (activar o inactivar).
// Son resultados esperados del negocio, no errores del sistema, por eso se devuelven como
// un valor y no como una excepción. El Controller decide qué mensaje mostrar en cada caso.
public enum ResultadoCambiarEstadoUsuario
{
    UsuarioInactivado,
    UsuarioReactivado,

    // El usuario ya estaba en el estado pedido (por ejemplo, otro Administrador lo cambió
    // un instante antes y el listado que tenía abierto quedó desactualizado). No se toca nada.
    YaTeniaEseEstado,

    UsuarioNoEncontrado,
    UltimoAdministradorActivo
}
