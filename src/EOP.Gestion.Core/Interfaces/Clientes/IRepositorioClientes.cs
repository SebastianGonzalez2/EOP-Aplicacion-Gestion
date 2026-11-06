using System;
using System.Collections.Generic;
using System.Text;
using EOP.Gestion.Core.Entidades.Clientes;

namespace EOP.Gestion.Core.Interfaces.Clientes
{
    public interface IRepositorioClientes
    {
        // Comprueba si ya existe un cliente con el RUT normalizado, si existe devuelve true, si no existe devuelve false
        //este metodo es el que se crea cuando se implementa la interfaz IRepositorioClientes
        Task<bool> ExistePorRutAsync(string rutNormalizado,CancellationToken cancellationToken = default);




        // Registra un nuevo cliente en la base de datos.
        // Recibe el cliente validado y permite cancelar la operación.
        //
        // S1-T27: devuelve false si no se guardó porque otro cliente con el mismo RUT se registró
        // justo antes (entre la consulta de ExistePorRutAsync y este guardado). Cualquier otro
        // problema no se disimula: sigue saliendo como excepción.
        Task<bool> AgregarSiRutDisponibleAsync(Cliente cliente, CancellationToken cancellationToken = default);


    }
}
