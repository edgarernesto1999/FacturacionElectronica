using FacturacionElectronica.Api.Data;
using FacturacionElectronica.Api.Domain;
using FacturacionElectronica.Api.Domain.Facturacion;
using FacturacionElectronica.Api.DTOs;
using FacturacionElectronica.Api.Security;
using FacturacionElectronica.Api.Services;
using FacturacionElectronica.Api.Services.Facturacion;
using FacturacionElectronica.Api.Services.Sri;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

QuestPDF.Settings.License = LicenseType.Community;



var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// ---------------- CORS ----------------
builder.Services.AddCors(options =>
{
  options.AddPolicy(name: MyAllowSpecificOrigins,
      policy =>
      {
        // URL de tu cliente Blazor
        policy.WithOrigins("https://localhost:7196")
                .AllowAnyHeader()
                .AllowAnyMethod();
      });
});

builder.Services.AddControllers();
builder.Services.AddScoped<FacturaSriBuilder>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddScoped<FirmaElectronicaService>();
builder.Services.AddScoped<FacturaPdfService>();
builder.Services.AddHttpClient<SriRecepcionClient>();
builder.Services.AddScoped<FacturaXmlService>();
builder.Services.AddScoped<SriAutorizacionClient>();
builder.Services.AddSingleton<EmailService>(); // o AddTransient / AddScoped según tu preferencia





// ---------------- JSON ----------------
// Evitar problemas con TypeInfoResolver vacío y ciclos
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
builder.Services.AddScoped<FacturaXmlService>();
builder.Services.AddScoped<FacturaPdfService>();
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
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
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

// ---------------- PIPELINE ----------------
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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

// ---------------- PRODUCTOS ----------------
app.MapPost("/api/productos", async (AppDbContext db, ProductCreateDto dto) =>
{
  if (string.IsNullOrWhiteSpace(dto.TipoProducto) || string.IsNullOrWhiteSpace(dto.Nombre))
    return Results.BadRequest("TipoProducto y Nombre son obligatorios.");

  var p = new Producto
  {
    TipoProducto = dto.TipoProducto.Trim(),
    Nombre = dto.Nombre.Trim(),
    Marca = dto.Marca.Trim(),
    Presentacion = dto.Presentacion.Trim(),
    Activo = dto.Activo
  };

  db.Productos.Add(p);
  await db.SaveChangesAsync();
  return Results.Created($"/api/productos/{p.ProductoId}", p);
});

// LISTAR PRODUCTOS (con lotes)
app.MapGet("/api/productos", async (AppDbContext db) =>
{
  var data = await db.Productos
      .AsNoTracking()
      .Include(p => p.Lotes)
      .OrderBy(p => p.Nombre)
      .Select(p => new
      {
        p.ProductoId,
        p.TipoProducto,
        p.Nombre,
        p.Marca,
        p.Presentacion,
        p.Activo,
        Lotes = p.Lotes
              .OrderBy(l => l.FechaCompra)
              .Select(l => new
              {
                l.LoteId,
                l.FechaCompra,
                l.FechaExpiracion,
                l.CostoUnitario,
                l.PrecioVentaUnitario,
                l.CantidadComprada,
                l.CantidadDisponible
              })
              .ToList()
      })
      .ToListAsync();

  return Results.Ok(new { total = data.Count, data });
});

app.MapGet("/api/productos/{id:int}", async (AppDbContext db, int id) =>
{
  var prod = await db.Productos
      .AsNoTracking()
      .Include(p => p.Lotes)
      .FirstOrDefaultAsync(p => p.ProductoId == id);

  if (prod is null) return Results.NotFound();

  var result = new
  {
    prod.ProductoId,
    prod.TipoProducto,
    prod.Nombre,
    prod.Marca,
    prod.Presentacion,
    prod.Activo,
    Lotes = prod.Lotes
          .OrderBy(l => l.FechaCompra)
          .Select(l => new
          {
            l.LoteId,
            l.FechaCompra,
            l.FechaExpiracion,
            l.CostoUnitario,
            l.PrecioVentaUnitario,
            l.CantidadComprada,
            l.CantidadDisponible
          })
          .ToList()
  };

  return Results.Ok(result);
});

app.MapPut("/api/productos/{id:int}", async (AppDbContext db, int id, ProductUpdateDto dto) =>
{
  var p = await db.Productos.FindAsync(id);
  if (p is null) return Results.NotFound();

  p.TipoProducto = dto.TipoProducto.Trim();
  p.Nombre = dto.Nombre.Trim();
  p.Marca = dto.Marca.Trim();
  p.Presentacion = dto.Presentacion.Trim();
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

// =====================================================================
//                              USUARIOS (solo admin)
// =====================================================================
app.MapPost("/api/usuarios", async (AppDbContext db, CreateUserDto dto) =>
{
  // Validaciones básicas
  if (string.IsNullOrWhiteSpace(dto.Nombre) ||
      string.IsNullOrWhiteSpace(dto.Apellido) ||
      string.IsNullOrWhiteSpace(dto.Correo) ||
      string.IsNullOrWhiteSpace(dto.Contrasena) ||
      string.IsNullOrWhiteSpace(dto.Rol))
  {
    return Results.BadRequest("Todos los campos (Nombre, Apellido, Correo, Contrasena, Rol) son obligatorios.");
  }

  var correo = dto.Correo.Trim();
  var rol = dto.Rol.Trim().ToLowerInvariant();
  var estado = string.IsNullOrWhiteSpace(dto.Estado) ? "activo" : dto.Estado.Trim().ToLowerInvariant();

  if (rol != "admin" && rol != "empleado")
    return Results.BadRequest("Rol inválido. Use 'admin' o 'empleado'.");

  if (estado != "activo" && estado != "inactivo")
    return Results.BadRequest("Estado inválido. Use 'activo' o 'inactivo'.");

  var correoLower = correo.ToLower();
  var existe = await db.Usuarios
      .AnyAsync(u => u.Correo.ToLower() == correoLower);

  if (existe)
    return Results.Conflict($"Ya existe un usuario con el correo: {correo}");

  var hash = PasswordHasher.Hash(dto.Contrasena);

  var user = new Usuario
  {
    Nombre = dto.Nombre.Trim(),
    Apellido = dto.Apellido.Trim(),
    Correo = correo,
    ContrasenaHash = hash,
    Rol = rol,
    Estado = estado
  };

  db.Usuarios.Add(user);
  await db.SaveChangesAsync();

  var result = new
  {
    user.Nombre,
    user.Apellido,
    user.Correo,
    user.Rol,
    user.Estado
  };

  return Results.Created($"/api/usuarios/{user.Id}", result);
});

app.MapGet("/api/usuarios", async (AppDbContext db) =>
{
  var data = await db.Usuarios
      .AsNoTracking()
      .OrderBy(u => u.Apellido).ThenBy(u => u.Nombre)
      .Select(u => new
      {
        u.Id,
        u.Nombre,
        u.Apellido,
        u.Correo,
        u.Rol,
        u.Estado
      })
      .ToListAsync();

  return Results.Ok(new { total = data.Count, data });
});


// POST: Crear un nuevo cliente
app.MapPost("/api/clientes", async (AppDbContext db, ClienteCreateDto dto) =>
{
  // Validación básica
  if (string.IsNullOrWhiteSpace(dto.Cedula) || string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
  {
    return Results.BadRequest("Cédula, Nombre y Apellido son campos obligatorios.");
  }

  // Verificar si el cliente ya existe por la cédula
  if (await db.Clientes.AnyAsync(c => c.Cedula == dto.Cedula))
  {
    return Results.Conflict($"Ya existe un cliente con la cédula '{dto.Cedula}'.");
  }

  var nuevoCliente = new Cliente
  {
    Cedula = dto.Cedula.Trim(),
    Nombre = dto.Nombre.Trim(),
    Apellido = dto.Apellido.Trim(),
    Direccion = dto.Direccion?.Trim(),
    Correo = dto.Correo?.Trim(),
    Telefono = dto.Telefono?.Trim()
  };

  db.Clientes.Add(nuevoCliente);
  await db.SaveChangesAsync();

  // Devolver el DTO de detalle para consistencia
  var resultDto = new ClienteDetailDto(
      nuevoCliente.Cedula,
      nuevoCliente.Nombre,
      nuevoCliente.Apellido,
      nuevoCliente.Direccion,
      nuevoCliente.Correo,
      nuevoCliente.Telefono
  );

  return Results.Created($"/api/clientes/{nuevoCliente.Cedula}", resultDto);
});

// GET: Obtener todos los clientes
// GET: Obtener todos los clientes (CORREGIDO)
// GET: Obtener todos los clientes (VERSIÓN CORREGIDA FINAL)
app.MapGet("/api/clientes", async (AppDbContext db) => {
  var clientes = await db.Clientes
      .AsNoTracking()
      .OrderBy(c => c.Apellido)
      .ThenBy(c => c.Nombre)
      // Usamos ClienteDetailDto para incluir la dirección y los demás campos
      .Select(c => new ClienteDetailDto(
          c.Cedula,
          c.Nombre,
          c.Apellido,
          c.Direccion, // <-- ¡Aquí está la dirección!
          c.Correo,
          c.Telefono
      ))
      .ToListAsync();

  return Results.Ok(clientes);
});

// GET: Obtener un cliente por su cédula
app.MapGet("/api/clientes/{cedula}", async (AppDbContext db, string cedula) =>
{
  var cliente = await db.Clientes
      .AsNoTracking()
      .FirstOrDefaultAsync(c => c.Cedula == cedula);

  if (cliente is null)
  {
    return Results.NotFound($"No se encontró el cliente con la cédula '{cedula}'.");
  }

  var resultDto = new ClienteDetailDto(
      cliente.Cedula,
      cliente.Nombre,
      cliente.Apellido,
      cliente.Direccion,
      cliente.Correo,
      cliente.Telefono
  );

  return Results.Ok(resultDto);
});

// PUT: Actualizar un cliente existente
app.MapPut("/api/clientes/{cedula}", async (AppDbContext db, string cedula, ClienteUpdateDto dto) =>
{
  var cliente = await db.Clientes.FindAsync(cedula);

  if (cliente is null)
  {
    return Results.NotFound($"No se encontró el cliente con la cédula '{cedula}'.");
  }

  // Actualizar los datos del cliente
  cliente.Nombre = dto.Nombre.Trim();
  cliente.Apellido = dto.Apellido.Trim();
  cliente.Direccion = dto.Direccion?.Trim();
  cliente.Correo = dto.Correo?.Trim();
  cliente.Telefono = dto.Telefono?.Trim();

  await db.SaveChangesAsync();

  return Results.NoContent(); // Respuesta estándar para una actualización exitosa
});

app.MapDelete("/api/lotes/{id:int}", async (int id, AppDbContext db) =>
{
  var lote = await db.Lotes.FindAsync(id);
  if (lote is null) return Results.NotFound(new { mensaje = "Lote no encontrado." });

  db.Lotes.Remove(lote);
  await db.SaveChangesAsync();

  return Results.NoContent();
});


app.MapPut("/api/usuarios/{id:int}", async (int id, UserUpdateDto dto, AppDbContext db) =>
{
  var user = await db.Usuarios.FindAsync(id);
  if (user is null) return Results.NotFound(new { mensaje = "Usuario no encontrado." });

  // Validaciones básicas
  if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido) || string.IsNullOrWhiteSpace(dto.Correo))
    return Results.BadRequest(new { mensaje = "Nombre, Apellido y Correo son obligatorios." });

  var correoNormalized = dto.Correo.Trim().ToLowerInvariant();

  // Verificar que el correo no lo use otro usuario
  var existeOtro = await db.Usuarios
      .AsNoTracking()
      .AnyAsync(u => u.Id != id && u.Correo.ToLower() == correoNormalized);
  if (existeOtro) return Results.Conflict(new { mensaje = "El correo ya está en uso por otro usuario." });

  // Actualizar campos
  user.Nombre = dto.Nombre.Trim();
  user.Apellido = dto.Apellido.Trim();
  user.Correo = dto.Correo.Trim();
  user.Rol = dto.Rol.Trim().ToLowerInvariant();
  user.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "activo" : dto.Estado.Trim().ToLowerInvariant();

  // Cambiar contraseña si viene ContrasenaNueva
  if (!string.IsNullOrWhiteSpace(dto.ContrasenaNueva))
  {
    // Usa tu PasswordHasher existente
    user.ContrasenaHash = PasswordHasher.Hash(dto.ContrasenaNueva);
  }

  await db.SaveChangesAsync();

  var result = new
  {
    user.Id,
    user.Nombre,
    user.Apellido,
    user.Correo,
    user.Rol,
    user.Estado
  };

  return Results.Ok(result);
});


app.MapPut("/api/lotes/{id:int}", async (int id, LoteUpdateDto dto, AppDbContext db) =>
{
  var lote = await db.Lotes.FindAsync(id);
  if (lote is null) return Results.NotFound(new { mensaje = "Lote no encontrado." });

  // Validaciones básicas
  if (dto.CantidadComprada <= 0) return Results.BadRequest("CantidadComprada debe ser mayor que 0.");
  if (dto.CostoUnitario < 0 || dto.PrecioVentaUnitario < 0) return Results.BadRequest("Precios inválidos.");
  if (dto.FechaExpiracion.HasValue && dto.FechaExpiracion.Value < dto.FechaCompra)
    return Results.BadRequest("FechaExpiracion no puede ser menor a FechaCompra.");

  // Si se actualiza ProductoId, verificar existencia del producto
  if (dto.ProductoId != lote.ProductoId)
  {
    var existeProducto = await db.Productos.AnyAsync(p => p.ProductoId == dto.ProductoId);
    if (!existeProducto) return Results.BadRequest("Producto especificado no existe.");
    lote.ProductoId = dto.ProductoId;
  }

  // Actualizar campos
  lote.FechaCompra = dto.FechaCompra;
  lote.FechaExpiracion = dto.FechaExpiracion;
  lote.CostoUnitario = dto.CostoUnitario;
  lote.PrecioVentaUnitario = dto.PrecioVentaUnitario;

  // Si la cantidad comprada cambia, ajustar cantidad disponible proporcionalmente salvo que el cliente quiera fijarla:
  // Aquí asumimos que CantidadDisponible se redefine explícitamente por el DTO.
  lote.CantidadComprada = dto.CantidadComprada;
  lote.CantidadDisponible = Math.Clamp(dto.CantidadDisponible, 0, dto.CantidadComprada);

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
  return Results.Ok(response);
});




/* ====================================================
                    ENDPOINTS FACTURAS
==================================================== */
var facturasApi = app.MapGroup("/api/facturas");

// POST: Crear factura
facturasApi.MapPost("/", async (FacturaDto dto, FacturaService facturaService) =>
{
  if (dto == null || dto.Detalles == null || dto.Detalles.Count == 0)
    return Results.BadRequest("La factura debe tener al menos un detalle.");

  // dto.Id será null cuando se crea
  var factura = await facturaService.CrearFacturaAsync(dto);

  // Puedes devolver el DTO completo o un resumen
  return Results.Ok(new
  {
    factura.Id,
    factura.Numero,
    factura.NombreCliente,
    factura.ImporteTotal,
    factura.FechaEmision
  });
});

// GET: Listar facturas (para grilla)
facturasApi.MapGet("/", (AppDbContext db) =>
{
  var list = db.Facturas
      .OrderByDescending(f => f.Id)
      .Select(f => new
      {
        f.Id,
        f.Numero,
        f.NombreCliente,
        f.ImporteTotal,
        f.FechaEmision
      })
      .ToList();

  return Results.Ok(list);
});

// GET: Obtener factura completa (devuelve FacturaDto)
facturasApi.MapGet("/{id:int}", (int id, AppDbContext db) =>
{
  var factura = db.Facturas
      .Include(f => f.Detalles)
      .FirstOrDefault(f => f.Id == id);

  if (factura is null)
    return Results.NotFound("Factura no existe.");

  var dto = new FacturaDto
  {
    Id = factura.Id,
    Numero = factura.Numero,
    FechaEmision = factura.FechaEmision,
    TipoIdentificacionCliente = factura.TipoIdentificacionCliente,
    IdentificacionCliente = factura.IdentificacionCliente,
    NombreCliente = factura.NombreCliente,
    DireccionCliente = factura.DireccionCliente,
    SubtotalSinImpuestos = factura.SubtotalSinImpuestos,
    TotalDescuento = factura.TotalDescuento,
    TotalIva = factura.TotalIva,
    ImporteTotal = factura.ImporteTotal,
    Detalles = factura.Detalles.Select(d => new FacturaDetalleDto
    {
      FacturaDetalleId = d.FacturaDetalleId,
      // ProductoId (si lo agregas a la entidad)
      Codigo = d.Codigo,
      Descripcion = d.Descripcion,
      Cantidad = d.Cantidad,
      PrecioUnitario = d.PrecioUnitario,
      Descuento = d.Descuento,
      TotalSinImpuesto = d.TotalSinImpuesto,
      Iva = d.Iva
    }).ToList()
  };

  return Results.Ok(dto);
});

// PUT: Actualizar factura completa
facturasApi.MapPut("/{id:int}", async (int id, FacturaDto dto, FacturaService facturaService) =>
{
  if (dto == null || dto.Detalles == null || dto.Detalles.Count == 0)
    return Results.BadRequest("La factura debe tener al menos un detalle.");

  // Nos aseguramos de que el Id coincida con la ruta
  dto.Id = id;

  var facturaActualizada = await facturaService.ActualizarFacturaAsync(dto);
  if (facturaActualizada is null)
    return Results.NotFound("Factura no existe.");

  return Results.Ok(new
  {
    facturaActualizada.Id,
    facturaActualizada.Numero,
    facturaActualizada.NombreCliente,
    facturaActualizada.ImporteTotal,
    facturaActualizada.FechaEmision
  });
});

// DELETE: Eliminar factura
facturasApi.MapDelete("/{id:int}", async (int id, FacturaService facturaService) =>
{
  var ok = await facturaService.EliminarFacturaAsync(id);
  return ok ? Results.NoContent() : Results.NotFound("Factura no existe.");
});

// GET: XML
facturasApi.MapGet("/{id:int}/xml", (int id, AppDbContext db, FacturaXmlService xmlService) =>
{
  var factura = db.Facturas
      .Include(f => f.Detalles)
      .FirstOrDefault(f => f.Id == id);

  if (factura is null)
    return Results.NotFound("Factura no existe.");

  var bytes = xmlService.GenerarXml(factura);
  return Results.File(bytes, "application/xml", $"FAC-{factura.Numero}.xml");
});

// GET: PDF
facturasApi.MapGet("/{id:int}/pdf", (int id, AppDbContext db, FacturaPdfService pdfService) =>
{
  var factura = db.Facturas
      .Include(f => f.Detalles)
      .FirstOrDefault(f => f.Id == id);

  if (factura is null)
    return Results.NotFound("Factura no existe.");

  var bytes = pdfService.GenerarPdf(factura);
  return Results.File(bytes, "application/pdf", $"FAC-{factura.Numero}.pdf");
});

var sriApi = app.MapGroup("/api/sri");

// ENVIAR UNA FACTURA EXISTENTE AL SRI POR ID
sriApi.MapPost("/enviar/{id:int}", async (
    int id,
    AppDbContext db,
    FacturacionElectronica.Api.Services.Facturacion.FacturaPdfService pdfService, // si lo necesitas luego
    FacturacionElectronica.Api.Services.Facturacion.FacturaXmlService xmlService, // si ya la tienes
    FirmaElectronicaService firmaService,
    SriRecepcionClient sriClient) =>
{
  // 1) Buscar la factura en la BD
  var factura = db.Facturas
      .Include(f => f.Detalles)
      .FirstOrDefault(f => f.Id == id);

  if (factura is null)
    return Results.NotFound("Factura no existe.");

  // 2) Generar XML SIN firmar de esa factura
  var xmlSinFirmar = xmlService.GenerarXml(factura); // byte[]

  // 3) Firmar el XML con tu archivo .p12
  var xmlFirmado = firmaService.FirmarXml(xmlSinFirmar);

  // 4) Enviar al SRI (Recepción)
  var respuestaXml = await sriClient.EnviarComprobanteAsync(xmlFirmado);

  // Por ahora devolvemos la respuesta tal cual para que la veas
  return Results.Ok(new { respuestaXml });
});

var sriApir = app.MapGroup("/api/sri");

sriApir.MapGet("/autorizacion/{id:int}", async (
    int id,
    [FromServices] AppDbContext db,
    [FromServices] FacturaXmlService xmlService,
    [FromServices] SriAutorizacionClient sriAuthClient) =>
{
  try
  {
    // --- PASO 1: Buscar la factura en la base de datos ---
    // Se incluye "Detalles" porque son necesarios si hay que generar el XML.
    var factura = await db.Facturas
        .Include(f => f.Detalles)
        .FirstOrDefaultAsync(f => f.Id == id);

    if (factura is null)
    {
      return Results.NotFound(new { mensaje = $"Factura con ID {id} no encontrada." });
    }

    // --- PASO 2: Generar clave de acceso si no existe ---
    // Si la factura no tiene clave, se genera el XML para crearla y se guarda en la BD.
    if (string.IsNullOrWhiteSpace(factura.ClaveAcceso))
    {
      Console.WriteLine($"Factura {id}: No tiene clave de acceso. Generando...");
      var xmlBytes = xmlService.GenerarXml(factura); // Este método asigna la clave al objeto 'factura'

      if (string.IsNullOrWhiteSpace(factura.ClaveAcceso))
      {
        // Si después de generar el XML sigue sin clave, algo salió muy mal.
        return Results.Problem("Error interno: No se pudo generar la clave de acceso.", statusCode: 500);
      }

      // Guardar la clave recién generada para futuras consultas.
      db.Facturas.Update(factura);
      await db.SaveChangesAsync();
      Console.WriteLine($"Factura {id}: Clave de acceso generada y guardada: {factura.ClaveAcceso}");
    }

    // --- PASO 3: Consultar el estado de la autorización en el SRI ---
    Console.WriteLine($"Consultando autorización para la clave: {factura.ClaveAcceso}");
    var resultadoAutorizacion = await sriAuthClient.AutorizarAsync(factura.ClaveAcceso);

    // --- PASO 4 (Opcional pero recomendado): Actualizar BD si fue autorizado ---
    if (string.Equals(resultadoAutorizacion.Estado, "AUTORIZADO", StringComparison.OrdinalIgnoreCase))
    {
      // Aquí podrías guardar el número y fecha de autorización en tu tabla de Facturas
      // Ejemplo (requiere que agregues estas propiedades a tu clase Factura):
      // factura.NumeroAutorizacion = resultadoAutorizacion.NumeroAutorizacion;
      // if (DateTime.TryParse(resultadoAutorizacion.FechaAutorizacion, out var fechaAuth))
      // {
      //     factura.FechaAutorizacion = fechaAuth;
      // }
      // await db.SaveChangesAsync();
    }

    // --- PASO 5: Devolver una respuesta clara y útil al cliente ---
    var response = new
    {
      facturaId = factura.Id,
      claveAcceso = factura.ClaveAcceso,
      estadoSri = resultadoAutorizacion.Estado,
      numeroAutorizacion = resultadoAutorizacion.NumeroAutorizacion,
      fechaAutorizacion = resultadoAutorizacion.FechaAutorizacion,
      mensajesSri = resultadoAutorizacion.Mensajes,
      // Durante el desarrollo, es muy útil devolver la respuesta cruda para depurar
      respuestaCrudaSri = resultadoAutorizacion.RawXml
    };

    return Results.Ok(response);
  }
  catch (Exception ex)
  {
    // Captura cualquier error inesperado y devuelve una respuesta de error 500.
    // Es crucial loggear el error para poder diagnosticarlo.
    Console.WriteLine($"Error crítico en el endpoint de autorización para factura ID {id}: {ex}");
    return Results.Problem(
        detail: "Ocurrió un error inesperado al procesar la autorización. Revise los logs del servidor.",
        statusCode: 500
    );
  }
});
// POST: /api/facturas/{facturaId}/enviar-correo/{clienteId}
// clienteId puede ser la cédula (string) o el Id numérico del cliente
facturasApi.MapPost("/{facturaId:int}/enviar-correo/{clienteId}", async (
    int facturaId,
    string clienteId,
    AppDbContext db,
    FacturaPdfService pdfService,
    FacturaXmlService xmlService,
    EmailService emailService) =>
{
  // 1) Cargar factura
  var factura = await db.Facturas
      .Include(f => f.Detalles)
      .FirstOrDefaultAsync(f => f.Id == facturaId);

  if (factura is null)
    return Results.NotFound(new { mensaje = "Factura no existe." });

  // 2) Intentar obtener el cliente por "clienteId"
  // Primero se busca por cédula (campo 'Cedula' usado en tu modelo Cliente)
  var cliente = await db.Clientes
      .AsNoTracking()
      .FirstOrDefaultAsync(c => c.Cedula == clienteId);

  // Si no se encontró, intentar buscar por Id numérico (si clienteId es parseable)
  if (cliente is null && int.TryParse(clienteId, out var clienteIdNum))
  {
    try
    {
      // Si tu entidad Cliente tiene propiedad Id (int), esto la encontrará.
      cliente = await db.Clientes
          .AsNoTracking()
          .FirstOrDefaultAsync(c => EF.Property<int>(c, "Id") == clienteIdNum);
    }
    catch
    {
      // Si la propiedad "Id" no existe en Cliente, simplemente ignoramos y cliente seguirá siendo null.
    }
  }

  if (cliente is null)
    return Results.BadRequest(new { mensaje = "Cliente no encontrado. Verifica el clienteId entregado." });

  var correo = cliente.Correo?.Trim();
  if (string.IsNullOrWhiteSpace(correo))
    return Results.BadRequest(new { mensaje = "El cliente no tiene un correo registrado." });

  // 3) Generar PDF
  byte[] pdfBytes;
  try
  {
    pdfBytes = pdfService.GenerarPdf(factura);
  }
  catch (Exception ex)
  {
    return Results.Problem(title: "Error generando PDF", detail: ex.Message, statusCode: 500);
  }

  // 4) Generar XML
  byte[] xmlBytes;
  try
  {
    xmlBytes = xmlService.GenerarXml(factura);
  }
  catch (Exception ex)
  {
    return Results.Problem(title: "Error generando XML", detail: ex.Message, statusCode: 500);
  }

  // 5) Enviar correo
  var nombreCliente = factura.NombreCliente ?? $"{cliente.Nombre} {cliente.Apellido}".Trim();
  var claveAcceso = factura.ClaveAcceso ?? factura.Numero ?? "N/A";

  try
  {
    await emailService.EnviarFacturaPorCorreoAsync(
        correo,
        nombreCliente,
        claveAcceso,
        xmlBytes,
        pdfBytes
    );
  }
  catch (Exception ex)
  {
    return Results.Problem(title: "Error enviando correo", detail: ex.Message, statusCode: 500);
  }

  // 6) Opcional: marcar en BD que se envió el correo (añade columnas en Factura si quieres historial)
 

  return Results.Ok(new { mensaje = "Factura enviada correctamente al correo.", email = correo });
});



app.Run();
