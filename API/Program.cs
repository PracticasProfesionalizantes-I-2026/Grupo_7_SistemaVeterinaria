using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using DataAccess.Context;
using DataAccess.Repositories;
using DataAccess.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ITokenRevocadoRepository, TokenRevocadoRepository>();
builder.Services.AddScoped<IRegistroOperacionUsuarioRepository, RegistroOperacionUsuarioRepository>();
builder.Services.AddScoped<IHistorialPropietarioRepository, HistorialPropietarioRepository>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<IDisponibilidadVeterinarioRepository, DisponibilidadVeterinarioRepository>();

// 2. Servicios (BusinessLogic)
builder.Services.AddScoped<IDuenoService, DuenoService>();
builder.Services.AddScoped<IMascotaService, MascotaService>();
builder.Services.AddScoped<IAtencionMedicaService, AtencionMedicaService>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IHistoriaClinicaService, HistoriaClinicaService>();

// Configuración de Autenticación con JWT Bearer
var secretKey = builder.Configuration["JwtSettings:SecretKey"] 
    ?? "VeterinariaPatitasDesarrolloSeguraSecretKey2026!#JWTAuthorization";
var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "VeterinariaPatitasAPI";
var audience = builder.Configuration["JwtSettings:Audience"] ?? "VeterinariaPatitasClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var path = context.HttpContext.Request.Path;
                bool isLogout = path.StartsWithSegments("/api/auth/logout", StringComparison.OrdinalIgnoreCase);

                // Si la petición no es de logout, bloqueamos los tokens revocados
                if (!isLogout)
                {
                    var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti))
                    {
                        var tokenRepo = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocadoRepository>();
                        if (await tokenRepo.EstaRevocadoAsync(jti))
                        {
                            context.Fail("El token de sesión ha sido revocado.");
                        }
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

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
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Requerido para WebApplicationFactory en tests de integración
public partial class Program { }
