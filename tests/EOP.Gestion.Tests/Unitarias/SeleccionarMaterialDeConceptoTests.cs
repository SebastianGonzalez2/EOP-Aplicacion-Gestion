using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T09 (US-007): el caso de uso solo valida la selección expresa y traduce el resultado
// del repositorio a un mensaje. Las reglas contra la base están en SeleccionarMaterialSqlServerTests.
public class SeleccionarMaterialDeConceptoTests
{
    private readonly RepositorioFalso _repositorio = new();
    private readonly Usuario _usuario = new() { Id = 1 };

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SinReferenciaElegida_SeRechazaSinConsultarAlRepositorio(int? referenciaId)
    {
        string? error = await EjecutarAsync(referenciaId);

        Assert.Equal("Seleccioná una referencia de material y proveedor.", error);
        Assert.Equal(0, _repositorio.Llamadas);
    }

    [Fact]
    public async Task ReferenciaElegida_SeEnviaAlRepositorioConLosIdsDeLaSolicitud()
    {
        _repositorio.Resultado = ResultadoSeleccionarMaterial.Seleccionado;

        string? error = await EjecutarAsync(referenciaId: 7);

        Assert.Null(error);
        Assert.Equal((3, 5, 7, _usuario), _repositorio.UltimaSolicitud);
    }

    [Theory]
    [InlineData(ResultadoSeleccionarMaterial.EdicionNoPermitida, "ya no está disponible para edición")]
    [InlineData(ResultadoSeleccionarMaterial.ConceptoInexistente, "ya no existe")]
    [InlineData(ResultadoSeleccionarMaterial.ConceptoNoEsMaterial, "filas de tipo Material")]
    [InlineData(ResultadoSeleccionarMaterial.ReferenciaInexistente, "no existe")]
    [InlineData(ResultadoSeleccionarMaterial.ReferenciaDuplicada, "ya están en otra fila")]
    [InlineData(ResultadoSeleccionarMaterial.SinCambios, "ya es la seleccionada")]
    public async Task ResultadoRechazado_SeTraduceAUnMensaje(ResultadoSeleccionarMaterial resultado, string fragmento)
    {
        _repositorio.Resultado = resultado;

        string? error = await EjecutarAsync(referenciaId: 7);

        Assert.Contains(fragmento, error);
    }

    [Fact]
    public async Task ResultadoDesconocido_NoSeIgnora()
    {
        _repositorio.Resultado = (ResultadoSeleccionarMaterial)999;

        await Assert.ThrowsAsync<InvalidOperationException>(() => EjecutarAsync(referenciaId: 7));
    }

    private Task<string?> EjecutarAsync(int? referenciaId) =>
        new SeleccionarMaterialDeConcepto(_repositorio)
            .EjecutarAsync(3, 5, referenciaId, _usuario, CancellationToken.None);

    private class RepositorioFalso : IRepositorioConceptosPresupuesto
    {
        public ResultadoSeleccionarMaterial Resultado { get; set; }
        public int Llamadas { get; private set; }
        public (int, int, int, Usuario)? UltimaSolicitud { get; private set; }

        public Task<ResultadoSeleccionarMaterial> SeleccionarMaterialSiPuedeEditarAsync(
            int presupuestoId, int conceptoId, int referenciaId, Usuario usuario,
            CancellationToken cancellationToken)
        {
            Llamadas++;
            UltimaSolicitud = (presupuestoId, conceptoId, referenciaId, usuario);
            return Task.FromResult(Resultado);
        }

        public Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ResultadoGuardarConcepto> AgregarSiPuedeEditarAsync(
            ConceptoPresupuesto concepto, Usuario usuario, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
