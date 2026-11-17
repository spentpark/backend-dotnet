Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports Xunit

''' <summary>Pruebas de los repositorios EF Core sobre el proveedor InMemory.</summary>
Public Class GameRepositoryTests

    <Fact>
    Public Async Function FindSummariesPaginatedAsync_ClassicPath_FiltersByPlatform() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.platform = "PS5"}, 1, 10)

            Assert.Equal(2, result.pagination.totalRecords)
            Assert.Equal(2, result.data.Count())
            Assert.All(result.data, Sub(game) Assert.Equal("PS5", game.platform))
        End Using
    End Function

    <Fact>
    Public Async Function FindSummariesPaginatedAsync_WithoutPlatform_ReturnsEmptyPage() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindSummariesPaginatedAsync(New GameFilters(), 1, 10)

            Assert.Equal(0, result.pagination.totalRecords)
            Assert.Empty(result.data)
        End Using
    End Function

    <Fact>
    Public Async Function FindSummariesPaginatedAsync_PaginatesAndOrdersById() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)
            Dim filters = New GameFilters With {.platform = "PS5"}

            Dim secondPage = Await repository.FindSummariesPaginatedAsync(filters, 2, 1)

            Assert.Equal(2, secondPage.pagination.totalRecords)
            Assert.Single(secondPage.data)
            Assert.Equal("Beta", secondPage.data.Single().title)
        End Using
    End Function

    <Fact>
    Public Async Function FindSummariesPaginatedAsync_AppliesGenreFilter() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.genre = "Action"}, 1, 10)

            Assert.Equal(2, result.pagination.totalRecords)
        End Using
    End Function

    <Fact>
    Public Async Function FindSummariesPaginatedAsync_AppliesYearDecadeAndPlayersFilters() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim byYear = Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.year = "1995"}, 1, 10)
            Dim byDecade = Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.decade = "200"}, 1, 10)
            Dim byPlayers = Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.players = "1"}, 1, 10)

            Assert.Equal("Beta", byYear.data.Single().title)
            Assert.Equal(1, byDecade.pagination.totalRecords)
            Assert.Equal(4, byPlayers.pagination.totalRecords)
        End Using
    End Function

    <Fact>
    Public Async Function FindSummariesPaginatedAsync_AppliesSortWhitelist() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Assert.Equal("Beta", (Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.platform = "PS5", .sort = "id_desc"}, 1, 1)).data.Single().title)

            Assert.Equal("Alpha", (Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.sort = "title_asc", .platform = "PS5"}, 1, 1)).data.Single().title)

            Assert.Equal("Beta", (Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.sort = "title_desc", .platform = "PS5"}, 1, 1)).data.Single().title)

            Assert.Equal("Omega", (Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.sort = "year_asc"}, 1, 1)).data.Single().title)

            Assert.Equal("Beta Remaster", (Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.sort = "year_desc"}, 1, 1)).data.Single().title)

            Assert.Equal("Alpha", (Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.sort = "score_desc"}, 1, 1)).data.Single().title)
        End Using
    End Function

    <Fact>
    Public Async Function FindSummariesPaginatedAsync_UnknownSortFallsBackToIdAsc() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindSummariesPaginatedAsync(
                New GameFilters With {.sort = "orden-inventado"}, 1, 1)

            Assert.Equal(1, result.data.Single().id)
        End Using
    End Function

    <Fact>
    Public Async Function FindByTitlePaginatedAsync_ReturnsPrefixMatches() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByTitlePaginatedAsync("Beta", 1, 10)

            Assert.Equal(2, result.pagination.totalRecords)
            Assert.All(result.data, Sub(game) Assert.StartsWith("Beta", game.title))
        End Using
    End Function

    <Fact>
    Public Async Function FindByTitlePaginatedAsync_WhenNothingMatches_ReturnsEmpty() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByTitlePaginatedAsync("NoExiste", 1, 10)

            Assert.Equal(0, result.pagination.totalRecords)
            Assert.Empty(result.data)
        End Using
    End Function

    <Fact>
    Public Async Function FindRandomAsync_WithPlatform_ReturnsGameWithTrailer() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim game = Await repository.FindRandomAsync(New GameFilters With {.platform = "PS5"})

            Assert.NotNull(game)
            Assert.Equal(1, game.Id)
        End Using
    End Function

    <Fact>
    Public Async Function FindRandomAsync_WithGenre_ReturnsAnyMatchingGame() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim game = Await repository.FindRandomAsync(New GameFilters With {.genre = "Action"})

            Assert.NotNull(game)
            Assert.Equal("Action", game.Genre)
        End Using
    End Function

    <Fact>
    Public Async Function FindRandomAsync_WithoutFilters_ReturnsGameWithTrailer() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim game = Await repository.FindRandomAsync(New GameFilters())

            Assert.NotNull(game)
            Assert.False(String.IsNullOrEmpty(game.Youtube_Trailer))
        End Using
    End Function

    <Fact>
    Public Async Function FindRandomAsync_WhenNoTrailerGames_ReturnsNothing() As Task
        Using context = CreateContext()
            context.games.Add(New Game With {.Id = 1, .Title = "Sin trailer", .Platform = "PC"})
            Await context.SaveChangesAsync()
            Dim repository = New GameRepository(context)

            Assert.Null(Await repository.FindRandomAsync(New GameFilters()))
        End Using
    End Function

    <Fact>
    Public Async Function FindRandomAsync_WhenDatabaseIsEmpty_ReturnsNothing() As Task
        Using context = CreateContext()
            Dim repository = New GameRepository(context)

            Assert.Null(Await repository.FindRandomAsync(New GameFilters()))
        End Using
    End Function

    <Fact>
    Public Async Function CreateUpdateDelete_WorkAsExpected() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New GameRepository(context)

            Dim created = Await repository.CreateAsync(
                New Game With {.Title = "Nuevo", .Platform = "PC"})
            Assert.True(created.Id > 0)

            ' Actualización parcial: solo cambia el título y conserva el resto.
            Dim updated = Await repository.UpdateAsync(created.Id,
                New Game With {.Title = "Nuevo actualizado"})
            Assert.True(updated)

            Dim stored = Await repository.GetByIdAsync(created.Id)
            Assert.Equal("Nuevo actualizado", stored.Title)
            Assert.Equal("PC", stored.Platform)

            Assert.False(Await repository.UpdateAsync(9999, New Game With {.Title = "x"}))
            Assert.True(Await repository.DeleteAsync(created.Id))
            Assert.False(Await repository.DeleteAsync(created.Id))
            Assert.Null(Await repository.GetByIdAsync(created.Id))
        End Using
    End Function
End Class

Public Class PlatformRepositoryTests

    <Fact>
    Public Async Function GetAllWithUrlAsync_ReturnsOnlyWithUrl() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New PlatformRepository(context)

            Dim result = (Await repository.GetAllWithUrlAsync()).ToList()

            Assert.Equal(2, result.Count)
            Assert.All(result, Sub(platform) Assert.False(String.IsNullOrEmpty(platform.url)))
        End Using
    End Function

    <Fact>
    Public Async Function GetByIdAsync_ReturnsPlatformOrNothing() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New PlatformRepository(context)

            Assert.Equal("PS5", (Await repository.GetByIdAsync(1)).Description)
            Assert.Null(Await repository.GetByIdAsync(999))
        End Using
    End Function

    <Fact>
    Public Async Function GetTopGamesAsync_ReturnsTopTenSortedByScore() As Task
        Using context = CreateContext()
            context.platform.Add(New Platform With {.Id = 1, .Description = "PS5", .Url = "x"})

            For index = 1 To 12
                context.games.Add(New Game With {
                    .Id = index, .Title = $"Juego {index}", .Platform = "PS5",
                    .Metacritic_Score = (60 + index).ToString()})
            Next

            context.games.Add(New Game With {.Id = 20, .Title = "Sin nota", .Platform = "PS5"})
            context.games.Add(New Game With {.Id = 21, .Title = "Nota rara", .Platform = "PS5", .Metacritic_Score = "abc"})
            Await context.SaveChangesAsync()

            Dim repository = New PlatformRepository(context)
            Dim result = (Await repository.GetTopGamesAsync(1)).ToList()

            Assert.Equal(10, result.Count)
            Assert.Equal("Juego 12", result(0).title)
            Assert.Equal("72", result(0).metacritic_score)
            Assert.Equal("Juego 3", result(9).title)
            ' Una nota no numérica se trata como 0 y queda fuera del top 10.
            Assert.DoesNotContain(result, Function(game) game.title = "Nota rara")
        End Using
    End Function

    <Fact>
    Public Async Function CreateUpdateDelete_WorkAsExpected() As Task
        Using context = CreateContext()
            Dim repository = New PlatformRepository(context)

            Dim created = Await repository.CreateAsync(New Platform With {.Description = "Nueva"})
            Assert.True(created.Id > 0)

            Assert.True(Await repository.UpdateAsync(created.Id, New Platform With {.Url = "/nueva"}))
            Dim stored = Await repository.GetByIdAsync(created.Id)
            Assert.Equal("Nueva", stored.Description)
            Assert.Equal("/nueva", stored.Url)

            Assert.False(Await repository.UpdateAsync(9999, New Platform With {.Description = "x"}))
            Assert.True(Await repository.DeleteAsync(created.Id))
            Assert.False(Await repository.DeleteAsync(created.Id))
        End Using
    End Function
End Class

Public Class ReviewRepositoryTests

    <Fact>
    Public Async Function FindByGameIdPaginatedAsync_FiltersByGameAndPaginates() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New ReviewRepository(context)

            Dim firstPage = Await repository.FindByGameIdPaginatedAsync(1, 1, 1)
            Dim secondPage = Await repository.FindByGameIdPaginatedAsync(1, 2, 1)

            Assert.Equal(2, firstPage.pagination.totalRecords)
            Assert.Equal(2, firstPage.pagination.totalPages)
            Assert.Equal("Press", firstPage.data.Single().Name)
            Assert.Equal("User", secondPage.data.Single().Name)
        End Using
    End Function

    <Fact>
    Public Async Function FindByGameIdPaginatedAsync_WithNoReviews_ReturnsZeroTotal() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New ReviewRepository(context)

            Dim result = Await repository.FindByGameIdPaginatedAsync(777, 1, 10)

            Assert.Equal(0, result.pagination.totalRecords)
            Assert.Empty(result.data)
        End Using
    End Function
End Class

Public Class SimilarGameRepositoryTests

    <Fact>
    Public Async Function FindByGameIdAsync_ReturnsRowOrNothing() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New SimilarGameRepository(context)

            Dim row = Await repository.FindByGameIdAsync(1)

            Assert.NotNull(row)
            Assert.Equal(1, row.IdGame)
            Assert.Contains("Gamma", row.Json)
            Assert.Null(Await repository.FindByGameIdAsync(999))
        End Using
    End Function
End Class

Public Class StatsRepositoryTests

    <Fact>
    Public Async Function GetSummaryAsync_CountsCatalogTotals() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New StatsRepository(context)

            Dim summary = Await repository.GetSummaryAsync()

            Assert.Equal(5, summary.totalGames)
            Assert.Equal(3, summary.scoredGames)
            Assert.Equal(3, summary.totalPlatforms)
            Assert.Equal(2, summary.totalGenres)
        End Using
    End Function

    <Fact>
    Public Async Function GetByPlatformAndGenre_ReturnOrderedDistributions() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New StatsRepository(context)

            Dim platforms = (Await repository.GetByPlatformAsync(20)).ToList()
            Dim genres = (Await repository.GetByGenreAsync(20)).ToList()

            Assert.Equal(3, platforms.Count)
            Assert.Equal("PS5", platforms(0).label)
            Assert.Equal(2, platforms(0).total)
            Assert.Equal("RPG", genres(0).label)
            Assert.Equal(3, genres(0).total)
            Assert.Equal(1, (Await repository.GetByPlatformAsync(1)).Count())
        End Using
    End Function

    <Fact>
    Public Async Function GetByDecadeAsync_IgnoresCorruptDatesAndOrdersDescending() As Task
        Using context = CreateContext()
            SeedCatalog(context)
            Dim repository = New StatsRepository(context)

            Dim decades = (Await repository.GetByDecadeAsync(20)).ToList()

            Assert.Equal(4, decades.Count)
            Assert.Equal("2010", decades(0).label)
            Assert.Equal("2000", decades(1).label)
            Assert.Equal("1990", decades(2).label)
            Assert.Equal("1980", decades(3).label)
        End Using
    End Function

    <Fact>
    Public Async Function Distributions_WhenDatabaseIsEmpty_ReturnEmpty() As Task
        Using context = CreateContext()
            Dim repository = New StatsRepository(context)

            Assert.Empty(Await repository.GetByPlatformAsync(20))
            Assert.Empty(Await repository.GetByGenreAsync(20))
            Assert.Empty(Await repository.GetByDecadeAsync(20))
        End Using
    End Function
End Class
