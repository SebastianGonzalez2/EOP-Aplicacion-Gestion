namespace EOP.Gestion.Core.DTOs.Clientes;

// Un país tal como lo ofrecemos en el selector del número de WhatsApp.
//
// Lo identificamos por su código ISO (UY, AR, US...) y NO por el prefijo, porque
// hay países que comparten prefijo: Estados Unidos y Canadá usan los dos el +1.
// Si guardáramos solo "+1" en el formulario, no sabríamos cuál de los dos eligió el usuario.
public sealed record PaisTelefonico(string CodigoIso, string Nombre, int Prefijo)
{
    // El texto que ve el usuario en el desplegable, por ejemplo: Uruguay (+598).
    public string Descripcion => $"{Nombre} (+{Prefijo})";
}
