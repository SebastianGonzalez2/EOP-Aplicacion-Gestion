using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class PresupuestoConfiguracion : IEntityTypeConfiguration<Presupuesto>
{
    public void Configure(EntityTypeBuilder<Presupuesto> builder)
    {
        builder.ToTable("Presupuestos", tabla =>
        {
            // Solo se permiten los ocho estados definidos en EstadoPresupuesto (RF-02).
            tabla.HasCheckConstraint(
                "CK_Presupuestos_EstadoValido",
                "[Estado] IN (1, 2, 3, 4, 5, 6, 7, 8)");

            // El correlativo del código tiene cuatro cifras: de 1 a 9999.
            tabla.HasCheckConstraint(
                "CK_Presupuestos_CorrelativoDeCuatroCifras",
                $"[NumeroCorrelativo] BETWEEN {GeneradorCodigoPresupuesto.CorrelativoMinimo} AND {GeneradorCodigoPresupuesto.CorrelativoMaximo}");

            // Un plazo de cero o negativo no tiene sentido: la vigencia es de al menos un día.
            tabla.HasCheckConstraint("CK_Presupuestos_PlazoVigenciaPositivo", "[PlazoVigenciaDias] > 0");

            // IsRequired solo prohíbe NULL, así que un texto vacío entraría igual.
            tabla.HasCheckConstraint("CK_Presupuestos_NombreProyectoNoVacio", "TRIM([NombreProyecto]) <> ''");
            tabla.HasCheckConstraint("CK_Presupuestos_CodigoNoVacio", "TRIM([Codigo]) <> ''");
        });

        builder.HasKey(p => p.Id);

        // Largo del código: "PRES-" (5) + fecha (8) + "-" + nombre (hasta 200) + "-" + correlativo (4) = 219.
        // Dejamos un poco de margen.
        builder.Property(p => p.Codigo)
            .IsRequired()
            .HasMaxLength(250);

        // El código no puede repetirse. Como lleva el correlativo, la base lo garantiza dos veces:
        // acá y en el índice del correlativo.
        builder.HasIndex(p => p.Codigo)
            .IsUnique();

        builder.Property(p => p.NumeroCorrelativo)
            .IsRequired();

        // El correlativo es global: no se reinicia por fecha, cliente ni tipo de proyecto (RF-01).
        // Por eso el índice único es sobre este campo solo.
        builder.HasIndex(p => p.NumeroCorrelativo)
            .IsUnique();

        // Mismo largo que el nombre de Cliente, Proveedor y Material.
        builder.Property(p => p.NombreProyecto)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.PlazoVigenciaDias)
            .IsRequired();

        // Guardamos el estado como número entero. Si no se informa, queda en Borrador
        // por el valor inicial de la propiedad en la entidad.
        builder.Property(p => p.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.FechaCreacion)
            .IsRequired();

        // Las relaciones son Restrict: no se puede borrar un cliente, un usuario ni un tipo de
        // proyecto que tenga presupuestos. (Clientes y usuarios de todas formas se inactivan.)
        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TipoProyecto>()
            .WithMany()
            .HasForeignKey(p => p.TipoProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(p => p.ResponsableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
