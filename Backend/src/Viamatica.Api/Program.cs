using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Viamatica.Api.Infrastructure;
using Viamatica.Application;
using Viamatica.Application.Abstractions;
using Viamatica.Infrastructure;
using Viamatica.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddControllers();

var security = builder.Configuration.GetSection(SecurityOptions.Section).Get<SecurityOptions>()
    ?? throw new InvalidOperationException("Falta la sección Security en la configuración.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = security.JwtIssuer,
            ValidAudience = security.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(security.JwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.Events = new JwtBearerEvents
        {
            // Las claims viajan cifradas en el token: aquí se descifran y se
            // exponen con los tipos estándar para que funcione [Authorize(Roles = ...)].
            OnTokenValidated = context =>
            {
                var cipher = context.HttpContext.RequestServices.GetRequiredService<IFieldCipher>();
                try
                {
                    string Read(string type) => cipher.Decrypt(context.Principal!.FindFirstValue(type)!);
                    var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Read(AppClaims.UserId)),
                        new Claim(ClaimTypes.Name, Read(AppClaims.Username)),
                        new Claim("rid", Read(AppClaims.RolId)),
                        new Claim(ClaimTypes.Role, Read(AppClaims.RolName)),
                    ], JwtBearerDefaults.AuthenticationScheme);
                    context.Principal = new ClaimsPrincipal(identity);
                }
                catch (Exception)
                {
                    context.Fail("Claims inválidas.");
                }
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Viamatica - Gestión de caja", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue el token devuelto por /api/auth/login",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();

// Swagger queda siempre disponible: es un requisito de la prueba.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await app.Services.InitializeDatabaseAsync(app.Configuration);

app.Run();
