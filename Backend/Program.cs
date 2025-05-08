using Backend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configura el puerto para HTTP
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // Configuraci�n del puerto HTTP
});

// Agregar servicios al contenedor.
builder.Services.AddControllers();

// Configuraci�n de DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar servicios de autenticaci�n
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };

        // Manejo de errores del token
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var message = new { error = "Token inv�lido o expirado. Por favor, inicie sesi�n nuevamente." };
                return context.Response.WriteAsJsonAsync(message);
            },
            OnChallenge = context =>
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var message = new { error = "El token no se proporcion� o es inv�lido. Por favor, proporcione un token v�lido." };
                return context.Response.WriteAsJsonAsync(message);
            }
        };
    });

// Configuraci�n de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Mi API", Version = "v1" });
});

// Construir la aplicaci�n
var app = builder.Build();

// Configuraci�n de seguridad de cabeceras
app.Use(async (context, next) =>
{
    // Configurar cabeceras de seguridad HTTP
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff"); // Prevenir MIME sniffing
    context.Response.Headers.Append("X-Frame-Options", "DENY"); // Evitar clickjacking
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block"); // Protecci�n b�sica contra XSS

    await next.Invoke();
});

// Habilitar Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mi API v1");
        c.RoutePrefix = string.Empty;  // Swagger UI en la ra�z (http://localhost:5000)
    });
}

// Configuraci�n para servir el archivo index.html (si tienes archivos est�ticos)
app.MapFallbackToFile("index.html");

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "Hello World!");

// Iniciar la aplicaci�n
app.Run();
