using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;
namespace PCP_Shooting.Tests;

public sealed class PortadasPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ProvisionPortadasAplicadaMantieneBucketOchoMibYPermisosRestringidos()
    {
        await using var db=fixture.CrearContext();
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT public AND file_size_limit=8388608 AND allowed_mime_types @> ARRAY['image/jpeg','image/png','image/webp']::text[] AS "Value"
            FROM storage.buckets WHERE id='pcp-portadas'
            """).SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS(SELECT 1 FROM pg_policies WHERE schemaname='storage' AND tablename='objects' AND policyname='pcp_portadas_insertar_imagen' AND cmd='INSERT')
                AND NOT has_function_privilege('anon','public.pcp_puede_subir_portada_pagina(text)','EXECUTE')
                AND has_function_privilege('authenticated','public.pcp_puede_subir_portada_pagina(text)','EXECUTE') AS "Value"
            """).SingleAsync());
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT public.pcp_puede_subir_portada_pagina('portadas/no-reservada.png') AS \"Value\"").SingleAsync());
    }

    [PostgresFact]
    public async Task MigracionGestionYCargaConservanEstadoYAuditoriaConTransaccionesReales()
    {
        await using (var db = fixture.CrearContext())
        {
            Assert.StartsWith("pcp_test_",await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"CargasPortadaPagina\", \"PortadasPagina\"");
            var ops = new PortadasPagina().UpOperations.Where(o => o is not SqlOperation s || !s.Sql.Contains("storage.buckets")).ToArray();
            foreach (var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(ops,db.Model)) await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
            Assert.Equal(2,await db.Database.SqlQueryRaw<int>("""
                SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname=current_schema() AND c.relname IN ('PortadasPagina','CargasPortadaPagina') AND c.relrowsecurity
                    AND NOT has_table_privilege('anon',c.oid,'SELECT') AND NOT has_table_privilege('authenticated',c.oid,'SELECT')
                """).SingleAsync());
        }
        var storage = new ContenidoImagenesApiTests.StorageSimulado(); using var origen = new OperacionesFactory(fixture);
        using var app = origen.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IImagenStorage>(); s.AddSingleton<IImagenStorage>(storage); }));
        using var admin = app.CreateClient(); admin.DefaultRequestHeaders.Add("X-Usuario-Prueba",OperacionesFactory.AdminId.ToString()); admin.DefaultRequestHeaders.Authorization = new("Bearer","prueba");
        const string ruta = "/api/administracion/portadas";
        Assert.Empty((await admin.GetFromJsonAsync<JsonElement>("/api/portadas")).EnumerateArray());
        Assert.Equal(HttpStatusCode.OK,(await admin.PutAsJsonAsync(ruta + "/inicio",PortadasApiTests.Datos(activa:false,imagen:null))).StatusCode);
        var id = Guid.NewGuid();
        async Task<HttpResponseMessage> Subir() {
            using var form = new MultipartFormDataContent(); form.Add(new StringContent(id.ToString()),"idOperacion");
            var archivo = new ByteArrayContent(ImagenesTests.Png); archivo.Headers.ContentType = new("image/png"); form.Add(archivo,"archivo","foto.png");
            return await admin.PostAsync(ruta + "/inicio/imagen",form);
        }
        var subida = await Subir(); Assert.Equal(HttpStatusCode.OK,subida.StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await Subir()).StatusCode); Assert.Equal(1,storage.Intentos);
        var portada = Assert.Single((await admin.GetFromJsonAsync<JsonElement>(ruta)).EnumerateArray());
        Assert.False(portada.GetProperty("activa").GetBoolean());
        Assert.Equal(HttpStatusCode.OK,(await admin.PutAsJsonAsync(ruta + "/inicio",PortadasApiTests.Datos(portada.GetProperty("version").GetGuid(),false,imagen:null))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Subir()).StatusCode);
        await using var verificar = fixture.CrearContext();
        var guardada = await verificar.Set<PortadaPagina>().SingleAsync(); Assert.False(guardada.Activa); Assert.True(guardada.Configurada);
        Assert.Single(await verificar.Set<CargaPortadaPagina>().Where(c => c.Disponible).ToListAsync());
        Assert.Contains(await verificar.EventosAuditoria.ToListAsync(),e => e.EntidadId==guardada.Id && e.ActorId==OperacionesFactory.AdminId && e.Accion=="Modified");
    }

    [PostgresFact]
    public async Task FuncionStorageSoloAutorizaReservaDeCargaConAutorActivoYVersionActual()
    {
        await using var db = fixture.CrearContext(); var esquema=await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.Matches("^pcp_test_[a-f0-9]{32}$",esquema); await using var tx=await db.Database.BeginTransactionAsync();
        var sql=new PortadasPagina().UpOperations.OfType<SqlOperation>().Single(s => s.Sql.Contains("CREATE FUNCTION public.pcp_puede_subir_portada_pagina")).Sql;
        var funcion=sql[sql.IndexOf("CREATE FUNCTION",StringComparison.Ordinal)..sql.IndexOf("CREATE POLICY",StringComparison.Ordinal)]
            .Replace("public.pcp_puede_subir_portada_pagina",$"\"{esquema}\".pcp_puede_subir_portada_pagina").Replace("public.\"",$"\"{esquema}\".\"");
        await db.Database.ExecuteSqlRawAsync(funcion);
        var actor=new PerfilUsuario { Id=Guid.NewGuid(),Nombre="Gestor Portadas",Rol=PCP_Shooting.Domain.Interfaces.RolUsuario.Administrador };
        var p=new PortadaPagina { Id=Guid.NewGuid(),Pagina="nosotros",Activa=true,Version=Guid.NewGuid(),ActualizadoEnUtc=DateTime.UtcNow };
        var carga=new CargaPortadaPagina { Id=Guid.NewGuid(),PortadaId=p.Id,CreadorId=actor.Id,Ruta=$"portadas/nosotros/{Guid.NewGuid():N}.png",
            Sha256=new string('a',64),TipoContenido="image/png",Tamano=8388608,VersionDestino=p.Version,CreadaEnUtc=DateTime.UtcNow };
        db.AddRange(actor,p,carga); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('request.jwt.claim.sub',{actor.Id.ToString()},true)");
        // Esquema validado por regex; nombre de archivo parametrizado.
        var consulta=$"SELECT \"{esquema}\".pcp_puede_subir_portada_pagina({{0}}) AS \"Value\"";
        Task<bool> Permite(string ruta) => db.Database.SqlQueryRaw<bool>(consulta,ruta).SingleAsync();
        Assert.True(await Permite(carga.Ruta)); Assert.False(await Permite("portadas/no-reservada.png"));
        actor.Activo=false; await db.SaveChangesAsync(); Assert.False(await Permite(carga.Ruta));
        actor.Activo=true; p.Version=Guid.NewGuid(); await db.SaveChangesAsync(); Assert.False(await Permite(carga.Ruta));
        carga.VersionDestino=p.Version; carga.Disponible=true; carga.SubidaEnUtc=DateTime.UtcNow; await db.SaveChangesAsync(); Assert.False(await Permite(carga.Ruta));
        await tx.RollbackAsync();
    }
}
