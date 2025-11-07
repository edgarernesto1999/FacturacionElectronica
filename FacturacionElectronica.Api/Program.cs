using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization; 


using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using FacturacionElectronica.Api.Data;
using FacturacionElectronica.Api.DTOs;
using FacturacionElectronica.Api.Security;
using FacturacionElectronica.Api.Domain;

var builder = WebApplication.CreateBuilder(args);

// ---------------- JSON ----------------
// Forzamos el resolutor por reflexión para evitar problemas de TypeInfoResolver vacío.
builder.Services.ConfigureHttpJsonOptions(o =>
{
  o.SerializerOptions.TypeInfoResolverChain.Clear();
  o.SerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());

  o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
  o.SerializerOptions.MaxDepth = 64;
});

// ---------------- EF Core ----------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------- JWT ----------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? "R3@lly$tr0ngJwtKey_2025#Factur@cion!";
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
  var httpPort = builder.Configuration["ASPNETCORE_HTTP_PORT"] ?? "5142";
  var httpsPort = builder.Configuration["ASPNETCORE_HTTPS_PORT"] ?? "7142";
  c.AddServer(new OpenApiServer { Url = $"https://localhost:{httpsPort}" });
  c.AddServer(new OpenApiServer { Url = $"http://localhost:{httpPort}" });
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



// ---------------- PRODUCTOS ----------------
app.MapPost("/api/productos", async (AppDbContext db, ProductCreateDto dto) =>
{
  if (string.IsNullOrWhiteSpace(dto.TipoProducto) || string.IsNullOrWhiteSpace(dto.Nombre))
    return Results.BadRequest("TipoProducto y Nombre son obligatorios.");

  var p = new Producto
  {
    TipoProducto = dto.TipoProducto.Trim(),
    Nombre = dto.Nombre.Trim(),
    Activo = dto.Activo
  };

  db.Productos.Add(p);
  await db.SaveChangesAsync();
  return Results.Created($"/api/productos/{p.ProductoId}", p);
});

// ==================== LISTAR PRODUCTOS ====================
app.MapGet("/api/productos", async (AppDbContext db) =>
{
  var data = await db.Productos
      .AsNoTracking()
      .Include(p => p.Lotes) // cargar los lotes asociados
      .OrderBy(p => p.Nombre)
      .Select(p => new
      {
        p.ProductoId,
        p.TipoProducto,
        p.Nombre,
        p.Activo,
        Lotes = p.Lotes.Select(l => new
        {
          l.LoteId,
          l.FechaCompra,
          l.FechaExpiracion,
          l.CostoUnitario,
          l.PrecioVentaUnitario,
          l.CantidadComprada,
          l.CantidadDisponible
        }).ToList()
      })
      .ToListAsync();

  return Results.Ok(new { total = data.Count, data });
});



app.MapGet("/api/productos/{id:int}", async (AppDbContext db, int id) =>
{
  var prod = await db.Productos
      .AsNoTracking()
      .Include(p => p.Lotes.OrderBy(l => l.FechaCompra))
      .FirstOrDefaultAsync(p => p.ProductoId == id);

  return prod is null ? Results.NotFound() : Results.Ok(prod);
});

app.MapPut("/api/productos/{id:int}", async (AppDbContext db, int id, ProductUpdateDto dto) =>
{
  var p = await db.Productos.FindAsync(id);
  if (p is null) return Results.NotFound();

  p.TipoProducto = dto.TipoProducto.Trim();
  p.Nombre = dto.Nombre.Trim();
  p.Activo = dto.Activo;

  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapDelete("/api/productos/{id:int}", async (AppDbContext db, int id) =>
{
  var p = await db.Productos.FindAsync(id);
  if (p is null) return Results.NotFound();

  p.Activo = false;
  await db.SaveChangesAsync();
  return Results.NoContent();
});

// ---------------- LOTES ----------------
app.MapPost("/api/lotes", async (AppDbContext db, LoteCreateDto dto) =>
{
  var prod = await db.Productos.FindAsync(dto.ProductId);
  if (prod is null) return Results.BadRequest("Producto no existe.");

  if (dto.CantidadComprada <= 0) return Results.BadRequest("CantidadComprada debe ser > 0.");
  if (dto.CostoUnitario < 0 || dto.PrecioVentaUnitario < 0) return Results.BadRequest("Precios inválidos.");
  if (dto.FechaExpiracion is not null && dto.FechaExpiracion < dto.FechaCompra)
    return Results.BadRequest("La fecha de expiración no puede ser menor que la fecha de compra.");

  var lote = new Lote
  {
    ProductoId = dto.ProductId,
    FechaCompra = dto.FechaCompra,
    FechaExpiracion = dto.FechaExpiracion,
    CostoUnitario = dto.CostoUnitario,
    PrecioVentaUnitario = dto.PrecioVentaUnitario,
    CantidadComprada = dto.CantidadComprada,
    CantidadDisponible = dto.CantidadComprada
  };

  db.Lotes.Add(lote);
  await db.SaveChangesAsync();
  var response = new
  {
    lote.LoteId,
    lote.ProductoId,
    lote.FechaCompra,
    lote.FechaExpiracion,
    lote.CostoUnitario,
    lote.PrecioVentaUnitario,
    lote.CantidadComprada,
    lote.CantidadDisponible
  };
  return Results.Created($"/api/lotes/{lote.LoteId}", response);
});

app.MapGet("/api/lotes/por-expirar", async (AppDbContext db, int dias = 30) =>
{
  var limite = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(dias));
  var data = await db.Lotes
      .AsNoTracking()
      .Where(l => l.FechaExpiracion != null
               && l.CantidadDisponible > 0
               && l.FechaExpiracion <= limite)
      .OrderBy(l => l.FechaExpiracion)
      .Select(l => new
      {
        l.LoteId,
        l.ProductoId,
        Producto = l.Producto!.Nombre,
        l.FechaCompra,
        l.FechaExpiracion,
        l.CantidadDisponible
      })
      .ToListAsync();

  return Results.Ok(new { dias, total = data.Count, data });
});

app.Run();
