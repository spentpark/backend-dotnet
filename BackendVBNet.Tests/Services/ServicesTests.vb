Imports System
Imports System.Collections.Generic
Imports System.IdentityModel.Tokens.Jwt
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports Microsoft.Extensions.Configuration
Imports Microsoft.IdentityModel.Tokens
Imports Moq
Imports Xunit

''' <summary>Pruebas de los servicios de negocio (repositorios simulados).</summary>
Public Class GameServiceTests

    Private Shared Function Filters() As GameFilters
        Return New GameFilters With {.platform = "PS5"}
    End Function

    <Fact>
    Public Async Function GetGamesAsync_NormalizesPagination() As Task
        Dim repository = New Mock(Of IGameRepository)()
        Dim expected = PaginatedResult(Of GameSummary).Create(New List(Of GameSummary)(), 0, 1, 10)
        repository.Setup(Function(r) r.FindSummariesPaginatedAsync(It.IsAny(Of GameFilters)(), 1, 10)).
            ReturnsAsync(expected)

        Dim service = New GameService(repository.Object)
        Dim result = Await service.GetGamesAsync(Filters(), 0, -5)

        Assert.Same(expected, result)
    End Function

    <Fact>
    Public Async Function SearchGamesAsync_NormalizesPagination() As Task
        Dim repository = New Mock(Of IGameRepository)()
        Dim expected = PaginatedResult(Of GameSummary).Create(New List(Of GameSummary)(), 0, 2, 5)
        repository.Setup(Function(r) r.FindByTitlePaginatedAsync("Zelda", 2, 5)).
            ReturnsAsync(expected)

        Dim service = New GameService(repository.Object)

        Assert.Same(expected, Await service.SearchGamesAsync("Zelda", 2, 5))
    End Function

    <Fact>
    Public Async Function GetRandomGameAsync_MapsEveryField() As Task
        Dim repository = New Mock(Of IGameRepository)()
        repository.Setup(Function(r) r.FindRandomAsync(It.IsAny(Of GameFilters)())).ReturnsAsync(
            New Game With {
                .Id = 7, .Title = "T", .Description = "D", .Image_Medium = "im",
                .Image_Large = "il", .Image_Original = "io", .Image_Front = "if",
                .Platform = "PS5", .Publisher = "Pub", .ReleaseDate = "2001-01-01",
                .Players = "1", .Genre = "RPG", .Youtube_Trailer = "yt"
            })

        Dim service = New GameService(repository.Object)
        Dim game = Await service.GetRandomGameAsync(Filters())

        Assert.Equal(7, game.id)
        Assert.Equal("T", game.title)
        Assert.Equal("D", game.description)
        Assert.Equal("im", game.imageMedium)
        Assert.Equal("il", game.imageLarge)
        Assert.Equal("io", game.imageOriginal)
        Assert.Equal("if", game.imageFront)
        Assert.Equal("PS5", game.platform)
        Assert.Equal("Pub", game.publisher)
        Assert.Equal("2001-01-01", game.releasedate)
        Assert.Equal("1", game.players)
        Assert.Equal("RPG", game.genre)
        Assert.Equal("yt", game.youtubeTrailer)
    End Function

    <Fact>
    Public Async Function GetRandomGameAsync_WhenRepositoryReturnsNothing_ReturnsNothing() As Task
        Dim repository = New Mock(Of IGameRepository)()
        repository.Setup(Function(r) r.FindRandomAsync(It.IsAny(Of GameFilters)())).ReturnsAsync(CType(Nothing, Game))

        Dim service = New GameService(repository.Object)

        Assert.Null(Await service.GetRandomGameAsync(Filters()))
    End Function

    <Fact>
    Public Async Function CrudMethods_DelegateOnRepository() As Task
        Dim repository = New Mock(Of IGameRepository)()
        Dim game = New Game With {.Id = 3, .Title = "Juego"}
        repository.Setup(Function(r) r.GetByIdAsync(3)).ReturnsAsync(game)
        repository.Setup(Function(r) r.CreateAsync(game)).ReturnsAsync(game)
        repository.Setup(Function(r) r.UpdateAsync(3, game)).ReturnsAsync(True)
        repository.Setup(Function(r) r.DeleteAsync(3)).ReturnsAsync(True)

        Dim service = New GameService(repository.Object)

        Assert.Same(game, Await service.GetGameByIdAsync(3))
        Assert.Same(game, Await service.CreateGameAsync(game))
        Assert.True(Await service.UpdateGameAsync(3, game))
        Assert.True(Await service.DeleteGameAsync(3))
    End Function
End Class

Public Class GameFiltersTests

    <Fact>
    Public Sub Helpers_DetectAdvancedRandomAndMissingPlatform()
        Assert.False(New GameFilters With {.platform = "PS5"}.HasAdvancedFilters())
        Assert.True(New GameFilters With {.genre = "RPG"}.HasAdvancedFilters())
        Assert.True(New GameFilters With {.year = "2000"}.HasAdvancedFilters())
        Assert.True(New GameFilters With {.decade = "199"}.HasAdvancedFilters())
        Assert.True(New GameFilters With {.players = "1"}.HasAdvancedFilters())
        Assert.True(New GameFilters With {.sort = "id_asc"}.HasAdvancedFilters())

        Assert.False(New GameFilters().HasRandomFilters())
        Assert.True(New GameFilters With {.platform = "PS5"}.HasRandomFilters())
        Assert.True(New GameFilters With {.genre = "RPG"}.HasRandomFilters())

        Assert.True(New GameFilters().MatchesNothing())
        Assert.False(New GameFilters With {.platform = "PS5"}.MatchesNothing())
    End Sub
End Class

Public Class PlatformServiceTests

    <Fact>
    Public Async Function Methods_DelegateOnRepository() As Task
        Dim repository = New Mock(Of IPlatformRepository)()
        Dim platform = New Platform With {.Id = 1, .Description = "PS5", .Url = "x"}
        Dim summary = New List(Of PlatformSummary) From {New PlatformSummary With {.id = 1}}
        Dim top = New List(Of TopGame) From {New TopGame With {.id = 1}}

        repository.Setup(Function(r) r.GetAllWithUrlAsync()).ReturnsAsync(summary)
        repository.Setup(Function(r) r.GetByIdAsync(1)).ReturnsAsync(platform)
        repository.Setup(Function(r) r.GetTopGamesAsync(1)).ReturnsAsync(top)
        repository.Setup(Function(r) r.CreateAsync(platform)).ReturnsAsync(platform)
        repository.Setup(Function(r) r.UpdateAsync(1, platform)).ReturnsAsync(True)
        repository.Setup(Function(r) r.DeleteAsync(1)).ReturnsAsync(True)

        Dim service = New PlatformService(repository.Object)

        Assert.Same(summary, Await service.GetPlatformsAsync())
        Assert.Same(platform, Await service.GetPlatformByIdAsync(1))
        Assert.Same(top, Await service.GetTopGamesAsync(1))
        Assert.Same(platform, Await service.CreatePlatformAsync(platform))
        Assert.True(Await service.UpdatePlatformAsync(1, platform))
        Assert.True(Await service.DeletePlatformAsync(1))
    End Function
End Class

Public Class ReviewServiceTests

    <Fact>
    Public Async Function GetReviewsAsync_NormalizesPagination() As Task
        Dim repository = New Mock(Of IReviewRepository)()
        Dim expected = PaginatedResult(Of Review).Create(New List(Of Review)(), 0, 1, 10)
        repository.Setup(Function(r) r.FindByGameIdPaginatedAsync(4, 1, 10)).ReturnsAsync(expected)

        Dim service = New ReviewService(repository.Object)

        Assert.Same(expected, Await service.GetReviewsAsync(4, 0, -1))
    End Function
End Class

Public Class SimilarGamesServiceTests

    Private Shared Function CreateService(json As String) As SimilarGamesService
        Dim repository = New Mock(Of ISimilarGameRepository)()
        repository.Setup(Function(r) r.FindByGameIdAsync(1)).
            ReturnsAsync(New SimilarGame With {.IdGame = 1, .Json = json})

        Return New SimilarGamesService(repository.Object)
    End Function

    <Fact>
    Public Async Function GetSimilarGamesAsync_ExcludesSelfOrdersAndLimits() As Task
        Dim service = CreateService(
            "[{""id"":1,""title"":""Self"",""similarity"":1.0}," &
            "{""id"":3,""title"":""C"",""similarity"":0.2}," &
            "{""id"":2,""title"":""B"",""similarity"":0.8}," &
            "{""id"":4,""title"":""D"",""similarity"":0.1}]")

        Dim result = Await service.GetSimilarGamesAsync(1, 2)

        Assert.Equal(1, result.gameId)
        Assert.Equal(2, result.total)
        Assert.Equal("B", result.similarGames(0).title)
        Assert.Equal("C", result.similarGames(1).title)
        Assert.Equal(80.0, result.similarGames(0).similarityPercentage)
    End Function

    <Fact>
    Public Async Function GetSimilarGamesAsync_WithoutLimit_ReturnsAll() As Task
        Dim service = CreateService(
            "[{""id"":2,""title"":""B"",""similarity"":0.8}," &
            "{""id"":3,""title"":""C"",""similarity"":0.2}]")

        Dim result = Await service.GetSimilarGamesAsync(1, Nothing)

        Assert.Equal(2, result.total)
    End Function

    <Fact>
    Public Async Function GetSimilarGamesAsync_WhenNoSimilarRemain_ReturnsNothing() As Task
        Dim service = CreateService("[{""id"":1,""title"":""Self"",""similarity"":1.0}]")

        Assert.Null(Await service.GetSimilarGamesAsync(1, Nothing))
    End Function

    <Fact>
    Public Async Function GetSimilarGamesAsync_WhenNoRow_ReturnsNothing() As Task
        Dim repository = New Mock(Of ISimilarGameRepository)()
        repository.Setup(Function(r) r.FindByGameIdAsync(99)).ReturnsAsync(CType(Nothing, SimilarGame))

        Dim service = New SimilarGamesService(repository.Object)

        Assert.Null(Await service.GetSimilarGamesAsync(99, Nothing))
    End Function
End Class

Public Class JsonParsingTests

    <Fact>
    Public Sub ParseSimilarGames_SkipsCorruptEntriesAndClampsPercentage()
        Dim entries = SimilarGamesService.ParseSimilarGames(
            "[{""id"":1,""title"":" & """" & "Uno" & """" & ",""similarity"":1.5}," &
            "{""id"":2,""similarity"":-0.2}," &
            "{""id"":""texto"",""similarity"":0.4}," &
            "{""id"":3,""similarity"":""0.4""}," &
            """cadena""," &
            "{""similarity"":0.3}," &
            "{""id"":4}]")

        Assert.Equal(2, entries.Count)
        Assert.Equal(100.0, entries(0).similarityPercentage)
        Assert.Equal("Uno", entries(0).title)
        Assert.Equal(0.0, entries(1).similarityPercentage)
        Assert.Null(entries(1).title)
    End Sub

    <Fact>
    Public Sub ParseSimilarGames_WithEmptyText_ReturnsEmpty() 
        Assert.Empty(SimilarGamesService.ParseSimilarGames(""))
        Assert.Empty(SimilarGamesService.ParseSimilarGames(Nothing))
    End Sub

    <Fact>
    Public Sub ParseSimilarGames_WithInvalidJson_ThrowsParseException() 
        Assert.Throws(Of SimilarGamesParseException)(
            Function() SimilarGamesService.ParseSimilarGames("esto no es json"))
    End Sub

    <Fact>
    Public Sub ParseSimilarGames_WithNonArrayJson_ThrowsParseException() 
        Assert.Throws(Of SimilarGamesParseException)(
            Function() SimilarGamesService.ParseSimilarGames("{""id"":1}"))
    End Sub

    <Fact>
    Public Sub ToSimilarityPercentage_ClampsToRange()
        Assert.Equal(50.0, SimilarGamesService.ToSimilarityPercentage(0.5))
        Assert.Equal(100.0, SimilarGamesService.ToSimilarityPercentage(2.0))
        Assert.Equal(0.0, SimilarGamesService.ToSimilarityPercentage(-1.0))
    End Sub
End Class

Public Class StatsServiceTests

    <Fact>
    Public Async Function GetStatsAsync_ReturnsSummaryAndCachesResult() As Task
        StatsService.ClearCache()

        Dim repository = New Mock(Of IStatsRepository)()
        repository.Setup(Function(r) r.GetSummaryAsync()).ReturnsAsync(
            New StatsSummary With {
                .totalGames = 10, .scoredGames = 5, .totalPlatforms = 3, .totalGenres = 4
            })
        repository.Setup(Function(r) r.GetByPlatformAsync(It.IsAny(Of Integer)())).
            ReturnsAsync(New List(Of StatsEntry) From {New StatsEntry With {.label = "PS5", .total = 2}})
        repository.Setup(Function(r) r.GetByGenreAsync(It.IsAny(Of Integer)())).
            ReturnsAsync(New List(Of StatsEntry) From {New StatsEntry With {.label = "RPG", .total = 3}})
        repository.Setup(Function(r) r.GetByDecadeAsync(It.IsAny(Of Integer)())).
            ReturnsAsync(New List(Of StatsEntry) From {New StatsEntry With {.label = "2000", .total = 1}})

        Dim service = New StatsService(repository.Object)

        Dim first = CType(Await service.GetStatsAsync(), Dictionary(Of String, Object))
        Dim second = CType(Await service.GetStatsAsync(), Dictionary(Of String, Object))

        Assert.Same(first, second)
        repository.Verify(Function(r) r.GetSummaryAsync(), Times.Once())
        Assert.Equal(10, first("summary").GetType().GetProperty("totalGames").GetValue(first("summary")))
        Assert.Equal(1, CType(first("platforms"), IEnumerable(Of StatsEntry)).Count())

        StatsService.ClearCache()
        Await service.GetStatsAsync()
        repository.Verify(Function(r) r.GetSummaryAsync(), Times.Exactly(2))
    End Function
End Class

Public Class AuthServiceTests

    Private Const Secret As String = "7456dd0b8487a4477dd5ef9078d8f9c264f82301e034f669bffa31e2b7dfe0d8"

    Private Shared Function CreateConfiguration(Optional configOverrides As Dictionary(Of String, String) = Nothing) As IConfiguration
        Dim values = New Dictionary(Of String, String) From {
            {"JWT_SECRET", Secret},
            {"JWT_EXPIRES_IN", "1h"},
            {"JWT_ISSUER", "rss-game-node"},
            {"JWT_AUDIENCE", "rss-game-node-api"},
            {"AUTH_USERNAME", "admin"},
            {"AUTH_ROLE", "admin"},
            {"AUTH_PASSWORD_HASH", BCrypt.Net.BCrypt.HashPassword("Dev-aekiu4-9mHsG")}
        }

        If configOverrides IsNot Nothing Then
            For Each item In configOverrides
                values(item.Key) = item.Value
            Next
        End If

        Return New ConfigurationBuilder().AddInMemoryCollection(values).Build()
    End Function

    <Fact>
    Public Sub Login_WithValidCredentials_ReturnsUser()
        Dim service = New AuthService(CreateConfiguration())

        Dim user = service.Login("admin", "Dev-aekiu4-9mHsG")

        Assert.NotNull(user)
        Assert.Equal("admin", user.username)
        Assert.Equal("admin", user.role)
    End Sub

    <Fact>
    Public Sub Login_WithWrongPasswordOrUser_ReturnsNothing()
        Dim service = New AuthService(CreateConfiguration())

        Assert.Null(service.Login("admin", "incorrecta"))
        Assert.Null(service.Login("otro", "Dev-aekiu4-9mHsG"))
        Assert.Null(service.Login(Nothing, "Dev-aekiu4-9mHsG"))
    End Sub

    <Fact>
    Public Sub Login_WithoutHashOrWithInvalidHash_ReturnsNothing()
        Dim withoutHash = New AuthService(CreateConfiguration(
            New Dictionary(Of String, String) From {{"AUTH_PASSWORD_HASH", ""}}))
        Dim invalidHash = New AuthService(CreateConfiguration(
            New Dictionary(Of String, String) From {{"AUTH_PASSWORD_HASH", "no-es-bcrypt"}}))

        Assert.Null(withoutHash.Login("admin", "x"))
        Assert.Null(invalidHash.Login("admin", "x"))
    End Sub

    <Fact>
    Public Sub IssueToken_ProducesValidJwtWithSubAndRole()
        Dim config = CreateConfiguration()
        Dim service = New AuthService(config)

        Dim issued = service.IssueToken(New AuthUser With {.username = "admin", .role = "admin"})

        Assert.Equal("Bearer", issued.tokenType)
        Assert.False(String.IsNullOrEmpty(issued.expiresAt))

        Dim parameters = New TokenValidationParameters With {
            .ValidateIssuer = True,
            .ValidIssuer = "rss-game-node",
            .ValidateAudience = True,
            .ValidAudience = "rss-game-node-api",
            .ValidateIssuerSigningKey = True,
            .IssuerSigningKey = New SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            .ValidateLifetime = True,
            .ClockSkew = TimeSpan.Zero,
            .NameClaimType = "sub",
            .RoleClaimType = "role"
        }

        Dim validatedToken As SecurityToken = Nothing
        Dim handler = New JwtSecurityTokenHandler()

        ' Valida firma, issuer, audience y expiración.
        handler.ValidateToken(issued.token, parameters, validatedToken)

        ' ReadJwtToken no mapea los claims: se comprueban los nombres de Node.
        Dim jwt = handler.ReadJwtToken(issued.token)
        Assert.Equal("admin", jwt.Claims.First(Function(claim) claim.Type = "sub").Value)
        Assert.Equal("admin", jwt.Claims.First(Function(claim) claim.Type = "role").Value)
        Assert.True(jwt.ValidTo > DateTime.UtcNow)
    End Sub

    <Fact>
    Public Sub IssueToken_WithoutSecret_Throws()
        Dim config = CreateConfiguration(New Dictionary(Of String, String) From {{"JWT_SECRET", ""}})
        Dim service = New AuthService(config)

        Assert.Throws(Of InvalidOperationException)(
            Function() service.IssueToken(New AuthUser With {.username = "a", .role = "admin"}))
    End Sub
End Class
