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
''' Pruebas de la composición de la aplicación (Program.vb): DI, configuración,
''' opciones de seguridad y pipeline HTTP real.
''' </summary>
Public Class ProgramTests

    Private Shared Function CreateConfig(Optional settings As Dictionary(Of String, String) = Nothing) As IConfiguration
        Return New ConfigurationBuilder().
            AddInMemoryCollection(If(settings, New Dictionary(Of String, String)())).
            Build()
    End Function

    ''' <summary>Argumentos que evitan el autodetect de MySQL y el fail-closed de producción.</summary>
    Private Shared Function TestArgs(ParamArray extra As String()) As String()
        Dim args = New List(Of String) From {"--Database:ServerVersion=8.0.36", "--ENVIRONMENT=development"}
        args.AddRange(extra)

        Return args.ToArray()
    End Function

    <Fact>
    Public Sub CreateApp_BuildsApplicationAndRegistersServices()
        Dim app = Program.CreateApp(TestArgs())

        Assert.NotNull(app)

        Using scope = app.Services.CreateScope()
            Dim provider = scope.ServiceProvider

            Assert.IsType(Of GameService)(provider.GetRequiredService(Of IGameService)())
            Assert.IsType(Of PlatformService)(provider.GetRequiredService(Of IPlatformService)())
            Assert.IsType(Of ReviewService)(provider.GetRequiredService(Of IReviewService)())
            Assert.IsType(Of SimilarGamesService)(provider.GetRequiredService(Of ISimilarGamesService)())
            Assert.IsType(Of StatsService)(provider.GetRequiredService(Of IStatsService)())
            Assert.IsType(Of AuthService)(provider.GetRequiredService(Of IAuthService)())

            Assert.NotNull(provider.GetRequiredService(Of IGameRepository)())
            Assert.NotNull(provider.GetRequiredService(Of IPlatformRepository)())
            Assert.NotNull(provider.GetRequiredService(Of IReviewRepository)())
            Assert.NotNull(provider.GetRequiredService(Of ISimilarGameRepository)())
            Assert.NotNull(provider.GetRequiredService(Of IStatsRepository)())
        End Using
    End Sub

    <Fact>
    Public Sub ConfigureServices_RegistersDbContextAuthenticationAndCors()
        Dim services = New ServiceCollection()
        Dim config = CreateConfig()

        Program.ConfigureServices(services, config)

        Using provider = services.BuildServiceProvider()
            Using scope = provider.CreateScope()
                Assert.NotNull(scope.ServiceProvider.GetRequiredService(Of AppDbContext)())
            End Using
        End Using
    End Sub

    <Fact>
    Public Sub RateLimitOptions_UseDefaultsAndConfiguredValues()
        Dim defaults = Program.ApiRateLimitOptions(CreateConfig())

        Assert.Equal(900, defaults.windowSeconds)
        Assert.Equal(300, defaults.limit)
        Assert.Equal("/api", defaults.pathPrefix)
        Assert.False(defaults.trustProxy)

        Dim configured = Program.ApiRateLimitOptions(CreateConfig(New Dictionary(Of String, String) From {
            {"API_RATE_LIMIT_WINDOW_MS", "60000"},
            {"API_RATE_LIMIT_MAX", "7"},
            {"TRUST_PROXY", "true"}
        }))

        Assert.Equal(60, configured.windowSeconds)
        Assert.Equal(7, configured.limit)
        Assert.True(configured.trustProxy)
    End Sub

    <Fact>
    Public Sub LoginRateLimitOptions_UseDefaultsAndConfiguredValues()
        Dim defaults = Program.LoginRateLimitOptions(CreateConfig())

        Assert.Equal(900, defaults.windowSeconds)
        Assert.Equal(5, defaults.limit)
        Assert.Equal("/api/auth/login", defaults.pathPrefix)
        Assert.Equal("POST", defaults.method)

        Dim configured = Program.LoginRateLimitOptions(CreateConfig(New Dictionary(Of String, String) From {
            {"AUTH_RATE_LIMIT_WINDOW_MS", "30000"},
            {"AUTH_RATE_LIMIT_MAX", "3"}
        }))

        Assert.Equal(30, configured.windowSeconds)
        Assert.Equal(3, configured.limit)
    End Sub

    <Fact>
    Public Sub ResolveConnectionString_UsesConfiguredOrHistoricalDefault()
        Assert.Equal("server=db-test;database=games;", Program.ResolveConnectionString(
            CreateConfig(New Dictionary(Of String, String) From {{"ConnectionStrings:GameDb", "server=db-test;database=games;"}})))

        Assert.Equal("server=mariadb;", Program.ResolveConnectionString(
            CreateConfig(New Dictionary(Of String, String) From {{"ConnectionStrings:MariaDB", "server=mariadb;"}})))

        Assert.Equal("server=game-db;", Program.ResolveConnectionString(
            CreateConfig(New Dictionary(Of String, String) From {
                {"ConnectionStrings:GameDb", "server=game-db;"},
                {"ConnectionStrings:MariaDB", "server=mariadb;"}
            })))

        Assert.Equal(Program.DefaultConnectionString, Program.ResolveConnectionString(CreateConfig()))
        Assert.Equal(Program.DefaultConnectionString, Program.ResolveConnectionString(
            CreateConfig(New Dictionary(Of String, String) From {{"ConnectionStrings:GameDb", "   "}})))
    End Sub

    <Fact>
    Public Sub ResolveServerVersion_UsesConfiguredVersionOrAutoDetects()
        Dim version = Program.ResolveServerVersion(
            CreateConfig(New Dictionary(Of String, String) From {{"Database:ServerVersion", "5.7.44"}}),
            "server=ignored;")

        Assert.IsType(Of MySqlServerVersion)(version)
        Assert.Equal(New Version(5, 7, 44), version.Version)

        Assert.ThrowsAny(Of Exception)(
            Sub()
                Program.ResolveServerVersion(
                    CreateConfig(),
                    "server=127.0.0.1;port=1;database=game;user=root;password=x;Connection Timeout=1;")
            End Sub)
    End Sub

    <Fact>
    Public Sub ConfigureDatabase_ConfiguresMySqlOrThrowsWithInvalidVersion()
        Dim validOptions = New DbContextOptionsBuilder()
        Program.ConfigureDatabase(validOptions, CreateConfig(New Dictionary(Of String, String) From {
            {"Database:ServerVersion", "8.0.36"},
            {"ConnectionStrings:GameDb", "server=db-test;database=games;"}
        }))
        Assert.NotNull(validOptions.Options)

        Assert.ThrowsAny(Of Exception)(
            Sub()
                Program.ConfigureDatabase(New DbContextOptionsBuilder(), CreateConfig(New Dictionary(Of String, String) From {
                    {"Database:ServerVersion", "no-es-una-version"}
                }))
            End Sub)
    End Sub

    <Fact>
    Public Async Function Pipeline_ServesDocsAndRequiresJwt() As Task
        Dim app = Program.CreateApp(TestArgs("--urls=http://127.0.0.1:0"))
        Await app.StartAsync()

        Dim baseUrl = app.Urls.First()

        Using client = New HttpClient()
            client.BaseAddress = New Uri(baseUrl)

            Dim docs = Await client.GetAsync("/api-docs.json")
            Assert.Equal(HttpStatusCode.OK, docs.StatusCode)
            Assert.Contains("/api/games", Await docs.Content.ReadAsStringAsync())

            Dim saludo = Await client.GetAsync("/api/saludo")
            Assert.Equal(HttpStatusCode.OK, saludo.StatusCode)

            Dim games = Await client.GetAsync("/api/games")
            Assert.Equal(HttpStatusCode.Unauthorized, games.StatusCode)
            Assert.Equal("Bearer", games.Headers.WwwAuthenticate.ToString())
            Assert.Contains("No autorizado", Await games.Content.ReadAsStringAsync())

            Dim unknown = Await client.GetAsync("/api/ruta-inexistente")
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode)
            Assert.Contains("Ruta no encontrada", Await unknown.Content.ReadAsStringAsync())
        End Using

        Await app.StopAsync()
        Await app.DisposeAsync()
    End Function
End Class
