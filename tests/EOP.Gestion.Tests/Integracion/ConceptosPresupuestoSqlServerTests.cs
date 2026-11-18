using System.Net;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Data.SqlClient;
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
            claveEsperada = "FK_ConceptosPresupuesto_MaterialProveedores_ReferenciaMaterial";
        }
        else
        {
            concepto = CrearConceptoMaterial();
            concepto.MaterialProveedorId = 999_999;
            claveEsperada = "FK_ConceptosPresupuesto_MaterialProveedores_ReferenciaMaterial";
        }

        await ComprobarQueLaBaseRechazaAsync(concepto, claveEsperada);
    }

    [FactConSqlServer]
    public async Task NoSePuedeGuardarLaReferenciaDeOtroMaterialAunqueAmbosIdsExistan()
    {
        ConceptoPresupuesto concepto = CrearConceptoMaterial();
        concepto.MaterialProveedorId = _otroMaterialProveedorId;

        await ComprobarQueLaBaseRechazaAsync(
            concepto, "FK_ConceptosPresupuesto_MaterialProveedores_ReferenciaMaterial");
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
    public async Task UnConceptoQueNoEsMargen_NoPuedeTenerBaseDeCalculo()
    {
        ConceptoPresupuesto concepto = CrearConceptoManual();
        await GuardarAsync(concepto);

        using EopDbContext contexto = CrearContexto();
        contexto.MargenesBaseTipo.Add(new MargenBaseTipo
        {
            ConceptoPresupuestoId = concepto.Id,
            TipoConceptoId = TipoConcepto.MaterialId
        });

        string error = await GuardarYObtenerMensajeDeErrorAsync(contexto);
        Assert.Contains("FK_MargenesBaseTipo_ConceptosPresupuesto_DuenoMargen", error);
        Assert.False(await contexto.MargenesBaseTipo.AnyAsync());
    }

    [FactConSqlServer]
    public async Task NoSePuedeUsarElTipoDeOtroConceptoParaAsignarleUnaBaseDeMargen()
    {
        ConceptoPresupuesto concepto = CrearConceptoManual();
        await GuardarAsync(concepto);

        using EopDbContext contexto = CrearContexto();
        var baseMargen = new MargenBaseTipo
        {
            ConceptoPresupuestoId = concepto.Id,
            TipoConceptoId = TipoConcepto.MaterialId
        };
        contexto.MargenesBaseTipo.Add(baseMargen)
            .Property<int>("TipoConceptoDuenoId").CurrentValue = TipoConcepto.ManoDeObraId;

        string error = await GuardarYObtenerMensajeDeErrorAsync(contexto);
        Assert.Contains("CK_MargenesBaseTipo_DuenoEsMargen", error);
        Assert.False(await contexto.MargenesBaseTipo.AnyAsync());
    }

    [FactConSqlServer]
    public async Task NoSePuedeCambiarElTipoDeUnMargenConBaseDeCalculo()
    {
        ConceptoPresupuesto margen = CrearConceptoMargen();
        await GuardarAsync(margen);

        using EopDbContext contexto = CrearContexto();
        contexto.MargenesBaseTipo.Add(new MargenBaseTipo
        {
            ConceptoPresupuestoId = margen.Id,
            TipoConceptoId = TipoConcepto.MaterialId
        });
        await contexto.SaveChangesAsync();

        // Ejecuta el UPDATE directo para comprobar la FK de SQL Server incluso si se omite el seguimiento de EF.
        SqlException excepcion = await Assert.ThrowsAsync<SqlException>(() => contexto.ConceptosPresupuesto
            .Where(concepto => concepto.Id == margen.Id)
            .ExecuteUpdateAsync(cambios => cambios
                .SetProperty(concepto => concepto.TipoConceptoId, TipoConcepto.ManoDeObraId)
                .SetProperty(concepto => concepto.PorcentajeMargen, (decimal?)null)));

        Assert.Equal(547, excepcion.Number);
        Assert.Contains("FK_MargenesBaseTipo_ConceptosPresupuesto_DuenoMargen", excepcion.Message);
        Assert.Equal(TipoConcepto.MargenId, await contexto.ConceptosPresupuesto.Select(concepto => concepto.TipoConceptoId).SingleAsync());
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

        Assert.Contains("FK_ConceptosPresupuesto_MaterialProveedores_ReferenciaMaterial", excepcion.GetBaseException().Message);
    }

    // S3-T06: además de verificar el esquema, recorremos el guardado desde el formulario real.
    [TheoryConSqlServer]
    [InlineData(TipoConcepto.ManoDeObraId, Moneda.USD, EstadoPresupuesto.Borrador, RolUsuario.Operador)]
    [InlineData(TipoConcepto.AlquilerId, Moneda.UYU, EstadoPresupuesto.ParaRevisionInterna, RolUsuario.Supervisor)]
    [InlineData(TipoConcepto.TrasladoId, Moneda.UYU, EstadoPresupuesto.SolicitudDeModificacionDelCliente, RolUsuario.Administrador)]
    public async Task AltaManual_ImporteDirectoConservaValorYMoneda(
        int tipo, Moneda moneda, EstadoPresupuesto estado, RolUsuario rol)
    {
        using EopDbContext contexto = CrearContexto();
        await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(p => p.Estado, estado));
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionParaConceptosAsync(fabrica, rol);
        string ruta = $"/ConceptosPresupuesto/Index/{_presupuestoId}?tipoId={tipo}";
        string pagina = await (await navegador.GetAsync(ruta)).Content.ReadAsStringAsync();
        Assert.Contains("Entrada.Importe", pagina);
        Assert.DoesNotContain("Entrada.Cantidad", pagina);
        Assert.DoesNotContain("Entrada.ReferenciaId", pagina);

        var formulario = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync(ruta),
            ["Entrada.TipoId"] = tipo.ToString(),
            ["Entrada.Descripcion"] = " Instalación ",
            ["Entrada.Importe"] = "123,4567890123",
            ["Entrada.Moneda"] = moneda.ToString()
        };
        var respuesta = await navegador.PostAsync($"/ConceptosPresupuesto/Agregar/{_presupuestoId}", formulario);
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        var concepto = await contexto.ConceptosPresupuesto.SingleAsync();
        Assert.Equal(tipo, concepto.TipoConceptoId);
        Assert.Equal("Instalación", concepto.Concepto);
        Assert.Null(concepto.ConceptoDetectado);
        Assert.Null(concepto.Unidad);
        Assert.Null(concepto.Cantidad);
        Assert.Equal(123.4567890123m, concepto.PrecioUnitario);
        Assert.Equal(moneda, concepto.Moneda);
        Assert.Equal(estado, await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
            .Select(p => p.Estado).SingleAsync());
    }

    [FactConSqlServer]
    public async Task AltaManual_MaterialConservaLaReferenciaYRechazaLaCombinacionRepetida()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionParaConceptosAsync(fabrica, RolUsuario.Operador);
        string ruta = $"/ConceptosPresupuesto/Index/{_presupuestoId}?tipoId=1&busqueda=cable";
        string pagina = await (await navegador.GetAsync(ruta)).Content.ReadAsStringAsync();
        Assert.Contains("Cable de cobre", pagina);
        Assert.DoesNotContain("Pintura blanca", pagina);
        Assert.Contains("Seleccioná una referencia", WebUtility.HtmlDecode(pagina));

        var formulario = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync(ruta),
            ["Entrada.TipoId"] = TipoConcepto.MaterialId.ToString(),
            ["Entrada.ReferenciaId"] = _materialProveedorId.ToString(),
            ["Entrada.Cantidad"] = "2.5",
            ["Entrada.Descripcion"] = "Nombre manipulado",
            ["Entrada.Importe"] = "1",
            ["Entrada.Moneda"] = "USD"
        };
        string destino = $"/ConceptosPresupuesto/Agregar/{_presupuestoId}?busqueda=cable";
        Assert.Equal(HttpStatusCode.Redirect, (await navegador.PostAsync(destino, formulario)).StatusCode);

        using EopDbContext contexto = CrearContexto();
        var material = await contexto.Materiales.SingleAsync(m => m.Id == _materialId);
        var referencia = await contexto.MaterialProveedores.SingleAsync(r => r.Id == _materialProveedorId);
        material.Nombre = "Material actualizado";
        material.Unidad = UnidadMedida.Centimetro;
        referencia.PrecioUnitario = 250m;
        referencia.Moneda = Moneda.USD;
        await contexto.SaveChangesAsync();

        var repetido = await navegador.PostAsync(destino, formulario);
        Assert.Equal(HttpStatusCode.OK, repetido.StatusCode);
        Assert.Contains("ya están en el presupuesto", WebUtility.HtmlDecode(await repetido.Content.ReadAsStringAsync()));
        var concepto = await contexto.ConceptosPresupuesto.SingleAsync();
        Assert.Equal("Cable de cobre", concepto.Concepto);
        Assert.Equal(_materialProveedorId, concepto.MaterialProveedorId);
        Assert.Equal(_materialId, concepto.MaterialId);
        Assert.Null(concepto.ConceptoDetectado);
        Assert.Equal(UnidadMedida.Metro, concepto.Unidad);
        Assert.Equal(Moneda.UYU, concepto.Moneda);
        Assert.Equal(100m, concepto.PrecioUnitario);
        Assert.Equal(2.5m, concepto.Cantidad);
        string tabla = await (await navegador.GetAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}")).Content.ReadAsStringAsync();
        Assert.Contains("Cable de cobre", tabla);
        Assert.Contains("UYU 100", tabla);
        Assert.DoesNotContain("Material actualizado", tabla);
    }

    [TheoryConSqlServer]
    [InlineData(EstadoPresupuesto.Borrador, false, RolUsuario.Operador)]
    [InlineData(EstadoPresupuesto.Aprobado, true, RolUsuario.Administrador)]
    public async Task AltaManual_SinPermisoOcultaElFormularioYRechazaElPost(
        EstadoPresupuesto estado, bool responsable, RolUsuario rol)
    {
        using EopDbContext contexto = CrearContexto();
        await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(p => p.Estado, estado));
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionParaConceptosAsync(fabrica, rol, responsable);
        string ruta = $"/ConceptosPresupuesto/Index/{_presupuestoId}?tipoId=2";
        string pagina = await (await navegador.GetAsync(ruta)).Content.ReadAsStringAsync();
        Assert.DoesNotContain("Agregar concepto", pagina);
        var respuesta = await navegador.PostAsync($"/ConceptosPresupuesto/Agregar/{_presupuestoId}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync(ruta),
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Importe"] = "100",
            ["Entrada.Moneda"] = "UYU"
        });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());
        Assert.Empty(await contexto.ConceptosPresupuesto.ToListAsync());
    }

    private async Task<FabricaConEntraSimulado.Navegador> IniciarSesionParaConceptosAsync(
        FabricaConEntraSimulado fabrica, RolUsuario rol, bool responsable = true)
    {
        using EopDbContext contexto = CrearContexto();
        string objectId = Guid.NewGuid().ToString();
        string correo = "conceptos@eop.com.uy";
        int usuarioId = await fabrica.GuardarUsuarioAsync(correo, objectId, rol);
        if (responsable)
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(p => p.ResponsableId, usuarioId));
        }

        var navegador = fabrica.CrearNavegador();
        await navegador.IniciarSesionAsync(new Dictionary<string, object>
        {
            ["sub"] = objectId,
            ["oid"] = objectId,
            ["name"] = "Usuario de prueba",
            ["preferred_username"] = correo
        });
        Assert.True(navegador.TieneSesion);
        return navegador;
    }

    [TheoryConSqlServer]
    [InlineData(TipoConcepto.MargenId, "10", "UYU", "Seleccioná un tipo de concepto disponible.")]
    [InlineData(TipoConcepto.ManoDeObraId, "3,12345678901", "USD", "Ingresá un importe válido")]
    [InlineData(TipoConcepto.AlquilerId, "10", "", "Seleccioná USD o UYU.")]
    public async Task AltaManual_DatosInvalidosNoGuardanLaFila(int tipo, string importe, string moneda, string error)
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionParaConceptosAsync(fabrica, RolUsuario.Operador);
        string ruta = $"/ConceptosPresupuesto/Index/{_presupuestoId}";
        var respuesta = await navegador.PostAsync($"/ConceptosPresupuesto/Agregar/{_presupuestoId}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync(ruta),
            ["Entrada.TipoId"] = tipo.ToString(),
            ["Entrada.Importe"] = importe,
            ["Entrada.Moneda"] = moneda
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(error, WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        using EopDbContext contexto = CrearContexto();
        Assert.False(await contexto.ConceptosPresupuesto.AnyAsync());
    }

    [FactConSqlServer]
    public async Task AltaManual_DosSolicitudesParaLaMismaReferenciaSoloGuardanUnaFila()
    {
        using EopDbContext contexto = CrearContexto();
        var responsable = await contexto.Usuarios.SingleAsync();

        async Task<string?> AgregarMaterialAsync()
        {
            using EopDbContext otroContexto = CrearContexto();
            var casoDeUso = new AgregarConceptoManual(new RepositorioConceptosPresupuesto(otroContexto));
            return await casoDeUso.EjecutarAsync(_presupuestoId, TipoConcepto.MaterialId, null, null, null,
                _materialProveedorId, 2m, responsable, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(AgregarMaterialAsync(), AgregarMaterialAsync());

        Assert.Single(resultados, resultado => resultado is null);
        Assert.Contains(resultados, resultado => resultado?.Contains("ya están en el presupuesto") == true);
        Assert.Equal(1, await contexto.ConceptosPresupuesto.CountAsync());
    }
}
