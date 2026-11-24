using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T12 (RF-020): el caso de uso exige la confirmación explícita y traduce el resultado del
// repositorio a un mensaje. Las reglas contra la base están en SeleccionarMaterialSqlServerTests.
public class ActualizarReferenciaDeConceptoTests
{
    private readonly RepositorioFalso _repositorio = new();
    private readonly Usuario _usuario = new() { Id = 1 };

    [Fact]
    public async Task SinConfirmacion_SeRechazaSinConsultarAlRepositorio()
    {
        string? error = await EjecutarAsync(confirmado: false);

        Assert.Contains("Confirmá la actualización", error);
        Assert.Equal(0, _repositorio.Llamadas);
    }

    [Fact]
    public async Task Confirmada_SeEnviaAlRepositorioConLosIdsDeLaSolicitud()
    {
        _repositorio.Resultado = ResultadoSeleccionarMaterial.Seleccionado;

        string? error = await EjecutarAsync(confirmado: true);

        Assert.Null(error);
        Assert.Equal((3, 5, _usuario), _repositorio.UltimaSolicitud);
    }

    [Theory]
    [InlineData(ResultadoSeleccionarMaterial.EdicionNoPermitida, "ya no está disponible para edición")]
    [InlineData(ResultadoSeleccionarMaterial.ConceptoInexistente, "ya no existe")]
    [InlineData(ResultadoSeleccionarMaterial.ConceptoNoEsMaterial, "filas de tipo Material")]
    [InlineData(ResultadoSeleccionarMaterial.ConceptoSinMaterial, "todavía no tiene un material")]
    [InlineData(ResultadoSeleccionarMaterial.ReferenciaInexistente, "ya no existe en el catálogo")]
    [InlineData(ResultadoSeleccionarMaterial.SinCambios, "ya coinciden con los del catálogo")]
    public async Task ResultadoRechazado_SeTraduceAUnMensaje(ResultadoSeleccionarMaterial resultado, string fragmento)
    {
        _repositorio.Resultado = resultado;

        string? error = await EjecutarAsync(confirmado: true);

        Assert.Contains(fragmento, error);
    }

    [Theory]
    [InlineData(ResultadoSeleccionarMaterial.ReferenciaDuplicada)]
    [InlineData(ResultadoSeleccionarMaterial.ReferenciaDeOtroMaterial)]
    [InlineData((ResultadoSeleccionarMaterial)999)]
    public async Task ResultadoQueEstaAccionNoDeberiaProducir_NoSeIgnora(ResultadoSeleccionarMaterial resultado)
    {
        _repositorio.Resultado = resultado;

        await Assert.ThrowsAsync<InvalidOperationException>(() => EjecutarAsync(confirmado: true));
    }

    private Task<string?> EjecutarAsync(bool confirmado) =>
        new ActualizarReferenciaDeConcepto(_repositorio)
            .EjecutarAsync(3, 5, confirmado, _usuario, CancellationToken.None);

    private class RepositorioFalso : IRepositorioConceptosPresupuesto
    {
        public ResultadoSeleccionarMaterial Resultado { get; set; }
        public int Llamadas { get; private set; }
        public (int, int, Usuario)? UltimaSolicitud { get; private set; }

        public Task<ResultadoSeleccionarMaterial> ActualizarReferenciaActualSiPuedeEditarAsync(
            int presupuestoId, int conceptoId, Usuario usuario, CancellationToken cancellationToken)
        {
            Llamadas++;
            UltimaSolicitud = (presupuestoId, conceptoId, usuario);
            return Task.FromResult(Resultado);
        }

        public Task<ResultadoSeleccionarMaterial> CambiarProveedorSiPuedeEditarAsync(
            int presupuestoId, int conceptoId, int referenciaId, Usuario usuario,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ResultadoSeleccionarMaterial> SeleccionarMaterialSiPuedeEditarAsync(
            int presupuestoId, int conceptoId, int referenciaId, Usuario usuario,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ResultadoGuardarConcepto> AgregarSiPuedeEditarAsync(
            ConceptoPresupuesto concepto, Usuario usuario, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
