using Microsoft.IdentityModel.Tokens;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Services;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PCP_Shooting.Security;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Configure ConnectionStrings:Postgres before using persistence.");
    }

    options.UseNpgsql(connectionString);
});

// Add services to the container.

builder.Services.AddScoped<IPedidosService, PedidosService>();
builder.Services.AddScoped<IMembresiasService, MembresiasService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IProductosService, ProductosService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IPerfilesService, PerfilesService>();
builder.Services.AddScoped<IAuthorizationHandler, AccesoPerfilHandler>();
builder.Services.AddScoped<AccesoAlimentosFilter>();
builder.Services.AddScoped<ISupabaseUsuariosPersistence, SupabaseUsuariosPersistence>();
builder.Services.AddScoped<IAdministradorInicialService, AdministradorInicialService>();
builder.Services.AddScoped<IEscriturasService, EscriturasService>();
builder.Services.AddScoped<IImagenesService, ImagenesService>();
builder.Services.AddHttpClient<IImagenStorage, SupabaseImagenStorage>();
builder.Services.AddScoped<ITransaccionesPersistence, TransaccionesPersistence>();
builder.Services.AddExceptionHandler<OperacionExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.IncludeErrorDetails = false;
    options.TokenValidationParameters.RequireSignedTokens = true;
    options.TokenValidationParameters.RequireExpirationTime = true;
    options.TokenValidationParameters.ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256 };
    options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
    options.TokenValidationParameters.ValidateIssuer = true;
    options.TokenValidationParameters.ValidateAudience = true;
    options.TokenValidationParameters.ValidAudience = "authenticated";
    options.TokenValidationParameters.ValidateLifetime = true;
    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
    options.TokenValidationParameters.RoleClaimType = "pcp_role";

    var projectUrl = builder.Configuration["Supabase:Url"];
    if (!string.IsNullOrWhiteSpace(projectUrl))
    {
        var issuer = projectUrl.TrimEnd('/') + "/auth/v1";
        options.TokenValidationParameters.ValidIssuer = issuer;
        options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            issuer + "/.well-known/jwks.json", new SupabaseJwksRetriever(issuer));
    }

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            if (!Guid.TryParse(context.Principal?.FindFirstValue("sub"), out var id) || id == Guid.Empty)
            {
                context.Fail("El token requiere un identificador de usuario válido.");
                return;
            }
            var identity = (ClaimsIdentity)context.Principal!.Identity!;
            foreach (var claim in identity.FindAll("pcp_role").ToArray()) identity.RemoveClaim(claim);

            var db = context.HttpContext.RequestServices.GetRequiredService<PcpShootingDbContext>();
            var perfil = await db.PerfilesUsuario.AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == id, context.HttpContext.RequestAborted);
            if (perfil?.Activo == true)
                identity.AddClaim(new Claim("pcp_role", perfil.Rol.ToString()));
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PerfilActivo", policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new AccesoPerfilRequirement()));
    options.AddPolicy("Administrador", policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new AccesoPerfilRequirement(RolUsuario.Administrador)));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAdministracionService, AdministracionService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddAuthorization(options =>
    options.AddPolicy("OperacionPedidos", policy => policy.RequireAuthenticatedUser()
        .AddRequirements(new AccesoPerfilRequirement(RolUsuario.Administrador, RolUsuario.Recepcion))));
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
        .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token de acceso de Supabase Auth."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres");

var app = builder.Build();

if (args.Contains("--bootstrap-admin", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        var inicializador = scope.ServiceProvider.GetRequiredService<IAdministradorInicialService>();
        var id = await inicializador.InicializarAsync(builder.Configuration["bootstrap-admin"] ?? "", CancellationToken.None);
        Console.WriteLine($"Administrador inicial configurado. Id {id}");
    }
    catch (PCP_Shooting.Domain.Models.OperacionException exception)
    {
        Console.Error.WriteLine(exception.Message);
        Environment.ExitCode = 1;
    }
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Frontend");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/postgres").RequireAuthorization("Administrador");
app.MapControllers();

app.Run();

public partial class Program { }
