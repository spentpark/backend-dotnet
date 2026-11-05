Imports System
Imports System.IdentityModel.Tokens.Jwt
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Authentication.JwtBearer
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.AspNetCore.Mvc.ApplicationParts
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.IdentityModel.Tokens
Imports Microsoft.OpenApi
Imports Microsoft.OpenApi.Extensions
Imports Microsoft.OpenApi.Models
Imports Swashbuckle.AspNetCore.Swagger

''' <summary>
''' Punto de entrada de la API.
''' La construccion de la aplicacion vive en <see cref="CreateApp"/> para poder
''' probarla (y medir su cobertura) sin arrancar Kestrel ni abrir conexiones MySQL.
''' </summary>
Public Class Program

    ''' <summary>Connection string historica, usada solo si la configuracion no define una.</summary>
    Public Const DefaultConnectionString As String =
        "server=192.168.1.82;" &
        "port=3306;" &
        "database=game;" &
        "user=root;" &
        "password=Felipe1981;"

    Public Shared Sub Main(args As String())
        CreateApp(args).Run()
    End Sub

    ''' <summary>Construye la aplicacion completa (servicios + pipeline) sin ejecutarla.</summary>
    Public Shared Function CreateApp(args As String()) As WebApplication
        Dim builder = WebApplication.CreateBuilder(args)

        ConfigureServices(builder.Services, builder.Configuration)

        Dim app = builder.Build()
        ConfigurePipeline(app)

        Return app
    End Function

    ''' <summary>Registra controladores, Swagger, EF Core, autenticacion y la capa de repositorios/servicios.</summary>
    Public Shared Sub ConfigureServices(services As IServiceCollection, config As IConfiguration)
        Dim mvc = services.AddControllers()
        RegisterApplicationPart(mvc)
        services.AddEndpointsApiExplorer()
        services.AddSwaggerGen()

        ' La configuracion se expone como singleton para AuthService y las pruebas.
        services.AddSingleton(config)

        ' CORS: allowlist de Node + cualquier localhost en desarrollo.
        services.AddCors(
            Sub(options)
                options.AddDefaultPolicy(
                    Sub(policy)
                        policy.
                            SetIsOriginAllowed(Function(origin) SecurityConfig.IsAllowedOrigin(origin, config)).
                            WithMethods("GET", "POST", "PUT", "DELETE").
                            WithHeaders("Content-Type", "Authorization", "apikey")
                    End Sub)
            End Sub)

        ConfigureAuthentication(services, config)
        services.AddAuthorization()

        services.AddDbContext(Of AppDbContext)(Sub(options) ConfigureDatabase(options, config))

        services.AddScoped(Of IGameRepository, GameRepository)()
        services.AddScoped(Of IGameService, GameService)()
        services.AddScoped(Of IPlatformRepository, PlatformRepository)()
        services.AddScoped(Of IPlatformService, PlatformService)()
        services.AddScoped(Of IReviewRepository, ReviewRepository)()
        services.AddScoped(Of IReviewService, ReviewService)()
        services.AddScoped(Of ISimilarGameRepository, SimilarGameRepository)()
        services.AddScoped(Of ISimilarGamesService, SimilarGamesService)()
        services.AddScoped(Of IStatsRepository, StatsRepository)()
        services.AddScoped(Of IStatsService, StatsService)()
        services.AddScoped(Of IAuthService, AuthService)()
    End Sub

    ''' <summary>
    ''' Configura JWT Bearer con el mismo secreto, issuer, audience y claim role que
    ''' backend-node20. Los 401/403 responden el JSON de Node.
    ''' </summary>
    Public Shared Sub ConfigureAuthentication(services As IServiceCollection, config As IConfiguration)
        Dim secret = SecurityConfig.GetJwtSecret(config)

        services.
            AddAuthentication(JwtBearerDefaults.AuthenticationScheme).
            AddJwtBearer(
                Sub(options)
                    options.MapInboundClaims = False
                    options.TokenValidationParameters = New TokenValidationParameters With {
                        .ValidateIssuer = True,
                        .ValidIssuer = SecurityConfig.GetJwtIssuer(config),
                        .ValidateAudience = True,
                        .ValidAudience = SecurityConfig.GetJwtAudience(config),
                        .ValidateIssuerSigningKey = True,
                        .IssuerSigningKey = New SymmetricSecurityKey(Encoding.UTF8.GetBytes(If(secret, ""))),
                        .ValidateLifetime = True,
                        .ClockSkew = TimeSpan.Zero,
                        .NameClaimType = "sub",
                        .RoleClaimType = "role"
                    }

                    options.Events = New JwtBearerEvents()
                    options.Events.OnChallenge = AddressOf HandleUnauthorized
                    options.Events.OnForbidden = AddressOf HandleForbidden
                End Sub)
    End Sub

    ''' <summary>401 JSON con WWW-Authenticate: Bearer, igual que Node.</summary>
    Public Shared Function HandleUnauthorized(context As JwtBearerChallengeContext) As Task
        context.HandleResponse()
        context.Response.StatusCode = 401
        context.Response.Headers("WWW-Authenticate") = "Bearer"
        context.Response.ContentType = "application/json"

        Return context.Response.WriteAsync("{""error"":""" & SecurityConfig.UnauthorizedMessage & """}")
    End Function

    ''' <summary>403 JSON cuando el rol del token no es suficiente.</summary>
    Public Shared Function HandleForbidden(context As ForbiddenContext) As Task
        context.Response.StatusCode = 403
        context.Response.ContentType = "application/json"

        Return context.Response.WriteAsync("{""error"":""" & SecurityConfig.ForbiddenMessage & """}")
    End Function

    ''' <summary>
    ''' Garantiza que los controladores de este ensamblado se descubran aunque el
    ''' ensamblado de entrada sea otro (por ejemplo, un host de pruebas).
    ''' </summary>
    Public Shared Sub RegisterApplicationPart(mvc As IMvcBuilder)
        Dim apiAssembly = GetType(Program).Assembly

        mvc.ConfigureApplicationPartManager(
            Sub(manager)
                Dim alreadyRegistered = manager.ApplicationParts _
                    .OfType(Of AssemblyPart)() _
                    .Any(Function(part) part.Assembly Is apiAssembly)

                If Not alreadyRegistered Then
                    manager.ApplicationParts.Add(New AssemblyPart(apiAssembly))
                End If
            End Sub)
    End Sub

    ''' <summary>
    ''' Configura el pipeline con el mismo orden que backend-node20: errores JSON,
    ''' cabeceras de seguridad, CORS, rate limiting, limite de cuerpo, autenticacion
    ''' y rutas. Swagger se sirve en /api-docs segun el entorno.
    ''' </summary>
    Public Shared Sub ConfigurePipeline(app As WebApplication)
        SecurityConfig.ValidateAuthConfiguration(app.Configuration, app.Logger)

        app.UseMiddleware(Of JsonErrorMiddleware)()
        app.UseMiddleware(Of SecurityHeadersMiddleware)()

        app.UseCors()

        app.UseMiddleware(Of RateLimitMiddleware)(ApiRateLimitOptions(app.Configuration))
        app.UseMiddleware(Of RateLimitMiddleware)(LoginRateLimitOptions(app.Configuration))
        app.UseMiddleware(Of BodyLimitMiddleware)()

        app.UseRouting()
        app.UseAuthentication()
        app.UseAuthorization()

        If SecurityConfig.IsDocsEnabled(app.Configuration) Then
            ConfigureDocumentation(app)
        End If

        app.MapControllers()
    End Sub

    ''' <summary>Swagger UI en /api-docs y documento OpenAPI en /api-docs.json.</summary>
    Private Shared Sub ConfigureDocumentation(app As WebApplication)
        app.UseSwagger()
        app.UseSwaggerUI(
            Sub(options)
                options.RoutePrefix = "api-docs"
                options.SwaggerEndpoint("/api-docs.json", "Games API")
            End Sub)

        app.MapGet("/api-docs.json",
            Async Function(context As HttpContext) As Task
                Dim provider = context.RequestServices.GetRequiredService(Of ISwaggerProvider)()
                Dim document = provider.GetSwagger("v1")
                Dim json = document.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0)

                context.Response.ContentType = "application/json; charset=utf-8"
                Await context.Response.WriteAsync(json)
            End Function)
    End Sub

    ''' <summary>Opciones del rate limiter global (300 peticiones / 15 min por IP).</summary>
    Public Shared Function ApiRateLimitOptions(config As IConfiguration) As RateLimitOptions
        Return New RateLimitOptions With {
            .windowSeconds = SecurityConfig.GetPositiveInt(config, "API_RATE_LIMIT_WINDOW_MS", SecurityConfig.DefaultApiWindowSeconds * 1000) \ 1000,
            .limit = SecurityConfig.GetPositiveInt(config, "API_RATE_LIMIT_MAX", SecurityConfig.DefaultApiMax),
            .message = SecurityConfig.ApiRateLimitMessage,
            .pathPrefix = "/api",
            .trustProxy = Boolean.Parse(SecurityConfig.GetValue(config, "TRUST_PROXY", "false"))
        }
    End Function

    ''' <summary>Opciones del rate limiter del login (5 intentos / 15 min por IP).</summary>
    Public Shared Function LoginRateLimitOptions(config As IConfiguration) As RateLimitOptions
        Return New RateLimitOptions With {
            .windowSeconds = SecurityConfig.GetPositiveInt(config, "AUTH_RATE_LIMIT_WINDOW_MS", SecurityConfig.DefaultAuthWindowSeconds * 1000) \ 1000,
            .limit = SecurityConfig.GetPositiveInt(config, "AUTH_RATE_LIMIT_MAX", SecurityConfig.DefaultAuthMax),
            .message = SecurityConfig.LoginRateLimitMessage,
            .pathPrefix = "/api/auth/login",
            .method = "POST",
            .trustProxy = Boolean.Parse(SecurityConfig.GetValue(config, "TRUST_PROXY", "false"))
        }
    End Function

    ''' <summary>
    ''' Resuelve el connection string: primero "ConnectionStrings:GameDb", luego
    ''' "ConnectionStrings:MariaDB" (la clave que ya usa appsettings.json) y, si
    ''' ninguna existe, el valor por defecto historico.
    ''' </summary>
    Public Shared Function ResolveConnectionString(config As IConfiguration) As String
        Dim configured = config.GetConnectionString("GameDb")

        If String.IsNullOrWhiteSpace(configured) Then
            configured = config.GetConnectionString("MariaDB")
        End If

        If Not String.IsNullOrWhiteSpace(configured) Then
            Return configured
        End If

        Return DefaultConnectionString
    End Function

    ''' <summary>
    ''' Resuelve la version del servidor MySQL: si "Database:ServerVersion" esta
    ''' configurada se usa tal cual (sirve para pruebas / CI) y, si no, se autodetecta.
    ''' </summary>
    Public Shared Function ResolveServerVersion(config As IConfiguration, connectionString As String) As ServerVersion
        Dim configuredVersion = config("Database:ServerVersion")

        If Not String.IsNullOrWhiteSpace(configuredVersion) Then
            Return New MySqlServerVersion(Version.Parse(configuredVersion))
        End If

        Return ServerVersion.AutoDetect(connectionString)
    End Function

    ''' <summary>Aplica el provider de MySQL al DbContext de la aplicacion.</summary>
    Public Shared Sub ConfigureDatabase(options As DbContextOptionsBuilder, config As IConfiguration)
        Dim connectionString = ResolveConnectionString(config)

        ' Node no pone timeout a las consultas y el catálogo remoto es grande
        ' (~1,4 GB): se amplía el timeout por defecto de 30 s (las agregaciones de /api/stats recorren la tabla).
        options.UseMySql(
            connectionString,
            ResolveServerVersion(config, connectionString),
            Sub(mySqlOptions) mySqlOptions.CommandTimeout(300))
    End Sub

End Class