using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using DataAccess.Context;
using DataAccess.Repositories;
using DataAccess.Seeding;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Controladores
builder.Services.AddControllers();

// Configuración de Persistencia (EF Core + SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=veterinaria.db;Cache=Shared";

builder.Services.AddDbContext<VeterinariaDbContext>(options =>
    options.UseSqlite(connectionString));

// Inyección de Dependencias por Constructor
// 1. Repositorios (DataAccess)
builder.Services.AddScoped<IDuenoRepository, DuenoRepository>();
builder.Services.AddScoped<IMascotaRepository, MascotaRepository>();
builder.Services.AddScoped<IAtencionMedicaRepository, AtencionMedicaRepository>();

// 2. Servicios (BusinessLogic)
builder.Services.AddScoped<IDuenoService, DuenoService>();
builder.Services.AddScoped<IMascotaService, MascotaService>();
builder.Services.AddScoped<IAtencionMedicaService, AtencionMedicaService>();

// Configuración de OpenAPI y Scalar
builder.Services.AddOpenApi();

var app = builder.Build();

// Inicialización de la base de datos y carga de datos de prueba al arrancar
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
    await DbInitializer.InitializeAsync(context);
}

// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Disponible en /scalar/v1
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Requerido para WebApplicationFactory en tests de integración
public partial class Program { }
