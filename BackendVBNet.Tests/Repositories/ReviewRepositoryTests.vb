Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports Xunit

Public Class ReviewRepositoryTests

    Private Function CreateInMemoryContext() As AppDbContext
        Dim options = New DbContextOptionsBuilder(Of AppDbContext)() _
            .UseInMemoryDatabase(databaseName:=Guid.NewGuid().ToString()) _
            .Options
        Return New AppDbContext(options)
    End Function

    <Fact>
    Public Async Function GetByGameIdAsync_ReturnsOnlyReviewsForGivenGame() As Task
        Using context = CreateInMemoryContext()
            context.review.AddRange(
                New Review With {.Id = 1, .GameId = 10, .Author = "Player1", .Score = "9", .Comment = "Excelente", .CreatedAt = DateTime.Now},
                New Review With {.Id = 2, .GameId = 10, .Author = "Player2", .Score = "8", .Comment = "Muy bueno", .CreatedAt = DateTime.Now},
                New Review With {.Id = 3, .GameId = 99, .Author = "Otro",    .Score = "5", .Comment = "Regular",   .CreatedAt = DateTime.Now}
            )
            Await context.SaveChangesAsync()

            Dim repository = New ReviewRepository(context)
            Dim result = Await repository.GetByGameIdAsync(10)
            Dim list = result.ToList()

            Assert.Equal(2, list.Count)
            For Each r In list
                Assert.Equal(10, r.gameId)
            Next
        End Using
    End Function

    <Fact>
    Public Async Function GetByGameIdAsync_ReturnsCorrectMappedFields() As Task
        Using context = CreateInMemoryContext()
            Dim createdAt = New DateTime(2024, 1, 15)
            context.review.Add(
                New Review With {
                    .Id = 1,
                    .GameId = 10,
                    .Author = "Player1",
                    .Score = "9",
                    .Comment = "Excelente juego",
                    .CreatedAt = createdAt
                }
            )
            Await context.SaveChangesAsync()

            Dim repository = New ReviewRepository(context)
            Dim result = Await repository.GetByGameIdAsync(10)
            Dim review = result.First()

            ' Cubre el .Select con todos los campos mapeados (lineas 18-24)
            Assert.Equal(1, review.id)
            Assert.Equal(10, review.gameId)
            Assert.Equal("Player1", review.author)
            Assert.Equal("9", review.score)
            Assert.Equal("Excelente juego", review.comment)
            Assert.Equal(createdAt, review.createdAt)
        End Using
    End Function

    <Fact>
    Public Async Function GetByGameIdAsync_WhenNoReviewsForGame_ReturnsEmptyList() As Task
        Using context = CreateInMemoryContext()
            context.review.Add(
                New Review With {
                    .Id = 1,
                    .GameId = 99,
                    .Author = "Player1",
                    .Score = "9",
                    .Comment = "Otro juego",
                    .CreatedAt = DateTime.Now
                }
            )
            Await context.SaveChangesAsync()

            Dim repository = New ReviewRepository(context)
            Dim result = Await repository.GetByGameIdAsync(10)

            Assert.Empty(result)
        End Using
    End Function

    <Fact>
    Public Async Function GetByGameIdAsync_WhenDatabaseEmpty_ReturnsEmptyList() As Task
        Using context = CreateInMemoryContext()
            Dim repository = New ReviewRepository(context)
            Dim result = Await repository.GetByGameIdAsync(10)

            Assert.Empty(result)
        End Using
    End Function

    <Fact>
    Public Async Function GetByGameIdAsync_WhenMultipleGames_ReturnsOnlyRequestedGame() As Task
        Using context = CreateInMemoryContext()
            context.review.AddRange(
                New Review With {.Id = 1, .GameId = 1, .Author = "A", .Score = "10", .Comment = "Juego 1",             .CreatedAt = DateTime.Now},
                New Review With {.Id = 2, .GameId = 2, .Author = "B", .Score = "8",  .Comment = "Juego 2",             .CreatedAt = DateTime.Now},
                New Review With {.Id = 3, .GameId = 3, .Author = "C", .Score = "6",  .Comment = "Juego 3",             .CreatedAt = DateTime.Now},
                New Review With {.Id = 4, .GameId = 2, .Author = "D", .Score = "7",  .Comment = "Juego 2 otra review", .CreatedAt = DateTime.Now}
            )
            Await context.SaveChangesAsync()

            Dim repository = New ReviewRepository(context)
            Dim result = Await repository.GetByGameIdAsync(2)
            Dim list = result.ToList()

            Assert.Equal(2, list.Count)
            For Each r In list
                Assert.Equal(2, r.gameId)
            Next
        End Using
    End Function

End Class