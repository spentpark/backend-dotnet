Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Net
Imports System.Net.Http
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Xunit

''' <summary>
''' Pruebas de la composicion de la aplicacion (Program.vb):
''' registro de dependencias, resolucion de configuracion y pipeline HTTP real.
''' </summary>
Public Class ProgramTests

    ''' <summary>Crea una IConfiguration en memoria con los valores indicados.</summary>
    Private Shared Function CreateConfig(Optional settings As Dictionary(Of String, String) = Nothing) As IConfiguration
        Dim values = If(settings, New Dictionary(Of String, String)())
        Return New ConfigurationBuilder().AddInMemoryCollection(values).Build()
    End Function

    ' ==========================================
    ''' <summary>Argumentos de linea de comandos que evitan el autodetect de MySQL.</summary>
    Private Shared Function TestArgs(ParamArray extra As String()) As String()
        Dim args = New List(Of String) From {"--Database:ServerVersion=8.0.36"}
        args.AddRange(extra)
        Return args.ToArray()
    End Function

    ' ==========================================
    ' CreateApp
    ' ==========================================

    <Fact>
    Public Sub CreateApp_BuildsApplicationWithoutStartingIt()
        Dim app = Program.CreateApp(TestArgs())

        Assert.NotNull(app)
        Assert.NotNull(app.Services)

        Using scope = app.Services.CreateScope()
            Assert.IsType(Of GameService)(scope.ServiceProvider.GetRequiredService(Of IGameService)())
        End Using
    End Sub

    <Fact>
    Public Async Function CreateApp_ServesSwaggerAndReturnsNotFoundForUnknownRoutes() As Task
        Dim app = Program.CreateApp(TestArgs("--urls=http://127.0.0.1:0"))

        Await app.StartAsync()
        Await AssertPipelineAsync(app)
        Await app.StopAsync()
        Await app.DisposeAsync()
    End Function

    ''' <summary>Verifica el pipeline HTTP expuesto por ConfigurePipeline.</summary>
    Private Shared Async Function AssertPipelineAsync(app As Microsoft.AspNetCore.Builder.WebApplication) As Task
        Dim baseUrl = app.Urls.First()

        Using client = New HttpClient()
            client.BaseAddress = New Uri(baseUrl)

            Dim swagger = Await client.GetAsync("/swagger/v1/swagger.json")
            Assert.Equal(HttpStatusCode.OK, swagger.StatusCode)

            Dim document = Await swagger.Content.ReadAsStringAsync()
            Assert.Contains("/api/games", document)
            Assert.Contains("/api/platforms", document)
            Assert.Contains("/api/reviews", document)

            Dim unknown = Await client.GetAsync("/api/ruta-inexistente")
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode)
        End Using
    End Function

    ' ==========================================
    ' ConfigureServices
    ' ==========================================

    <Fact>
    Public Sub ConfigureServices_RegistersAllRepositoriesAndServices()
        Dim services = New ServiceCollection()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"Database:ServerVersion", "8.0.36"}
        })

        Program.ConfigureServices(services, config)

        Using provider = services.BuildServiceProvider()
            Using scope = provider.CreateScope()
                Dim sp = scope.ServiceProvider

                Assert.IsType(Of GameRepository)(sp.GetRequiredService(Of IGameRepository)())
                Assert.IsType(Of PlatformRepository)(sp.GetRequiredService(Of IPlatformRepository)())
                Assert.IsType(Of ReviewRepository)(sp.GetRequiredService(Of IReviewRepository)())

                Assert.IsType(Of GameService)(sp.GetRequiredService(Of IGameService)())
                Assert.IsType(Of PlatformService)(sp.GetRequiredService(Of IPlatformService)())
                Assert.IsType(Of ReviewService)(sp.GetRequiredService(Of IReviewService)())
            End Using
        End Using
    End Sub

    <Fact>
    Public Sub ConfigureServices_RegistersDbContextWithMySqlProvider()
        Dim services = New ServiceCollection()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"Database:ServerVersion", "8.0.36"}
        })

        Program.ConfigureServices(services, config)

        Using provider = services.BuildServiceProvider()
            Using scope = provider.CreateScope()
                Dim context = scope.ServiceProvider.GetRequiredService(Of AppDbContext)()

                ' El contexto se construye sin abrir conexion (la version es explicita).
                Assert.NotNull(context)
                Assert.Contains("MySql", context.Database.ProviderName, StringComparison.OrdinalIgnoreCase)
            End Using
        End Using
    End Sub

    <Fact>
    Public Sub ConfigureServices_RegistersControllers()
        Dim services = New ServiceCollection()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"Database:ServerVersion", "8.0.36"}
        })

        Program.ConfigureServices(services, config)

        Using provider = services.BuildServiceProvider()
            Dim activator = provider.GetService(Of Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator)()
            Assert.NotNull(activator)
        End Using
    End Sub

    ' ==========================================
    ' ResolveConnectionString
    ' ==========================================

    <Fact>
    Public Sub ResolveConnectionString_ReturnsConfiguredValue()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"ConnectionStrings:GameDb", "server=db-test;database=games;"}
        })

        Assert.Equal("server=db-test;database=games;", Program.ResolveConnectionString(config))
    End Sub

    <Fact>
    Public Sub ResolveConnectionString_WhenMissing_ReturnsDefaultValue()
        Dim config = CreateConfig()

        Dim result = Program.ResolveConnectionString(config)

        Assert.Equal(Program.DefaultConnectionString, result)
        Assert.Contains("database=game", result)
    End Sub

    <Fact>
    Public Sub ResolveConnectionString_WhenBlank_ReturnsDefaultValue()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"ConnectionStrings:GameDb", "   "}
        })

        Assert.Equal(Program.DefaultConnectionString, Program.ResolveConnectionString(config))
    End Sub

    <Fact>
    Public Sub ResolveConnectionString_FallsBackToMariaDbKeyFromAppSettings()
        ' appsettings.json define ConnectionStrings:MariaDB; esa clave tambien se respeta.
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"ConnectionStrings:MariaDB", "server=192.168.1.82;database=game;"}
        })

        Assert.Equal("server=192.168.1.82;database=game;", Program.ResolveConnectionString(config))
    End Sub

    <Fact>
    Public Sub ResolveConnectionString_GameDbHasPriorityOverMariaDb()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"ConnectionStrings:GameDb", "server=game-db;"},
            {"ConnectionStrings:MariaDB", "server=mariadb;"}
        })

        Assert.Equal("server=game-db;", Program.ResolveConnectionString(config))
    End Sub

    ' ==========================================
    ' ResolveServerVersion
    ' ==========================================

    <Fact>
    Public Sub ResolveServerVersion_WithConfiguredVersion_ReturnsThatVersion()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"Database:ServerVersion", "5.7.44"}
        })

        Dim version = Program.ResolveServerVersion(config, "server=ignored;")

        Assert.IsType(Of MySqlServerVersion)(version)
        Assert.Equal(New Version(5, 7, 44), version.Version)
    End Sub

    <Fact>
    Public Sub ResolveServerVersion_WithoutConfiguration_TriesToConnectAndFails()
        ' Sin "Database:ServerVersion" el codigo autodetecta contra el servidor.
        ' Apuntamos a un puerto cerrado en loopback: el fallo es inmediato y deterministico.
        Dim config = CreateConfig()
        Dim unreachable = "server=127.0.0.1;port=1;database=game;user=root;password=x;Connection Timeout=1;"

        Assert.ThrowsAny(Of Exception)(Sub()
            Dim ignored = Program.ResolveServerVersion(config, unreachable)
        End Sub)
    End Sub

    ' ==========================================
    ' ConfigureDatabase
    ' ==========================================

    <Fact>
    Public Sub ConfigureDatabase_WithInvalidServerVersion_Throws()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"Database:ServerVersion", "no-es-una-version"}
        })
        Dim options = New DbContextOptionsBuilder()

        Assert.ThrowsAny(Of Exception)(Sub() Program.ConfigureDatabase(options, config))
    End Sub

    <Fact>
    Public Sub ConfigureDatabase_ConfiguresMySqlOptions()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"Database:ServerVersion", "8.0.36"},
            {"ConnectionStrings:GameDb", "server=db-test;database=games;"}
        })
        Dim options = New DbContextOptionsBuilder()

        Program.ConfigureDatabase(options, config)

        Assert.NotNull(options.Options)
    End Sub

End Class
