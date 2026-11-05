Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Hosting
Imports Microsoft.AspNetCore.Mvc.Testing
Imports Microsoft.AspNetCore.TestHost
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.DependencyInjection.Extensions
Imports Xunit

''' <summary>
''' Host de pruebas: aplica la configuración real (JWT/CORS/rate limit) pero
''' sustituye MySQL por una base InMemory sembrada por cada prueba.
''' </summary>
Public Class ApiWebFactory
    Inherits WebApplicationFactory(Of Program)

    Public Const TestSecret As String = "7456dd0b8487a4477dd5ef9078d8f9c264f82301e034f669bffa31e2b7dfe0d8"
    Public Const TestPassword As String = "Dev-aekiu4-9mHsG"

    Private ReadOnly _databaseName As String = Guid.NewGuid().ToString()
    Private ReadOnly _overrides As Dictionary(Of String, String)

    Public Sub New(Optional configOverrides As Dictionary(Of String, String) = Nothing)
        _overrides = If(configOverrides, New Dictionary(Of String, String)())
    End Sub

    Protected Overrides Sub ConfigureWebHost(builder As IWebHostBuilder)
        builder.UseEnvironment("Development")

        builder.ConfigureAppConfiguration(
            Sub(context, config)
                Dim values = New Dictionary(Of String, String) From {
                    {"JWT_SECRET", TestSecret},
                    {"JWT_EXPIRES_IN", "1h"},
                    {"JWT_ISSUER", "rss-game-node"},
                    {"JWT_AUDIENCE", "rss-game-node-api"},
                    {"AUTH_USERNAME", "admin"},
                    {"AUTH_ROLE", "admin"},
                    {"AUTH_PASSWORD_HASH", BCrypt.Net.BCrypt.HashPassword(TestPassword)},
                    {"API_RATE_LIMIT_MAX", "10000"},
                    {"AUTH_RATE_LIMIT_MAX", "1000"},
                    {"API_DOCS_ENABLED", "true"},
                    {"Database:ServerVersion", "8.0.36"}
                }

                For Each item In _overrides
                    values(item.Key) = item.Value
                Next

                config.AddInMemoryCollection(values)
            End Sub)

        builder.ConfigureTestServices(
            Sub(services)
                ' Se sustituye el DbContext MySQL por InMemory.
                services.RemoveAll(Of DbContextOptions(Of AppDbContext))()
                services.RemoveAll(Of DbContextOptions)()
                services.RemoveAll(Of AppDbContext)()
                services.AddDbContext(Of AppDbContext)(
                    Sub(options) options.UseInMemoryDatabase(_databaseName))
            End Sub)
    End Sub

    ''' <summary>Siembra el catálogo de prueba en la base InMemory.</summary>
    Public Sub Seed()
        Using scope = Services.CreateScope()
            SeedCatalog(scope.ServiceProvider.GetRequiredService(Of AppDbContext)())
        End Using
    End Sub

    ''' <summary>Emite un JWT con el mismo esquema configurado en el host.</summary>
    Public Function CreateToken(Optional username As String = "admin",
                                Optional role As String = "admin") As String
        Dim config = Services.GetRequiredService(Of IConfiguration)()

        Return New AuthService(config).IssueToken(
            New AuthUser With {.username = username, .role = role}).token
    End Function
End Class

''' <summary>Pruebas HTTP de integración de todos los endpoints y la seguridad.</summary>
Public Class ApiIntegrationTests

    Private Shared Function Json(content As String) As StringContent
        Return New StringContent(content, Encoding.UTF8, "application/json")
    End Function

    Private Shared Function LoginBody() As String
        Return "{""username"":""admin"",""password"":""" & ApiWebFactory.TestPassword & """}"
    End Function

    Private Shared Sub Authorize(client As HttpClient, token As String)
        client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)
    End Sub

    ' ==========================================
    ' Autenticación
    ' ==========================================

    <Fact>
    Public Async Function Login_ReturnsTokenAndNoStoreHeaders() As Task
        Using factory = New ApiWebFactory()
            Dim client = factory.CreateClient()

            Dim response = Await client.PostAsync("/api/auth/login", Json(LoginBody()))

            Assert.Equal(HttpStatusCode.OK, response.StatusCode)
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString())
            Dim body = Await response.Content.ReadAsStringAsync()
            Assert.Contains("""token""", body)
            Assert.Contains("""Bearer""", body)
        End Using
    End Function

    <Fact>
    Public Async Function Login_WithInvalidOrMissingCredentials_ReturnsError() As Task
        Using factory = New ApiWebFactory()
            Dim client = factory.CreateClient()

            Dim invalid = Await client.PostAsync("/api/auth/login",
                Json("{""username"":""admin"",""password"":""mal""}"))
            Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode)
            Assert.Contains("Credenciales inválidas", Await invalid.Content.ReadAsStringAsync())

            Dim missing = Await client.PostAsync("/api/auth/login", Json("{}"))
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode)
            Assert.Contains("obligatorios", Await missing.Content.ReadAsStringAsync())

            Dim tooLong = Await client.PostAsync("/api/auth/login",
                Json("{""username"":""" & New String("a"c, 200) & """,""password"":""x""}"))
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode)
        End Using
    End Function

    <Fact>
    Public Async Function AuthMe_RequiresValidToken() As Task
        Using factory = New ApiWebFactory()
            factory.Seed()
            Dim client = factory.CreateClient()
            Authorize(client, factory.CreateToken())

            Dim meResponse = Await client.GetAsync("/api/auth/me")
            Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode)
            Dim body = Await meResponse.Content.ReadAsStringAsync()
            Assert.Contains("admin", body)
            Assert.Contains("role", body)

            client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", "token-invalido")
            Dim invalid = Await client.GetAsync("/api/auth/me")
            Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode)
            Assert.Equal("Bearer", invalid.Headers.WwwAuthenticate.ToString())
        End Using
    End Function

    ' ==========================================
    ' Catálogo
    ' ==========================================

    <Fact>
    Public Async Function Games_RequireJwtAndReturnPaginatedContract() As Task
        Using factory = New ApiWebFactory()
            factory.Seed()
            Dim client = factory.CreateClient()

            Dim anonymous = Await client.GetAsync("/api/games?platform=PS5")
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode)

            Authorize(client, factory.CreateToken())

            Dim byPlatform = Await client.GetAsync("/api/games?platform=PS5&page=1&limit=10")
            Assert.Equal(HttpStatusCode.OK, byPlatform.StatusCode)
            Dim payload = Await byPlatform.Content.ReadAsStringAsync()
            Assert.Contains("""pagination""", payload)
            Assert.Contains("""totalRecords"":2", payload.Replace(" ", ""))

            Dim filtered = Await client.GetAsync("/api/games?genre=RPG&sort=title_asc")
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode)

            Dim search = Await client.GetAsync("/api/games/search?title=Bet")
            Assert.Equal(HttpStatusCode.OK, search.StatusCode)

            Dim missingTitle = Await client.GetAsync("/api/games/search")
            Assert.Equal(HttpStatusCode.BadRequest, missingTitle.StatusCode)
            Assert.Contains("requerido", Await missingTitle.Content.ReadAsStringAsync())

            Dim random = Await client.GetAsync("/api/games/random?platform=PS5")
            Assert.Equal(HttpStatusCode.OK, random.StatusCode)
            Assert.Contains("platform", Await random.Content.ReadAsStringAsync())

            Dim detail = Await client.GetAsync("/api/games/1")
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode)
            Assert.Contains("""Platform""", Await detail.Content.ReadAsStringAsync())

            Dim notFound = Await client.GetAsync("/api/games/999")
            Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode)
            Assert.Contains("Juego no encontrado", Await notFound.Content.ReadAsStringAsync())
        End Using
    End Function

    <Fact>
    Public Async Function SimilarGames_ReturnContractOrNotFound() As Task
        Using factory = New ApiWebFactory()
            factory.Seed()
            Dim client = factory.CreateClient()
            Authorize(client, factory.CreateToken())

            Dim similar = Await client.GetAsync("/api/games/1/similar?limit=1")
            Assert.Equal(HttpStatusCode.OK, similar.StatusCode)
            Assert.Contains("similarGames", Await similar.Content.ReadAsStringAsync())

            Dim missing = Await client.GetAsync("/api/games/999/similar")
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode)
            Assert.Contains("No se encontraron juegos similares", Await missing.Content.ReadAsStringAsync())
        End Using
    End Function

    <Fact>
    Public Async Function PlatformsAndTopGames_ReturnNodeContract() As Task
        Using factory = New ApiWebFactory()
            factory.Seed()
            Dim client = factory.CreateClient()
            Authorize(client, factory.CreateToken())

            Dim platforms = Await client.GetAsync("/api/platforms")
            Assert.Equal(HttpStatusCode.OK, platforms.StatusCode)
            Assert.Contains("PS5", Await platforms.Content.ReadAsStringAsync())

            Dim platform = Await client.GetAsync("/api/platforms/1")
            Assert.Equal(HttpStatusCode.OK, platform.StatusCode)

            Dim topGames = Await client.GetAsync("/api/platforms/1/top-games")
            Assert.Equal(HttpStatusCode.OK, topGames.StatusCode)
            Assert.Contains("metacritic_score", Await topGames.Content.ReadAsStringAsync())

            Dim missing = Await client.GetAsync("/api/platforms/999")
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode)
            Assert.Contains("Plataforma no encontrada", Await missing.Content.ReadAsStringAsync())
        End Using
    End Function

    <Fact>
    Public Async Function Reviews_ReturnPaginatedRowsOrNotFound() As Task
        Using factory = New ApiWebFactory()
            factory.Seed()
            Dim client = factory.CreateClient()
            Authorize(client, factory.CreateToken())

            Dim reviews = Await client.GetAsync("/api/reviews/1")
            Assert.Equal(HttpStatusCode.OK, reviews.StatusCode)
            Assert.Contains("pagination", Await reviews.Content.ReadAsStringAsync())

            Dim missing = Await client.GetAsync("/api/reviews/999")
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode)
            Assert.Contains("Reseña no encontrada", Await missing.Content.ReadAsStringAsync())
        End Using
    End Function

    <Fact>
    Public Async Function Stats_ReturnSummaryDistributions() As Task
        StatsService.ClearCache()

        Using factory = New ApiWebFactory()
            factory.Seed()
            Dim client = factory.CreateClient()
            Authorize(client, factory.CreateToken())

            Dim response = Await client.GetAsync("/api/stats")

            Assert.Equal(HttpStatusCode.OK, response.StatusCode)
            Dim body = Await response.Content.ReadAsStringAsync()
            Assert.Contains("totalGames", body)
            Assert.Contains("platforms", body)
            Assert.Contains("genres", body)
            Assert.Contains("decades", body)
        End Using
    End Function

    ' ==========================================
    ' Escrituras y roles
    ' ==========================================

    <Fact>
    Public Async Function GameWrites_RequireAdminRole() As Task
        Using factory = New ApiWebFactory()
            factory.Seed()
            Dim client = factory.CreateClient()

            Dim anonymous = Await client.PostAsync("/api/games", Json("{""title"":""x""}"))
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode)

            Authorize(client, factory.CreateToken(role:="user"))
            Dim forbidden = Await client.PostAsync("/api/games", Json("{""title"":""x""}"))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode)
            Assert.Contains("Acceso denegado", Await forbidden.Content.ReadAsStringAsync())
        End Using
    End Function

    <Fact>
    Public Async Function GameCrud_WorksForAdmin() As Task
        Using factory = New ApiWebFactory()
            factory.Seed()
            Dim client = factory.CreateClient()
            Authorize(client, factory.CreateToken())

            Dim created = Await client.PostAsync("/api/games",
                Json("{""title"":""Nuevo"",""Platform"":""PC""}"))
            Assert.Equal(HttpStatusCode.Created, created.StatusCode)
            Assert.Contains("Nuevo", Await created.Content.ReadAsStringAsync())

            Dim updated = Await client.PutAsync("/api/games/6",
                Json("{""title"":""Actualizado""}"))
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode)
            Assert.Contains("Juego actualizado", Await updated.Content.ReadAsStringAsync())

            Dim deleted = Await client.DeleteAsync("/api/games/6")
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode)
            Assert.Contains("Juego eliminado", Await deleted.Content.ReadAsStringAsync())

            Dim again = Await client.DeleteAsync("/api/games/6")
            Assert.Equal(HttpStatusCode.NotFound, again.StatusCode)

            Dim missingUpdate = Await client.PutAsync("/api/games/999", Json("{""title"":""x""}"))
            Assert.Equal(HttpStatusCode.NotFound, missingUpdate.StatusCode)
        End Using
    End Function

    <Fact>
    Public Async Function PlatformCrud_WorksForAdmin() As Task
        Using factory = New ApiWebFactory()
            factory.Seed()
            Dim client = factory.CreateClient()
            Authorize(client, factory.CreateToken())

            Dim created = Await client.PostAsync("/api/platforms",
                Json("{""description"":""Nueva"",""url"":""/nueva""}"))
            Assert.Equal(HttpStatusCode.Created, created.StatusCode)

            Dim updated = Await client.PutAsync("/api/platforms/5",
                Json("{""description"":""Actualizada""}"))
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode)
            Assert.Contains("Plataforma actualizada", Await updated.Content.ReadAsStringAsync())

            Dim deleted = Await client.DeleteAsync("/api/platforms/5")
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode)

            Dim missing = Await client.PutAsync("/api/platforms/999", Json("{""description"":""x""}"))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode)
        End Using
    End Function

    ' ==========================================
    ' Middlewares globales
    ' ==========================================

    <Fact>
    Public Async Function SecurityHeaders_404DocsAndHello_BehaveLikeNode() As Task
        Using factory = New ApiWebFactory()
            Dim client = factory.CreateClient()

            Dim saludo = Await client.GetAsync("/api/saludo")
            Assert.Equal(HttpStatusCode.OK, saludo.StatusCode)
            Assert.Equal("nosniff", saludo.Headers.GetValues("X-Content-Type-Options").Single())
            Assert.True(saludo.Headers.Contains("Content-Security-Policy"))
            Assert.True(saludo.Headers.Contains("RateLimit-Limit"))

            Dim docs = Await client.GetAsync("/api-docs.json")
            Assert.Equal(HttpStatusCode.OK, docs.StatusCode)
            Assert.Contains("/api/games", Await docs.Content.ReadAsStringAsync())

            Dim ui = Await client.GetAsync("/api-docs")
            Assert.Equal(HttpStatusCode.OK, ui.StatusCode)
            Assert.Contains("swagger-ui", Await ui.Content.ReadAsStringAsync())

            Dim unknown = Await client.GetAsync("/api/no-existe")
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode)
            Assert.Contains("Ruta no encontrada", Await unknown.Content.ReadAsStringAsync())

            ' hello: el proveedor InMemory no tiene conexión relacional -> 500 JSON.
            Dim hello = Await client.GetAsync("/api/hello")
            Assert.Equal(HttpStatusCode.InternalServerError, hello.StatusCode)
            Assert.Contains("Error en la base de datos", Await hello.Content.ReadAsStringAsync())
        End Using
    End Function

    <Fact>
    Public Async Function BodyLimit_Returns413ForLargeBodies() As Task
        Using factory = New ApiWebFactory()
            Dim client = factory.CreateClient()

            Dim response = Await client.PostAsync("/api/auth/login",
                Json("{""username"":""admin"",""password"":""" & New String("x"c, 12000) & """}"))

            Assert.Equal(413, CInt(response.StatusCode))
            Assert.Contains("demasiado grande", Await response.Content.ReadAsStringAsync())
        End Using
    End Function

    <Fact>
    Public Async Function GlobalRateLimit_Returns429AfterLimit() As Task
        Using factory = New ApiWebFactory(New Dictionary(Of String, String) From {
            {"API_RATE_LIMIT_MAX", "2"}
        })
            Dim client = factory.CreateClient()

            Assert.Equal(HttpStatusCode.OK, (Await client.GetAsync("/api/saludo")).StatusCode)
            Assert.Equal(HttpStatusCode.OK, (Await client.GetAsync("/api/saludo")).StatusCode)
            Dim limited = Await client.GetAsync("/api/saludo")

            Assert.Equal(429, CInt(limited.StatusCode))
            Assert.True(limited.Headers.Contains("Retry-After"))
            Assert.Contains("Demasiadas solicitudes", Await limited.Content.ReadAsStringAsync())
        End Using
    End Function

    <Fact>
    Public Async Function LoginRateLimit_Returns429AfterLimit() As Task
        Using factory = New ApiWebFactory(New Dictionary(Of String, String) From {
            {"AUTH_RATE_LIMIT_MAX", "1"},
            {"API_RATE_LIMIT_MAX", "1000"}
        })
            Dim client = factory.CreateClient()
            Dim body = Json(LoginBody())
            Await client.PostAsync("/api/auth/login", body)

            Dim limited = Await client.PostAsync("/api/auth/login", body)

            Assert.Equal(429, CInt(limited.StatusCode))
            Assert.Contains("inicio de sesión", Await limited.Content.ReadAsStringAsync())
        End Using
    End Function
End Class
