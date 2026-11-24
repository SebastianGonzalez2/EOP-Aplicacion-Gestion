using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EOP.Gestion.Tests.Integracion;

// S3-T14 (US-003, RF-005): pruebas integrales contra SQL Server real del guardado explícito de
// borradores, gestión de cambios pendientes, valores históricos (DT-11), catálogo intacto,
// permisos (RF-073), estados editables (RF-002), atomicidad, errores con reintento y concurrencia.
public class GuardarBorradorSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    private int _presupuestoId;
    private int _responsableOriginalId;
    private int _cableId;
    private int _cableProveedorAId;
    private int _cableProveedorBId;
    private int _pinturaProveedorAId;

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
            Nombre = "Cliente Borrador S.A.",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456"
        };
        var responsable = new Usuario
        {
            NombreCompleto = "Operador Inicial",
            CorreoCorporativo = "inicial@eop.com.uy",
            Rol = RolUsuario.Operador,
            Activo = true
        };
        var tipoProyecto = new TipoProyecto { Nombre = "Iluminación" };
        contexto.AddRange(cliente, responsable, tipoProyecto);
        await contexto.SaveChangesAsync();

        var presupuesto = new Presupuesto
        {
            Codigo = "PRES-20261124-BORRADOR-0001",
            NumeroCorrelativo = 1,
            NombreProyecto = "Proyecto Borrador",
            ClienteId = cliente.Id,
            TipoProyectoId = tipoProyecto.Id,
            PlazoVigenciaDias = 30,
            ResponsableId = responsable.Id,
            Estado = EstadoPresupuesto.Borrador,
            FechaCreacion = new DateTime(2026, 11, 24, 12, 0, 0)
        };
        var proveedorA = DatosDePruebaProveedores.CrearProveedor("Proveedor A", "215521750017");
        var proveedorB = DatosDePruebaProveedores.CrearProveedor("Proveedor B", "210000000019");
        var cable = new Material { Nombre = "Cable de cobre", Unidad = UnidadMedida.Metro };
        var pintura = new Material { Nombre = "Pintura blanca", Unidad = UnidadMedida.Litro };
        contexto.AddRange(presupuesto, proveedorA, proveedorB, cable, pintura);
        await contexto.SaveChangesAsync();

        var cableA = new MaterialProveedor
        {
            MaterialId = cable.Id,
            ProveedorId = proveedorA.Id,
            PrecioUnitario = 100m,
            Moneda = Moneda.UYU,
            FechaUltimaActualizacion = new DateTime(2026, 11, 1, 10, 0, 0)
        };
        var cableB = new MaterialProveedor
        {
            MaterialId = cable.Id,
            ProveedorId = proveedorB.Id,
            PrecioUnitario = 2.5m,
            Moneda = Moneda.USD,
            FechaUltimaActualizacion = new DateTime(2026, 11, 2, 10, 0, 0)
        };
        var pinturaA = new MaterialProveedor
        {
            MaterialId = pintura.Id,
            ProveedorId = proveedorA.Id,
            PrecioUnitario = 7.5m,
            Moneda = Moneda.USD,
            FechaUltimaActualizacion = new DateTime(2026, 11, 3, 10, 0, 0)
        };
        contexto.MaterialProveedores.AddRange(cableA, cableB, pinturaA);
        await contexto.SaveChangesAsync();

        _presupuestoId = presupuesto.Id;
        _responsableOriginalId = responsable.Id;
        _cableId = cable.Id;
        _cableProveedorAId = cableA.Id;
        _cableProveedorBId = cableB.Id;
        _pinturaProveedorAId = pinturaA.Id;
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

    private async Task<FabricaConEntraSimulado.Navegador> IniciarSesionAsync(
        FabricaConEntraSimulado fabrica, RolUsuario rol, bool responsable = true, string? correo = null)
    {
        using EopDbContext contexto = CrearContexto();
        string objectId = Guid.NewGuid().ToString();
        string correoFinal = correo ?? $"{Guid.NewGuid():N}@eop.com.uy";
        int usuarioId = await fabrica.GuardarUsuarioAsync(correoFinal, objectId, rol);
        if (responsable)
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.ResponsableId, usuarioId));
        }

        var navegador = fabrica.CrearNavegador();
        await navegador.IniciarSesionAsync(new Dictionary<string, object>
        {
            ["sub"] = objectId,
            ["oid"] = objectId,
            ["name"] = "Usuario de prueba",
            ["preferred_username"] = correoFinal
        });
        Assert.True(navegador.TieneSesion);
        return navegador;
    }

    private async Task<HttpResponseMessage> AgregarConceptoAsync(
        FabricaConEntraSimulado.Navegador navegador, Dictionary<string, string> campos)
    {
        string ruta = $"/ConceptosPresupuesto/Index/{_presupuestoId}";
        var formulario = new Dictionary<string, string>(campos)
        {
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync(ruta)
        };
        return await navegador.PostAsync($"/ConceptosPresupuesto/Agregar/{_presupuestoId}", formulario);
    }

    private async Task<HttpResponseMessage> GuardarBorradorAsync(
        FabricaConEntraSimulado.Navegador navegador, bool esperabaCambiosPendientes = true)
    {
        string ruta = $"/ConceptosPresupuesto/Index/{_presupuestoId}";
        var formulario = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync(ruta),
            ["esperabaCambiosPendientes"] = esperabaCambiosPendientes ? "true" : "false"
        };
        return await navegador.PostAsync($"/ConceptosPresupuesto/GuardarBorrador/{_presupuestoId}", formulario);
    }

    private async Task<string> LeerPaginaAsync(FabricaConEntraSimulado.Navegador navegador, string ruta) =>
        WebUtility.HtmlDecode(await (await navegador.GetAsync(ruta)).Content.ReadAsStringAsync());

    private async Task<string> FotoDelCatalogoAsync()
    {
        using EopDbContext contexto = CrearContexto();
        var materiales = await contexto.Materiales.AsNoTracking().OrderBy(m => m.Id)
            .Select(m => $"M{m.Id}|{m.Nombre}|{m.Unidad}").ToListAsync();
        var referencias = await contexto.MaterialProveedores.AsNoTracking().OrderBy(r => r.Id)
            .Select(r => $"R{r.Id}|{r.MaterialId}|{r.ProveedorId}|{r.PrecioUnitario}|{r.Moneda}|{r.FechaUltimaActualizacion:O}").ToListAsync();
        var proveedores = await contexto.Proveedores.AsNoTracking().OrderBy(p => p.Id)
            .Select(p => $"P{p.Id}|{p.Nombre}|{p.Rut}").ToListAsync();
        return string.Join("\n", materiales.Concat(referencias).Concat(proveedores));
    }

    [FactConSqlServer]
    public async Task CambiosPendientes_NoSePersistenAntesDeGuardarYNoAparecenAlVolverEnOtraSesion()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var sesionA = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Agregamos material, mano de obra y margen SIN presionar Guardar borrador.
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(sesionA, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MaterialId.ToString(),
            ["Entrada.ReferenciaId"] = _cableProveedorAId.ToString(),
            ["Entrada.Cantidad"] = "5"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(sesionA, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Instalación eléctrica",
            ["Entrada.Importe"] = "2500",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(sesionA, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MargenId.ToString(),
            ["Entrada.PorcentajeMargen"] = "15",
            ["Entrada.TiposBaseSeleccionados"] = TipoConcepto.MaterialId.ToString()
        })).StatusCode);

        // Durante la elaboración se ven los avances y el indicador de cambios pendientes.
        string paginaConPendientes = await LeerPaginaAsync(sesionA, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("data-cambios-pendientes=\"true\"", paginaConPendientes);
        Assert.Contains("Cable de cobre", paginaConPendientes);
        Assert.Contains("Instalación eléctrica", paginaConPendientes);
        Assert.Contains("Margen", paginaConPendientes);

        // Incluso podemos navegar a SeleccionarMaterial para una fila nueva pendiente (Id negativo)
        // y cambiarle el proveedor antes de guardar definitivamente.
        Match enlaceSeleccion = Regex.Match(paginaConPendientes, @"conceptoId=(-\d+)");
        Assert.True(enlaceSeleccion.Success);
        string idTemporal = enlaceSeleccion.Groups[1].Value;

        string paginaSeleccion = await LeerPaginaAsync(
            sesionA, $"/ConceptosPresupuesto/SeleccionarMaterial/{_presupuestoId}?conceptoId={idTemporal}");
        Assert.Contains("Referencias de precios de Cable de cobre", paginaSeleccion);
        Assert.Contains("data-cambios-pendientes=\"true\"", paginaSeleccion);

        var cambioProveedor = await sesionA.PostAsync(
            $"/ConceptosPresupuesto/CambiarProveedor/{_presupuestoId}?conceptoId={idTemporal}",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = await sesionA.ObtenerTokenAntifalsificacionAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}"),
                ["referenciaId"] = _cableProveedorBId.ToString()
            });
        Assert.Equal(HttpStatusCode.Redirect, cambioProveedor.StatusCode);

        // SQL Server sigue completamente vacío: nada se persistió sin presionar Guardar borrador.
        using (EopDbContext contexto = CrearContexto())
        {
            Assert.Empty(await contexto.ConceptosPresupuesto.ToListAsync());
            Assert.Empty(await contexto.MargenesBaseTipo.ToListAsync());
        }

        // Si se vuelve posteriormente en una nueva sesión (abandonando sin guardar), solo se ven los
        // datos efectivamente guardados (ninguno).
        var nuevaSesion = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor, responsable: false);
        string paginaNuevaSesion = await LeerPaginaAsync(nuevaSesion, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("Todavía no hay conceptos registrados.", paginaNuevaSesion);
        Assert.Contains("data-cambios-pendientes=\"false\"", paginaNuevaSesion);
    }

    [FactConSqlServer]
    public async Task GuardarBorrador_PersisteTodoYSeRecuperaAlVolverPosteriormente()
    {
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MaterialId.ToString(),
            ["Entrada.ReferenciaId"] = _cableProveedorAId.ToString(),
            ["Entrada.Cantidad"] = "12,5"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Cableado",
            ["Entrada.Importe"] = "4000",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MargenId.ToString(),
            ["Entrada.PorcentajeMargen"] = "18,5",
            ["Entrada.TiposBaseSeleccionados"] = TipoConcepto.ManoDeObraId.ToString()
        })).StatusCode);

        // Guardamos explícitamente.
        var respuestaGuardar = await GuardarBorradorAsync(navegador);
        Assert.Equal(HttpStatusCode.Redirect, respuestaGuardar.StatusCode);

        string paginaTrasGuardar = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("El borrador se guardó correctamente.", paginaTrasGuardar);
        Assert.Contains("data-cambios-pendientes=\"false\"", paginaTrasGuardar);
        Assert.Contains("Elaboración", paginaTrasGuardar);
        Assert.Contains("Resumen", paginaTrasGuardar);
        Assert.Contains("Documentos", paginaTrasGuardar);
        Assert.Contains("Revisión interna", paginaTrasGuardar);

        // Al regresar en una sesión nueva se recuperan los datos efectivamente guardados.
        var sesionPosterior = await IniciarSesionAsync(fabrica, RolUsuario.Administrador, responsable: false);
        string paginaRecuperada = await LeerPaginaAsync(sesionPosterior, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("Cable de cobre", paginaRecuperada);
        Assert.Contains("Proveedor A", paginaRecuperada);
        Assert.Contains("Cableado", paginaRecuperada);
        Assert.Contains("Margen", paginaRecuperada);

        using EopDbContext contexto = CrearContexto();
        List<ConceptoPresupuesto> conceptos = await contexto.ConceptosPresupuesto
            .Where(c => c.PresupuestoId == _presupuestoId)
            .OrderBy(c => c.TipoConceptoId)
            .ToListAsync();
        Assert.Equal(3, conceptos.Count);

        ConceptoPresupuesto material = conceptos.Single(c => c.TipoConceptoId == TipoConcepto.MaterialId);
        Assert.Equal(_cableId, material.MaterialId);
        Assert.Equal(_cableProveedorAId, material.MaterialProveedorId);
        Assert.Equal("Cable de cobre", material.Concepto);
        Assert.Equal(UnidadMedida.Metro, material.Unidad);
        Assert.Equal(100m, material.PrecioUnitario);
        Assert.Equal(Moneda.UYU, material.Moneda);
        Assert.Equal(12.5m, material.Cantidad);

        ConceptoPresupuesto margen = conceptos.Single(c => c.TipoConceptoId == TipoConcepto.MargenId);
        Assert.Equal(18.5m, margen.PorcentajeMargen);
        List<int> bases = await contexto.MargenesBaseTipo
            .Where(b => b.ConceptoPresupuestoId == margen.Id)
            .Select(b => b.TipoConceptoId)
            .ToListAsync();
        Assert.Equal([TipoConcepto.ManoDeObraId], bases);

        // El catálogo permanece intacto.
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [TheoryConSqlServer]
    [InlineData(EstadoPresupuesto.Borrador, RolUsuario.Operador, true)]
    [InlineData(EstadoPresupuesto.ParaRevisionInterna, RolUsuario.Supervisor, false)]
    [InlineData(EstadoPresupuesto.SolicitudDeModificacionDelCliente, RolUsuario.Administrador, false)]
    public async Task GuardarBorrador_NoCreaOtroPresupuestoNiAlteraCodigoResponsableNiEstado(
        EstadoPresupuesto estado, RolUsuario rol, bool esResponsable)
    {
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.Estado, estado));
        }

        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, rol, responsable: esResponsable);

        Presupuesto antes;
        using (EopDbContext contexto = CrearContexto())
        {
            antes = await contexto.Presupuestos.AsNoTracking().SingleAsync(p => p.Id == _presupuestoId);
        }

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
            ["Entrada.Descripcion"] = "Andamio",
            ["Entrada.Importe"] = "500",
            ["Entrada.Moneda"] = "USD"
        });

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        using EopDbContext verificacion = CrearContexto();
        Assert.Equal(1, await verificacion.Presupuestos.CountAsync());
        Presupuesto despues = await verificacion.Presupuestos.AsNoTracking().SingleAsync(p => p.Id == _presupuestoId);
        Assert.Equal(antes.Codigo, despues.Codigo);
        Assert.Equal(antes.NumeroCorrelativo, despues.NumeroCorrelativo);
        Assert.Equal(antes.ResponsableId, despues.ResponsableId);
        Assert.Equal(estado, despues.Estado);
        Assert.Equal(antes.FechaCreacion, despues.FechaCreacion);
        Assert.Equal(antes.NombreProyecto, despues.NombreProyecto);
        Assert.Equal(antes.ClienteId, despues.ClienteId);
        Assert.Equal(antes.TipoProyectoId, despues.TipoProyectoId);
        Assert.Equal(antes.PlazoVigenciaDias, despues.PlazoVigenciaDias);
    }

    [FactConSqlServer]
    public async Task GuardarBorradorNuevamente_TrasCambiosEnElCatalogo_ConservaValoresHistoricosDeFilasNoModificadas()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Guardamos primero el cable con los valores originales del catálogo.
        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MaterialId.ToString(),
            ["Entrada.ReferenciaId"] = _cableProveedorAId.ToString(),
            ["Entrada.Cantidad"] = "10"
        });
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        int idOriginalFila;
        using (EopDbContext contexto = CrearContexto())
        {
            idOriginalFila = await contexto.ConceptosPresupuesto.Select(c => c.Id).SingleAsync();

            // Modificamos los cuatro valores en el catálogo.
            await contexto.Materiales.Where(m => m.Id == _cableId)
                .ExecuteUpdateAsync(c => c
                    .SetProperty(m => m.Nombre, "Cable modificado en catálogo")
                    .SetProperty(m => m.Unidad, UnidadMedida.Unidad));
            await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorAId)
                .ExecuteUpdateAsync(c => c
                    .SetProperty(r => r.PrecioUnitario, 999m)
                    .SetProperty(r => r.Moneda, Moneda.USD));
        }

        string catalogoDespuesDelCambio = await FotoDelCatalogoAsync();

        // Agregamos otro concepto y volvemos a guardar el borrador.
        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.TrasladoId.ToString(),
            ["Entrada.Descripcion"] = "Flete",
            ["Entrada.Importe"] = "800",
            ["Entrada.Moneda"] = "UYU"
        });
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        using EopDbContext lectura = CrearContexto();
        ConceptoPresupuesto filaHistorica = await lectura.ConceptosPresupuesto
            .SingleAsync(c => c.Id == idOriginalFila);

        Assert.Equal("Cable de cobre", filaHistorica.Concepto);
        Assert.Equal(UnidadMedida.Metro, filaHistorica.Unidad);
        Assert.Equal(100m, filaHistorica.PrecioUnitario);
        Assert.Equal(Moneda.UYU, filaHistorica.Moneda);
        Assert.Equal(catalogoDespuesDelCambio, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task GuardadosRepetidos_NoGeneranDuplicados()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Mano de obra",
            ["Entrada.Importe"] = "1500",
            ["Entrada.Moneda"] = "UYU"
        });

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador, esperabaCambiosPendientes: true)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador, esperabaCambiosPendientes: false)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador, esperabaCambiosPendientes: false)).StatusCode);

        using EopDbContext contexto = CrearContexto();
        Assert.Equal(1, await contexto.ConceptosPresupuesto.CountAsync(c => c.PresupuestoId == _presupuestoId));
    }

    [FactConSqlServer]
    public async Task GuardarBorrador_SinTokenAntifalsificacion_SeRechaza()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await navegador.PostAsync(
            $"/ConceptosPresupuesto/GuardarBorrador/{_presupuestoId}",
            new Dictionary<string, string> { ["esperabaCambiosPendientes"] = "false" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [FactConSqlServer]
    public async Task SiElPresupuestoDejaDeSerEditableAntesDeGuardar_ElServidorRechazaElGuardadoSinPersistir()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Trabajo pendiente",
            ["Entrada.Importe"] = "3000",
            ["Entrada.Moneda"] = "UYU"
        });

        // El presupuesto cambia a Aprobado antes de que el operador presione Guardar borrador.
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.Estado, EstadoPresupuesto.Aprobado));
        }

        var respuesta = await GuardarBorradorAsync(navegador, esperabaCambiosPendientes: true);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());

        using EopDbContext lectura = CrearContexto();
        Assert.Empty(await lectura.ConceptosPresupuesto.ToListAsync());
    }

    [FactConSqlServer]
    public async Task IntercambioDeProveedoresEntreFilasEnElMismoBorrador_SeGuardaSinFalsoDuplicado()
    {
        int fila1Id;
        int fila2Id;
        using (EopDbContext contexto = CrearContexto())
        {
            var cable = await contexto.Materiales.SingleAsync(m => m.Id == _cableId);
            var refA = await contexto.MaterialProveedores.SingleAsync(r => r.Id == _cableProveedorAId);
            var refB = await contexto.MaterialProveedores.SingleAsync(r => r.Id == _cableProveedorBId);

            var fila1 = new ConceptoPresupuesto { PresupuestoId = _presupuestoId, TipoConceptoId = TipoConcepto.MaterialId, Cantidad = 2m };
            fila1.CopiarReferencia(cable, refA);
            var fila2 = new ConceptoPresupuesto { PresupuestoId = _presupuestoId, TipoConceptoId = TipoConcepto.MaterialId, Cantidad = 4m };
            fila2.CopiarReferencia(cable, refB);

            contexto.ConceptosPresupuesto.AddRange(fila1, fila2);
            await contexto.SaveChangesAsync();
            fila1Id = fila1.Id;
            fila2Id = fila2.Id;
        }

        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Fila 1 pasa a Pintura (libera Cable/Proveedor A en el borrador).
        var asignar1 = await navegador.PostAsync(
            $"/ConceptosPresupuesto/AsignarMaterial/{_presupuestoId}?conceptoId={fila1Id}",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}"),
                ["referenciaId"] = _pinturaProveedorAId.ToString(),
                ["busqueda"] = "pintura"
            });
        Assert.Equal(HttpStatusCode.Redirect, asignar1.StatusCode);

        // Fila 2 toma Cable/Proveedor A dentro del mismo borrador antes de guardar.
        var cambiar2 = await navegador.PostAsync(
            $"/ConceptosPresupuesto/CambiarProveedor/{_presupuestoId}?conceptoId={fila2Id}",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}"),
                ["referenciaId"] = _cableProveedorAId.ToString()
            });
        Assert.Equal(HttpStatusCode.Redirect, cambiar2.StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        using EopDbContext lectura = CrearContexto();
        Assert.Equal(_pinturaProveedorAId, (await lectura.ConceptosPresupuesto.SingleAsync(c => c.Id == fila1Id)).MaterialProveedorId);
        Assert.Equal(_cableProveedorAId, (await lectura.ConceptosPresupuesto.SingleAsync(c => c.Id == fila2Id)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task ErrorEnPersistenciaDeUnConcepto_RevierteTodaLaTransaccionSinCambiosParciales()
    {
        using EopDbContext contexto = CrearContexto();
        Usuario responsable = await contexto.Usuarios.SingleAsync(u => u.Id == _responsableOriginalId);
        var repositorio = new RepositorioConceptosPresupuesto(contexto);

        BorradorPresupuestoDto borrador = (await repositorio.ObtenerBorradorBaseAsync(_presupuestoId, CancellationToken.None))!;
        // Un concepto válido + un concepto con referencia inexistente: no debe guardarse NINGUNO.
        borrador.Conceptos.Add(new ConceptoBorradorDto
        {
            Id = borrador.ObtenerSiguienteIdTemporal(),
            TipoConceptoId = TipoConcepto.ManoDeObraId,
            TipoNombre = "Mano de obra",
            Concepto = "Válido que no debe quedar a medias",
            PrecioUnitario = 1000m,
            Moneda = Moneda.UYU,
            EsNuevo = true
        });
        borrador.Conceptos.Add(new ConceptoBorradorDto
        {
            Id = borrador.ObtenerSiguienteIdTemporal(),
            TipoConceptoId = TipoConcepto.MaterialId,
            TipoNombre = "Material",
            Concepto = "Inexistente",
            MaterialId = _cableId,
            MaterialProveedorId = 999999,
            Cantidad = 1m,
            Unidad = UnidadMedida.Metro,
            PrecioUnitario = 10m,
            Moneda = Moneda.UYU,
            EsNuevo = true
        });

        ResultadoGuardarBorrador resultado = await repositorio.GuardarBorradorSiPuedeEditarAsync(
            borrador, responsable, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.ReferenciaInexistente, resultado);
        using EopDbContext lectura = CrearContexto();
        Assert.Empty(await lectura.ConceptosPresupuesto.ToListAsync());
    }

    [FactConSqlServer]
    public async Task AnteErrorInesperadoAlGuardar_InformaYConservaCambiosPendientesParaReintentar()
    {
        var estadoFallo = new ControlDeFalloTransitorio { FallarProximoGuardado = true };

        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion, servicios =>
        {
            servicios.RemoveAll<IRepositorioConceptosPresupuesto>();
            servicios.AddSingleton(estadoFallo);
            servicios.AddScoped<RepositorioConceptosPresupuesto>();
            servicios.AddScoped<IRepositorioConceptosPresupuesto>(sp =>
                new RepositorioConFalloTransitorio(
                    sp.GetRequiredService<RepositorioConceptosPresupuesto>(),
                    sp.GetRequiredService<ControlDeFalloTransitorio>()));
        });

        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Reintento seguro",
            ["Entrada.Importe"] = "2200",
            ["Entrada.Moneda"] = "UYU"
        });

        // Primer intento falla inesperadamente: la base queda intacta y los cambios siguen en sesión.
        var primerIntento = await GuardarBorradorAsync(navegador);
        Assert.Equal(HttpStatusCode.Redirect, primerIntento.StatusCode);

        string paginaConError = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("No se pudo guardar el borrador", paginaConError);
        Assert.Contains("Reintento seguro", paginaConError);
        Assert.Contains("data-cambios-pendientes=\"true\"", paginaConError);

        using (EopDbContext contexto = CrearContexto())
        {
            Assert.Empty(await contexto.ConceptosPresupuesto.ToListAsync());
        }

        // Segundo intento (reintento): ahora persiste correctamente sin perder nada.
        var reintento = await GuardarBorradorAsync(navegador);
        Assert.Equal(HttpStatusCode.Redirect, reintento.StatusCode);

        string paginaGuardada = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("El borrador se guardó correctamente.", paginaGuardada);
        Assert.Contains("data-cambios-pendientes=\"false\"", paginaGuardada);

        using EopDbContext verificacion = CrearContexto();
        ConceptoPresupuesto guardado = await verificacion.ConceptosPresupuesto.SingleAsync();
        Assert.Equal("Reintento seguro", guardado.Concepto);
        Assert.Equal(2200m, guardado.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task DosUsuariosEditandoElMismoPresupuesto_ElSegundoRecibeConflictoSinPisarCambiosDelPrimero()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var operador = await IniciarSesionAsync(fabrica, RolUsuario.Operador, responsable: true, correo: "op@eop.com.uy");
        var supervisor = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor, responsable: false, correo: "sup@eop.com.uy");

        // Ambos cargan sus cambios pendientes partiendo del mismo estado base.
        await AgregarConceptoAsync(operador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Cambio del operador",
            ["Entrada.Importe"] = "1000",
            ["Entrada.Moneda"] = "UYU"
        });
        await AgregarConceptoAsync(supervisor, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
            ["Entrada.Descripcion"] = "Cambio del supervisor",
            ["Entrada.Importe"] = "500",
            ["Entrada.Moneda"] = "USD"
        });

        // El operador guarda primero y tiene éxito.
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(operador)).StatusCode);

        // El supervisor intenta guardar después: se detecta concurrencia y no se pisa al operador.
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(supervisor)).StatusCode);
        string paginaSupervisor = await LeerPaginaAsync(supervisor, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("modificado desde otra operación", paginaSupervisor);
        // Sus cambios pendientes tampoco se pierden en silencio.
        Assert.Contains("Cambio del supervisor", paginaSupervisor);

        using EopDbContext contexto = CrearContexto();
        ConceptoPresupuesto unicoGuardado = await contexto.ConceptosPresupuesto.SingleAsync();
        Assert.Equal("Cambio del operador", unicoGuardado.Concepto);
    }

    [FactConSqlServer]
    public async Task SiLaSesionPerdioLosCambiosPendientes_GuardarInformaErrorEnLugarDeConfirmarExito()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Simulamos que la vista indicó esperabaCambiosPendientes=true pero la sesión ya no los tiene.
        var respuesta = await GuardarBorradorAsync(navegador, esperabaCambiosPendientes: true);
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        string pagina = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("No se encontraron los cambios pendientes", pagina);
        Assert.DoesNotContain("El borrador se guardó correctamente.", pagina);
    }

    private sealed class ControlDeFalloTransitorio
    {
        public bool FallarProximoGuardado { get; set; }
    }

    private sealed class RepositorioConFalloTransitorio(
        IRepositorioConceptosPresupuesto interno,
        ControlDeFalloTransitorio control) : IRepositorioConceptosPresupuesto
    {
        public Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken) =>
            interno.ObtenerPresupuestoAsync(presupuestoId, cancellationToken);

        public Task<bool> PuedeEditarEnBaseAsync(int presupuestoId, Usuario usuario, CancellationToken cancellationToken) =>
            interno.PuedeEditarEnBaseAsync(presupuestoId, usuario, cancellationToken);

        public Task<BorradorPresupuestoDto?> ObtenerBorradorBaseAsync(int presupuestoId, CancellationToken cancellationToken) =>
            interno.ObtenerBorradorBaseAsync(presupuestoId, cancellationToken);

        public Task<ReferenciaCatalogoPresupuestoDto?> ObtenerReferenciaCatalogoAsync(int referenciaId, CancellationToken cancellationToken) =>
            interno.ObtenerReferenciaCatalogoAsync(referenciaId, cancellationToken);

        public Task<string?> ObtenerNombreTipoConceptoAsync(int tipoId, CancellationToken cancellationToken) =>
            interno.ObtenerNombreTipoConceptoAsync(tipoId, cancellationToken);

        public Task<ResultadoGuardarBorrador> GuardarBorradorSiPuedeEditarAsync(
            BorradorPresupuestoDto borrador, Usuario usuario, CancellationToken cancellationToken)
        {
            if (control.FallarProximoGuardado)
            {
                control.FallarProximoGuardado = false;
                throw new InvalidOperationException("Fallo simulado de persistencia.");
            }

            return interno.GuardarBorradorSiPuedeEditarAsync(borrador, usuario, cancellationToken);
        }
    }
}
