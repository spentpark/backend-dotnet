Imports System
Imports System.Linq
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Mvc.ApplicationParts
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.EntityFrameworkCore

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

    ''' <summary>Registra controladores, Swagger, EF Core y la capa de repositorios/servicios.</summary>
    Public Shared Sub ConfigureServices(services As IServiceCollection, config As IConfiguration)
        Dim mvc = services.AddControllers()
        RegisterApplicationPart(mvc)
        services.AddEndpointsApiExplorer()
        services.AddSwaggerGen()

        services.AddDbContext(Of AppDbContext)(Sub(options) ConfigureDatabase(options, config))

        services.AddScoped(Of IGameRepository, GameRepository)()
        services.AddScoped(Of IGameService, GameService)()
        services.AddScoped(Of IPlatformRepository, PlatformRepository)()
        services.AddScoped(Of IPlatformService, PlatformService)()
        services.AddScoped(Of IReviewRepository, ReviewRepository)()
        services.AddScoped(Of IReviewService, ReviewService)()
    End Sub

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

    ''' <summary>Configura el middleware HTTP y el mapeo de controladores.</summary>
    Public Shared Sub ConfigurePipeline(app As WebApplication)
        app.UseSwagger()
        app.UseSwaggerUI()
        app.MapControllers()
    End Sub

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
        options.UseMySql(connectionString, ResolveServerVersion(config, connectionString))
    End Sub

End Class
