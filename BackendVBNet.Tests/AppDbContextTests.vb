Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports Xunit

''' <summary>Pruebas del DbContext (DbSets y operaciones básicas con InMemory).</summary>
Public Class AppDbContextTests

    <Fact>
    Public Sub Constructor_CreatesContextAndDbSets()
        Using context = CreateContext()
            Assert.NotNull(context)
            Assert.NotNull(context.games)
            Assert.NotNull(context.platform)
            Assert.NotNull(context.review)
            Assert.NotNull(context.similar_game)
        End Using
    End Sub

    <Fact>
    Public Async Function DbSets_CanAddAndRetrieveEntities() As Task
        Using context = CreateContext()
            Dim game = New Game With {.Id = 1, .Title = "Elden Ring"}
            Dim platform = New Platform With {.Id = 1, .Description = "PS5", .Url = "https://ps5"}
            Dim review = New Review With {
                .Id = 1, .IdGame = 1, .Name = "Press", .Score = "90", .ReviewDate = "2024-01-01"
            }
            Dim similar = New SimilarGame With {.IdGame = 1, .Json = "[]"}

            context.games.Add(game)
            context.platform.Add(platform)
            context.review.Add(review)
            context.similar_game.Add(similar)
            Await context.SaveChangesAsync()

            Assert.Equal("Elden Ring", (Await context.games.FindAsync(1)).Title)
            Assert.Equal("PS5", (Await context.platform.FindAsync(1)).Description)
            Assert.Equal("Press", (Await context.review.FindAsync(1)).Name)
            Assert.Equal("[]", (Await context.similar_game.FindAsync(1)).Json)
        End Using
    End Function

    <Fact>
    Public Async Function DbSets_CanDeleteEntity() As Task
        Using context = CreateContext()
            Dim game = New Game With {.Id = 1, .Title = "Elden Ring"}
            context.games.Add(game)
            Await context.SaveChangesAsync()

            context.games.Remove(game)
            Await context.SaveChangesAsync()

            Assert.Null(Await context.games.FindAsync(1))
        End Using
    End Function

    <Fact>
    Public Async Function Contexts_AreIsolated() As Task
        Using first = CreateContext()
            Using second = CreateContext()
                first.games.Add(New Game With {.Id = 1, .Title = "Uno"})
                Await first.SaveChangesAsync()

                Assert.Equal(0, Await second.games.CountAsync())
            End Using
        End Using
    End Function
End Class
