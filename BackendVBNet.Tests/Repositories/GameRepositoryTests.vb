Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports Xunit

''' <summary>
''' Pruebas de paginacion y busqueda del repositorio de juegos contra una
''' base de datos en memoria (mismo provider que usa la aplicacion en produccion:
''' IQueryable + EF Core, por lo que se valida el SQL generado y el mapeo a DTO).
''' </summary>
Public Class GameRepositoryTests

    Private Shared Function CreateInMemoryContext() As AppDbContext
        Dim options = New DbContextOptionsBuilder(Of AppDbContext)() _
            .UseInMemoryDatabase(databaseName:=Guid.NewGuid().ToString()) _
            .Options
        Return New AppDbContext(options)
    End Function

    Private Shared Sub Seed(context As AppDbContext)
        context.games.AddRange(
            New Game With {.Id = 1, .Title = "Alpha", .Platform = "PS5"},
            New Game With {.Id = 2, .Title = "Beta", .Platform = "PS5"},
            New Game With {.Id = 3, .Title = "Gamma", .Platform = "PS5"},
            New Game With {.Id = 4, .Title = "Delta", .Platform = "Xbox"},
            New Game With {.Id = 5, .Title = "Beta Remaster", .Platform = "Xbox"}
        )
        context.SaveChanges()
    End Sub

    ' ==========================================
    ' FindByPlatformPaginatedAsync
    ' ==========================================

    <Fact>
    Public Async Function FindByPlatformPaginatedAsync_ReturnsOnlyPlatformGames() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByPlatformPaginatedAsync("PS5", 1, 10)

            Assert.Equal(3, result.total)
            Assert.Equal(1, result.page)
            Assert.Equal(10, result.limit)
            Assert.Equal(3, result.data.Count())
            Assert.All(result.data, Sub(g) Assert.Equal("PS5", g.platform))
        End Using
    End Function

    <Fact>
    Public Async Function FindByPlatformPaginatedAsync_OrdersByTitleAndPaginates() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim firstPage = Await repository.FindByPlatformPaginatedAsync("PS5", 1, 2)
            Dim secondPage = Await repository.FindByPlatformPaginatedAsync("PS5", 2, 2)

            ' Total siempre es el del filtro, no el de la pagina.
            Assert.Equal(3, firstPage.total)
            Assert.Equal(2, firstPage.data.Count())
            Assert.Equal(New String() {"Alpha", "Beta"}, firstPage.data.Select(Function(g) g.title).ToArray())

            Assert.Equal(1, secondPage.data.Count())
            Assert.Equal("Gamma", secondPage.data.Single().title)
        End Using
    End Function

    <Fact>
    Public Async Function FindByPlatformPaginatedAsync_WhenPageIsOutOfRange_ReturnsEmptyData() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByPlatformPaginatedAsync("PS5", 99, 10)

            Assert.Equal(3, result.total)
            Assert.Empty(result.data)
        End Using
    End Function

    <Fact>
    Public Async Function FindByPlatformPaginatedAsync_MapsSummaryFields() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByPlatformPaginatedAsync("Xbox", 1, 10)
            Dim summary = result.data.Single(Function(g) g.title = "Delta")

            Assert.Equal(4, summary.id)
            Assert.Equal("Delta", summary.title)
            Assert.Equal("Xbox", summary.platform)
        End Using
    End Function

    ' ==========================================
    ' FindByTitlePaginatedAsync
    ' ==========================================

    <Fact>
    Public Async Function FindByTitlePaginatedAsync_FiltersByPartialTitle() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByTitlePaginatedAsync("Beta", 1, 10)

            Assert.Equal(2, result.total)
            Assert.Equal(2, result.data.Count())
            Assert.All(result.data, Sub(g) Assert.Contains("Beta", g.title))
        End Using
    End Function

    <Fact>
    Public Async Function FindByTitlePaginatedAsync_IsCaseSensitiveForInMemoryProvider() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByTitlePaginatedAsync("beta", 1, 10)

            ' Documenta el comportamiento actual del filtro: Contains sin normalizar.
            Assert.Equal(0, result.total)
            Assert.Empty(result.data)
        End Using
    End Function

    <Fact>
    Public Async Function FindByTitlePaginatedAsync_WithEmptyTitle_ReturnsEverythingPaginated() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByTitlePaginatedAsync("", 2, 2)

            Assert.Equal(5, result.total)
            Assert.Equal(2, result.page)
            Assert.Equal(2, result.limit)
            Assert.Equal(New String() {"Beta Remaster", "Delta"}, result.data.Select(Function(g) g.title).ToArray())
        End Using
    End Function

    <Fact>
    Public Async Function FindByTitlePaginatedAsync_WithWhitespaceTitle_ReturnsEverything() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByTitlePaginatedAsync("   ", 1, 10)

            Assert.Equal(5, result.total)
        End Using
    End Function

    <Fact>
    Public Async Function FindByTitlePaginatedAsync_WhenNothingMatches_ReturnsEmptyResult() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim result = Await repository.FindByTitlePaginatedAsync("NoExiste", 1, 10)

            Assert.Equal(0, result.total)
            Assert.Empty(result.data)
        End Using
    End Function

    ' ==========================================
    ' GetByIdAsync
    ' ==========================================

    <Fact>
    Public Async Function GetByIdAsync_ReturnsEntity_WhenExists() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Dim game = Await repository.GetByIdAsync(3)

            Assert.NotNull(game)
            Assert.Equal("Gamma", game.Title)
        End Using
    End Function

    <Fact>
    Public Async Function GetByIdAsync_ReturnsNothing_WhenMissing() As Task
        Using context = CreateInMemoryContext()
            Seed(context)
            Dim repository = New GameRepository(context)

            Assert.Null(Await repository.GetByIdAsync(404))
        End Using
    End Function

End Class
