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
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
