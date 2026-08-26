using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using HealthChecks.UI.Client;
using Hunter.Application;
using Hunter.Infrastructure;
using Hunter.Infrastructure.Persistence;
using Hunter.Infrastructure.Security;
using Hunter.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "DIFRANI | Hunter CRM AI API",
            Version = "v1"
        });
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    const string FrontendCorsPolicy = "Frontend";
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173"];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
    });

    var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
    var jwtOptions = jwtSection.Get<JwtOptions>()
        ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
        throw new InvalidOperationException("Missing 'Jwt:SigningKey'. Set it via environment variable or user-secrets, never in appsettings.json.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization();

    // Sin esto, login/register/refresh (todos [AllowAnonymous]) aceptaban intentos ilimitados:
    // el hash PBKDF2 hace cada intento costoso, pero no había lockout por cuenta ni límite por IP
    // (auditoria.md, hallazgo Medio "sin rate limiting"). Particionado por IP, no por usuario: un
    // intento de login con cualquier email cuenta contra el mismo balde de esa IP.
    static string ClientIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
            ClientIp(context),
            _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 5, QueueLimit = 0 }));

        options.AddPolicy("register", context => RateLimitPartition.GetFixedWindowLimiter(
            ClientIp(context),
            _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromHours(1), PermitLimit = 3, QueueLimit = 0 }));

        options.AddPolicy("refresh", context => RateLimitPartition.GetFixedWindowLimiter(
            ClientIp(context),
            _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 20, QueueLimit = 0 }));

        options.OnRejected = async (context, ct) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail("Demasiados intentos. Esperá un momento antes de volver a intentar."), ct);
        };
    });

    var connectionString = builder.Configuration.GetConnectionString("HunterDb")
        ?? throw new InvalidOperationException("Missing connection string 'HunterDb'.");

    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "postgresql");

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<HunterDbContext>();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        // Lock advisory de sesión (se libera solo al cerrar la conexión, o explícitamente abajo)
        // con una key fija arbitraria para todo el proceso: si algún día se escala
        // horizontalmente, o un deploy solapado arranca dos procesos a la vez, la segunda
        // instancia espera acá en vez de correr Database.Migrate() en paralelo contra la misma
        // base (auditoria.md, hallazgo Medio "migración automática sin lock" — riesgo de DDL
        // concurrente, "column already exists", deadlock de catálogo). Con una sola instancia
        // (caso de hoy) esto no cambia nada observable.
        const long MigrationLockKey = 727483910;
        try
        {
            await using (var lockCmd = connection.CreateCommand())
            {
                lockCmd.CommandText = $"SELECT pg_advisory_lock({MigrationLockKey})";
                await lockCmd.ExecuteNonQueryAsync();
            }

            db.Database.Migrate();
        }
        finally
        {
            await using var unlockCmd = connection.CreateCommand();
            unlockCmd.CommandText = $"SELECT pg_advisory_unlock({MigrationLockKey})";
            await unlockCmd.ExecuteNonQueryAsync();
        }
    }

    // Sin esto, una excepción no controlada en un controller devolvía un 500 vacío del handler
    // default de ASP.NET Core, sin el formato ApiResponse que usa el resto de la API
    // (auditoria.md, hallazgo Medio/Info "sin manejador de excepciones global"). No se expone
    // ex.Message al cliente: el detalle solo va al log, para no filtrar información interna.
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (error is not null)
        {
            context.RequestServices.GetRequiredService<ILogger<Program>>()
                .LogError(error, "Excepción no controlada procesando {Path}", context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail("Ocurrió un error inesperado. Intentá de nuevo más tarde."));
    }));

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "DIFRANI | Hunter CRM AI API v1");
        });
    }

    app.UseHttpsRedirection();

    app.UseCors(FrontendCorsPolicy);

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Hunter API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
