using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S3-T05 (US-011): los conceptos del presupuesto contra un SQL Server real. Las reglas que más
// importan están en la base (índices únicos filtrados y restricciones CHECK), y los filtros de
// esos índices son específicos de SQL Server. Por eso las pruebas guardan directo con el contexto,
// a propósito: lo que se verifica es la defensa de la base si algún camino se saltea las
// validaciones del servidor, y que las migraciones dejan las tablas y los tipos fijos como se espera.
public class ConceptosPresupuestoSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    private int _presupuestoId;
    private int _otroPresupuestoId;
    private int _materialId;
    private int _otroMaterialId;
    private int _materialProveedorId;
    private int _otroMaterialProveedorId;

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();

        var cliente = new Cliente
        {
            Nombre = "Cliente de prueba",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456"
        };
        var responsable = new Usuario
        {
            NombreCompleto = "Ana Pereira",
            CorreoCorporativo = "ana@eop.com.uy",
            Rol = RolUsuario.Operador,
            Activo = true
        };
        var tipoProyecto = new TipoProyecto { Nombre = "Tipo de prueba de conceptos" };

        contexto.AddRange(cliente, responsable, tipoProyecto);
        await contexto.SaveChangesAsync();

        var presupuesto = new Presupuesto
        {
            Codigo = "PRES-20261118-OBRA-0001",
            NumeroCorrelativo = 1,
            NombreProyecto = "Obra",
            ClienteId = cliente.Id,
            TipoProyectoId = tipoProyecto.Id,
            PlazoVigenciaDias = 30,
            ResponsableId = responsable.Id,
            FechaCreacion = new DateTime(2026, 11, 18, 12, 0, 0)
        };
        var otroPresupuesto = new Presupuesto
        {
            Codigo = "PRES-20261118-OTRA-0002",
            NumeroCorrelativo = 2,
            NombreProyecto = "Otra obra",
            ClienteId = cliente.Id,
            TipoProyectoId = tipoProyecto.Id,
            PlazoVigenciaDias = 30,
            ResponsableId = responsable.Id,
            FechaCreacion = new DateTime(2026, 11, 18, 12, 0, 0)
        };
        contexto.Presupuestos.AddRange(presupuesto, otroPresupuesto);

        var proveedor = DatosDePruebaProveedores.CrearProveedor();
        var material = new Material { Nombre = "Cable de cobre", Unidad = UnidadMedida.Metro };
        var otroMaterial = new Material { Nombre = "Pintura blanca", Unidad = UnidadMedida.Litro };
        contexto.Proveedores.Add(proveedor);
        contexto.Materiales.AddRange(material, otroMaterial);
        await contexto.SaveChangesAsync();

        var relacion = new MaterialProveedor
        {
            MaterialId = material.Id,
            ProveedorId = proveedor.Id,
            PrecioUnitario = 100m,
            Moneda = Moneda.UYU,
            FechaUltimaActualizacion = new DateTime(2026, 11, 1, 10, 0, 0)
        };
        var otraRelacion = new MaterialProveedor
        {
            MaterialId = otroMaterial.Id,
            ProveedorId = proveedor.Id,
            PrecioUnitario = 7.5m,
            Moneda = Moneda.USD,
            FechaUltimaActualizacion = new DateTime(2026, 11, 2, 11, 0, 0)
        };
        contexto.MaterialProveedores.AddRange(relacion, otraRelacion);
        await contexto.SaveChangesAsync();

        _presupuestoId = presupuesto.Id;
        _otroPresupuestoId = otroPresupuesto.Id;
        _materialId = material.Id;
        _otroMaterialId = otroMaterial.Id;
        _materialProveedorId = relacion.Id;
        _otroMaterialProveedorId = otraRelacion.Id;
    }

    public async Task DisposeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    private EopDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EopDbContext>().UseSqlServer(_cadenaConexion).Options);

    private ConceptoPresupuesto CrearConceptoMaterial(int? presupuestoId = null) => new()
    {
        PresupuestoId = presupuestoId ?? _presupuestoId,
        TipoConceptoId = TipoConcepto.MaterialId,
        Concepto = "Cable de cobre",
        MaterialId = _materialId,
        MaterialProveedorId = _materialProveedorId,
        Cantidad = 10m,
        Unidad = UnidadMedida.Metro,
        PrecioUnitario = 100m,
        Moneda = Moneda.UYU
    };

    private ConceptoPresupuesto CrearConceptoManual(int? presupuestoId = null) => new()
    {
        PresupuestoId = presupuestoId ?? _presupuestoId,
        TipoConceptoId = TipoConcepto.ManoDeObraId,
        Concepto = "Mano de obra",
        PrecioUnitario = 1500m,
        Moneda = Moneda.UYU
    };

    private ConceptoPresupuesto CrearConceptoMargen(int? presupuestoId = null, decimal porcentaje = 20m) => new()
    {
        PresupuestoId = presupuestoId ?? _presupuestoId,
        TipoConceptoId = TipoConcepto.MargenId,
        Concepto = "Margen",
        PorcentajeMargen = porcentaje
    };

    private async Task GuardarAsync(ConceptoPresupuesto concepto)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.ConceptosPresupuesto.Add(concepto);
        await contexto.SaveChangesAsync();
    }

    private async Task<int> ContarConceptosAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.ConceptosPresupuesto.CountAsync();
    }

    // Mira el motivo además del rechazo: sin eso, la prueba podría pasar porque falló otra restricción.
    private async Task ComprobarQueLaBaseRechazaAsync(ConceptoPresupuesto concepto, string motivoEsperado)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.ConceptosPresupuesto.Add(concepto);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains(motivoEsperado, excepcion.GetBaseException().Message);
        Assert.Equal(0, await ContarConceptosAsync());
    }

    private async Task<string> GuardarYObtenerMensajeDeErrorAsync(EopDbContext contexto)
    {
        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
        return excepcion.GetBaseException().Message;
    }

    [FactConSqlServer]
    public async Task ConceptoDeMaterial_SeGuardaConSusCuatroDatosHistoricosYSuCantidad()
    {
        ConceptoPresupuesto concepto = CrearConceptoMaterial();
        concepto.Cantidad = 1.2345678901m;
        concepto.PrecioUnitario = 0.1234567891m;
        concepto.Moneda = Moneda.USD;

        await GuardarAsync(concepto);

        using EopDbContext contexto = CrearContexto();
        ConceptoPresupuesto leido = await contexto.ConceptosPresupuesto.SingleAsync();

        Assert.Equal(_presupuestoId, leido.PresupuestoId);
        Assert.Equal(TipoConcepto.MaterialId, leido.TipoConceptoId);
        Assert.Equal("Cable de cobre", leido.Concepto);
        Assert.Equal(_materialId, leido.MaterialId);
        Assert.Equal(_materialProveedorId, leido.MaterialProveedorId);
        Assert.Equal(1.2345678901m, leido.Cantidad);
        Assert.Equal(UnidadMedida.Metro, leido.Unidad);
        Assert.Equal(0.1234567891m, leido.PrecioUnitario);
        Assert.Equal(Moneda.USD, leido.Moneda);
        Assert.Null(leido.PorcentajeMargen);
        // La fila se agrega a mano: no hay concepto detectado por el análisis del relevamiento.
        Assert.Null(leido.ConceptoDetectado);
    }

    [FactConSqlServer]
    public async Task ConceptoManual_SeGuardaConSuImporteYMonedaSinCantidadNiUnidad()
    {
        await GuardarAsync(CrearConceptoManual());

        using EopDbContext contexto = CrearContexto();
        ConceptoPresupuesto leido = await contexto.ConceptosPresupuesto.SingleAsync();

        Assert.Equal(TipoConcepto.ManoDeObraId, leido.TipoConceptoId);
        Assert.Equal("Mano de obra", leido.Concepto);
        Assert.Equal(1500m, leido.PrecioUnitario);
        Assert.Equal(Moneda.UYU, leido.Moneda);
        Assert.Null(leido.Cantidad);
        Assert.Null(leido.Unidad);
        Assert.Null(leido.MaterialId);
        Assert.Null(leido.MaterialProveedorId);
        Assert.Null(leido.ConceptoDetectado);
    }

    [FactConSqlServer]
    public async Task VariosConceptosManuales_NoChocanConElIndiceFiltradoDeMateriales()
    {
        ConceptoPresupuesto primero = CrearConceptoManual();
        ConceptoPresupuesto segundo = CrearConceptoManual();
        segundo.TipoConceptoId = TipoConcepto.AlquilerId;
        segundo.Concepto = "Alquiler de andamio";
        segundo.PrecioUnitario = 3000m;

        using (EopDbContext contexto = CrearContexto())
        {
            contexto.ConceptosPresupuesto.AddRange(primero, segundo);
            await contexto.SaveChangesAsync();
        }

        Assert.Equal(2, await ContarConceptosAsync());
    }

    [FactConSqlServer]
    public async Task ElMismoMaterialProveedor_NoSePuedeRepetirDentroDelPresupuesto()
    {
        await GuardarAsync(CrearConceptoMaterial());

        using EopDbContext contexto = CrearContexto();
        ConceptoPresupuesto repetido = CrearConceptoMaterial();
        repetido.Cantidad = 5m;
        contexto.ConceptosPresupuesto.Add(repetido);

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(contexto);

        Assert.Contains("IX_ConceptosPresupuesto_PresupuestoId_MaterialProveedorId", mensaje);
        Assert.Equal(1, await ContarConceptosAsync());
    }

    [FactConSqlServer]
    public async Task DosMaterialesDistintos_SePuedenGuardarEnElMismoPresupuesto()
    {
        await GuardarAsync(CrearConceptoMaterial());

        ConceptoPresupuesto segundo = CrearConceptoMaterial();
        segundo.Concepto = "Pintura blanca";
        segundo.MaterialId = _otroMaterialId;
        segundo.MaterialProveedorId = _otroMaterialProveedorId;
        segundo.Unidad = UnidadMedida.Litro;
        segundo.PrecioUnitario = 7.5m;
        segundo.Moneda = Moneda.USD;
        await GuardarAsync(segundo);

        Assert.Equal(2, await ContarConceptosAsync());
    }

    [FactConSqlServer]
    public async Task ElMismoMaterialProveedor_SePuedeUsarEnOtroPresupuesto()
    {
        await GuardarAsync(CrearConceptoMaterial());
        await GuardarAsync(CrearConceptoMaterial(_otroPresupuestoId));

        Assert.Equal(2, await ContarConceptosAsync());
    }

    [FactConSqlServer]
    public async Task UnPresupuesto_AdmiteUnSoloConceptoMargen()
    {
        await GuardarAsync(CrearConceptoMargen());

        using EopDbContext contexto = CrearContexto();
        contexto.ConceptosPresupuesto.Add(CrearConceptoMargen(null, 10m));

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(contexto);

        Assert.Contains("IX_ConceptosPresupuesto_UnicoMargen", mensaje);
        Assert.Equal(1, await ContarConceptosAsync());
    }

    [FactConSqlServer]
    public async Task CadaPresupuesto_PuedeTenerSuPropioMargen()
    {
        await GuardarAsync(CrearConceptoMargen());
        await GuardarAsync(CrearConceptoMargen(_otroPresupuestoId));

        Assert.Equal(2, await ContarConceptosAsync());
    }

    [FactConSqlServer]
    public async Task UnConceptoQueNoEsMargen_NoPuedeLlevarPorcentaje()
    {
        ConceptoPresupuesto concepto = CrearConceptoManual();
        concepto.PorcentajeMargen = 10m;

        await ComprobarQueLaBaseRechazaAsync(concepto, "CK_ConceptosPresupuesto_MargenConPorcentaje");
    }

    [FactConSqlServer]
    public async Task ElConceptoMargen_DebeLlevarPorcentaje()
    {
        ConceptoPresupuesto concepto = CrearConceptoMargen();
        concepto.PorcentajeMargen = null;

        await ComprobarQueLaBaseRechazaAsync(concepto, "CK_ConceptosPresupuesto_MargenConPorcentaje");
    }

    [FactConSqlServer]
    public async Task ElPorcentajeDelMargen_NoPuedeSerNegativo()
    {
        await ComprobarQueLaBaseRechazaAsync(CrearConceptoMargen(null, -1m), "CK_ConceptosPresupuesto_PorcentajeNoNegativo");
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task LaCantidad_DebeSerMayorQueCero(double cantidadInvalida)
    {
        ConceptoPresupuesto concepto = CrearConceptoMaterial();
        concepto.Cantidad = (decimal)cantidadInvalida;

        await ComprobarQueLaBaseRechazaAsync(concepto, "CK_ConceptosPresupuesto_CantidadPositiva");
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(3)]
    public async Task LaMoneda_SoloPuedeSerUyuOUsd(int monedaInvalida)
    {
        ConceptoPresupuesto concepto = CrearConceptoManual();
        concepto.Moneda = (Moneda)monedaInvalida;

        await ComprobarQueLaBaseRechazaAsync(concepto, "CK_ConceptosPresupuesto_MonedaValida");
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(15)]
    public async Task LaUnidad_DebeSerUnaDeLasFijas(int unidadInvalida)
    {
        ConceptoPresupuesto concepto = CrearConceptoMaterial();
        concepto.Unidad = (UnidadMedida)unidadInvalida;

        await ComprobarQueLaBaseRechazaAsync(concepto, "CK_ConceptosPresupuesto_UnidadValida");
    }

    [FactConSqlServer]
    public async Task ElPrecioYLaMoneda_SeGuardanJuntos()
    {
        ConceptoPresupuesto sinMoneda = CrearConceptoManual();
        sinMoneda.Moneda = null;

        await ComprobarQueLaBaseRechazaAsync(sinMoneda, "CK_ConceptosPresupuesto_PrecioYMonedaJuntos");
    }

    [FactConSqlServer]
    public async Task NoSePuedeGuardarUnaMonedaSinPrecio()
    {
        ConceptoPresupuesto sinPrecio = CrearConceptoManual();
        sinPrecio.PrecioUnitario = null;

        await ComprobarQueLaBaseRechazaAsync(sinPrecio, "CK_ConceptosPresupuesto_PrecioYMonedaJuntos");
    }

    [FactConSqlServer]
    public async Task LaReferenciaDeMaterial_SeGuardaCompletaONoSeGuarda()
    {
        ConceptoPresupuesto soloMaterial = CrearConceptoMaterial();
        soloMaterial.MaterialProveedorId = null;
        soloMaterial.Cantidad = null;
        soloMaterial.Unidad = null;
        soloMaterial.PrecioUnitario = null;
        soloMaterial.Moneda = null;

        await ComprobarQueLaBaseRechazaAsync(soloMaterial, "CK_ConceptosPresupuesto_ReferenciaCompleta");
    }

    [FactConSqlServer]
    public async Task NoSePuedeGuardarSoloLaReferenciaDeMaterialProveedor()
    {
        ConceptoPresupuesto soloRelacion = CrearConceptoMaterial();
        soloRelacion.MaterialId = null;
        soloRelacion.Cantidad = null;
        soloRelacion.Unidad = null;
        soloRelacion.PrecioUnitario = null;
        soloRelacion.Moneda = null;

        await ComprobarQueLaBaseRechazaAsync(soloRelacion, "CK_ConceptosPresupuesto_ReferenciaCompleta");
    }

    [TheoryConSqlServer]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ElConcepto_NoPuedeSerSoloEspacios(string conceptoEnBlanco)
    {
        ConceptoPresupuesto concepto = CrearConceptoManual();
        concepto.Concepto = conceptoEnBlanco;

        await ComprobarQueLaBaseRechazaAsync(concepto, "CK_ConceptosPresupuesto_ConceptoNoVacio");
    }

    [TheoryConSqlServer]
    [InlineData("presupuesto")]
    [InlineData("tipo")]
    [InlineData("material")]
    [InlineData("relacion")]
    public async Task UnaReferenciaInexistente_LoRechazaLaClaveForanea(string referencia)
    {
        ConceptoPresupuesto concepto;
        string claveEsperada;

        if (referencia == "presupuesto")
        {
            concepto = CrearConceptoManual(999_999);
            claveEsperada = "FK_ConceptosPresupuesto_Presupuestos_PresupuestoId";
        }
        else if (referencia == "tipo")
        {
            concepto = CrearConceptoManual();
            concepto.TipoConceptoId = 999_999;
            claveEsperada = "FK_ConceptosPresupuesto_TiposConcepto_TipoConceptoId";
        }
        else if (referencia == "material")
        {
            concepto = CrearConceptoMaterial();
            concepto.MaterialId = 999_999;
            claveEsperada = "FK_ConceptosPresupuesto_Materiales_MaterialId";
        }
        else
        {
            concepto = CrearConceptoMaterial();
            concepto.MaterialProveedorId = 999_999;
            claveEsperada = "FK_ConceptosPresupuesto_MaterialProveedores_MaterialProveedorId";
        }

        await ComprobarQueLaBaseRechazaAsync(concepto, claveEsperada);
    }

    [FactConSqlServer]
    public async Task LasMigraciones_PrecarganLosSeisTiposDeConcepto()
    {
        using EopDbContext contexto = CrearContexto();

        Dictionary<int, string> tipos = await contexto.TiposConcepto.ToDictionaryAsync(tipo => tipo.Id, tipo => tipo.Nombre);

        Assert.Equal(
            new Dictionary<int, string>
            {
                [TipoConcepto.MaterialId] = "Material",
                [TipoConcepto.ManoDeObraId] = "Mano de obra",
                [TipoConcepto.AlquilerId] = "Alquiler",
                [TipoConcepto.TrasladoId] = "Traslado",
                [TipoConcepto.CostosIndirectosId] = "Costos indirectos",
                [TipoConcepto.MargenId] = "Margen"
            },
            tipos);
    }

    [FactConSqlServer]
    public async Task ElMargen_GuardaLosTiposElegidosComoBaseDeCalculo()
    {
        ConceptoPresupuesto margen = CrearConceptoMargen();
        await GuardarAsync(margen);

        using (EopDbContext contexto = CrearContexto())
        {
            contexto.MargenesBaseTipo.AddRange(
                new MargenBaseTipo { ConceptoPresupuestoId = margen.Id, TipoConceptoId = TipoConcepto.MaterialId },
                new MargenBaseTipo { ConceptoPresupuestoId = margen.Id, TipoConceptoId = TipoConcepto.AlquilerId });
            await contexto.SaveChangesAsync();
        }

        using EopDbContext lectura = CrearContexto();
        List<int> bases = await lectura.MargenesBaseTipo
            .Where(baseDeMargen => baseDeMargen.ConceptoPresupuestoId == margen.Id)
            .Select(baseDeMargen => baseDeMargen.TipoConceptoId)
            .OrderBy(tipo => tipo)
            .ToListAsync();

        Assert.Equal([TipoConcepto.MaterialId, TipoConcepto.AlquilerId], bases);
    }

    [FactConSqlServer]
    public async Task ElMismoTipo_NoSePuedeRepetirEnLaBaseDelMargen()
    {
        ConceptoPresupuesto margen = CrearConceptoMargen();
        await GuardarAsync(margen);

        using (EopDbContext contexto = CrearContexto())
        {
            contexto.MargenesBaseTipo.Add(new MargenBaseTipo
            {
                ConceptoPresupuestoId = margen.Id,
                TipoConceptoId = TipoConcepto.MaterialId
            });
            await contexto.SaveChangesAsync();
        }

        using EopDbContext otroContexto = CrearContexto();
        otroContexto.MargenesBaseTipo.Add(new MargenBaseTipo
        {
            ConceptoPresupuestoId = margen.Id,
            TipoConceptoId = TipoConcepto.MaterialId
        });

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(otroContexto);

        Assert.Contains("PK_MargenesBaseTipo", mensaje);
    }

    [FactConSqlServer]
    public async Task ElConceptoMargen_NoPuedeSerBaseDeSiMismo()
    {
        ConceptoPresupuesto margen = CrearConceptoMargen();
        await GuardarAsync(margen);

        using EopDbContext contexto = CrearContexto();
        contexto.MargenesBaseTipo.Add(new MargenBaseTipo
        {
            ConceptoPresupuestoId = margen.Id,
            TipoConceptoId = TipoConcepto.MargenId
        });

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(contexto);

        Assert.Contains("CK_MargenesBaseTipo_NoIncluyeMargen", mensaje);
    }

    [FactConSqlServer]
    public async Task AlBorrarElConceptoMargen_SeBorranSusTiposBase()
    {
        ConceptoPresupuesto margen = CrearConceptoMargen();
        await GuardarAsync(margen);

        using (EopDbContext contexto = CrearContexto())
        {
            contexto.MargenesBaseTipo.Add(new MargenBaseTipo
            {
                ConceptoPresupuestoId = margen.Id,
                TipoConceptoId = TipoConcepto.MaterialId
            });
            await contexto.SaveChangesAsync();
        }

        using (EopDbContext contexto = CrearContexto())
        {
            contexto.ConceptosPresupuesto.Remove(await contexto.ConceptosPresupuesto.SingleAsync());
            await contexto.SaveChangesAsync();
        }

        using EopDbContext lectura = CrearContexto();
        Assert.Equal(0, await lectura.MargenesBaseTipo.CountAsync());
        Assert.Equal(0, await lectura.ConceptosPresupuesto.CountAsync());
    }

    [FactConSqlServer]
    public async Task UnTipoDeConceptoEnUso_NoSePuedeBorrarFisicamente()
    {
        await GuardarAsync(CrearConceptoMaterial());

        using EopDbContext contexto = CrearContexto();
        TipoConcepto tipoMaterial = await contexto.TiposConcepto.SingleAsync(tipo => tipo.Id == TipoConcepto.MaterialId);
        contexto.TiposConcepto.Remove(tipoMaterial);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("FK_ConceptosPresupuesto_TiposConcepto_TipoConceptoId", excepcion.GetBaseException().Message);
    }

    [FactConSqlServer]
    public async Task UnPresupuestoConConceptos_NoSePuedeBorrarFisicamente()
    {
        await GuardarAsync(CrearConceptoMaterial());

        using EopDbContext contexto = CrearContexto();
        Presupuesto presupuesto = await contexto.Presupuestos.SingleAsync(p => p.Id == _presupuestoId);
        contexto.Presupuestos.Remove(presupuesto);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("FK_ConceptosPresupuesto_Presupuestos_PresupuestoId", excepcion.GetBaseException().Message);
    }

    [FactConSqlServer]
    public async Task UnMaterialConConceptos_NoSePuedeBorrarFisicamente()
    {
        await GuardarAsync(CrearConceptoMaterial());

        using EopDbContext contexto = CrearContexto();
        Material material = await contexto.Materiales.SingleAsync(m => m.Id == _materialId);
        contexto.Materiales.Remove(material);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("FK_ConceptosPresupuesto_Materiales_MaterialId", excepcion.GetBaseException().Message);
    }

    [FactConSqlServer]
    public async Task UnaRelacionMaterialProveedorEnUso_NoSePuedeBorrarFisicamente()
    {
        await GuardarAsync(CrearConceptoMaterial());

        using EopDbContext contexto = CrearContexto();
        MaterialProveedor relacion = await contexto.MaterialProveedores.SingleAsync(r => r.Id == _materialProveedorId);
        contexto.MaterialProveedores.Remove(relacion);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("FK_ConceptosPresupuesto_MaterialProveedores_MaterialProveedorId", excepcion.GetBaseException().Message);
    }
}
