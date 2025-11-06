using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization.Metadata;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using FacturacionElectronica.Api.Data;
using FacturacionElectronica.Api.DTOs;
using FacturacionElectronica.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// ---------------- JSON ----------------
// Forzamos el resolutor por reflexión para evitar problemas de TypeInfoResolver vacío.
builder.Services.ConfigureHttpJsonOptions(o =>
{
  o.SerializerOptions.TypeInfoResolverChain.Clear();
  o.SerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
});

// ---------------- EF Core ----------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------- JWT ----------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? "CAMBIA_ESTA_CLAVE_LARGA_DE_32+_CHARS";
var jwtIssuer = jwtSection["Issuer"] ?? "Facturacion.Api";
var jwtAudience = jwtSection["Audience"] ?? "Facturacion.Client";
var jwtMinutes = int.TryParse(jwtSection["ExpirationMinutes"], out var mm) ? mm : 60;

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
      o.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.Zero
      };
    });

builder.Services.AddAuthorization(options =>
{
  options.AddPolicy("admin", p => p.RequireClaim(ClaimTypes.Role, "admin"));
  options.AddPolicy("empleado", p => p.RequireClaim(ClaimTypes.Role, "empleado", "admin"));
});

// ---------------- Swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "Facturación API", Version = "v1" });

  c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Description = "JWT en el header. Ej: Bearer {token}",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT"
  });

  c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme, Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// ---------------- HEALTH ----------------
//app.MapGet("/api/health", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));

// ---------------- LOGIN ----------------
app.MapPost("/api/auth/login", async (AppDbContext db, LoginRequest req) =>
{
  var email = (req.Correo ?? "").Trim();

  // Búsqueda case-insensitive por correo
  var user = await db.Usuarios
      .Where(u => u.Correo.ToLower() == email.ToLower())
      .FirstOrDefaultAsync();

  if (user is null)
  {
    Console.WriteLine($"LOGIN FAIL: usuario no encontrado ({email})");
    return Results.Unauthorized();
  }

  if (!string.Equals(user.Estado?.Trim(), "activo", StringComparison.OrdinalIgnoreCase))
  {
    Console.WriteLine($"LOGIN FAIL: estado {user.Estado}");
    return Results.Forbid();
  }

  // Fallback temporal: si no es PBKDF2, comparar texto plano
  bool ok = user.ContrasenaHash.StartsWith("PBKDF2$", StringComparison.Ordinal)
            ? PasswordHasher.Verify(req.Contrasena ?? "", user.ContrasenaHash)
            : (user.ContrasenaHash ?? "") == (req.Contrasena ?? "");

  if (!ok)
  {
    Console.WriteLine("LOGIN FAIL: contraseña incorrecta");
    return Results.Unauthorized();
  }

  var expires = DateTime.UtcNow.AddMinutes(jwtMinutes);

  var claims = new[]
  {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Correo),
        new Claim(ClaimTypes.Name, $"{user.Nombre} {user.Apellido}"),
        new Claim(ClaimTypes.Role, user.Rol)
    };

  var token = new JwtSecurityToken(
      issuer: jwtIssuer,
      audience: jwtAudience,
      claims: claims,
      expires: expires,
      signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
  );

  var jwt = new JwtSecurityTokenHandler().WriteToken(token);
  return Results.Ok(new LoginResponse(jwt, expires, $"{user.Nombre} {user.Apellido}", user.Rol));
});

// ---------------- ENDPOINT PROTEGIDO (admin) ----------------
//app.MapGet("/api/seguro/admin-solo", () => Results.Ok(new { ok = true, area = "admin" }))
 //  .RequireAuthorization("admin");

app.Run();
app.Run();
