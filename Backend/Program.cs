using Backend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
        // Personalizar el manejo de eventos de error del token
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Si la autenticaci�n del token falla
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var message = new { error = "Token inv�lido o expirado. Por favor, inicie sesi�n nuevamente." };
                return context.Response.WriteAsJsonAsync(message);
            },
            OnChallenge = context =>
            {
                // Si el token no est� presente
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var message = new { error = "El token no se proporcion� o es inv�lido. Por favor, proporcione un token v�lido." };
                return context.Response.WriteAsJsonAsync(message);
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
