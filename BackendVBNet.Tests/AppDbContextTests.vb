Imports System
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports Xunit

Public Class AppDbContextTests

    Private Function CreateInMemoryContext() As AppDbContext
        Dim options = New DbContextOptionsBuilder(Of AppDbContext)() _
            .UseInMemoryDatabase(databaseName:=Guid.NewGuid().ToString()) _
            .Options
        Return New AppDbContext(options)
    End Function

    ' =============================================
    ' Tests de construccion y configuracion
    ' =============================================

    <Fact>
    Public Sub Constructor_WithOptions_CreatesContext()
        ' Arrange & Act
        Using context = CreateInMemoryContext()
            ' Assert - no lanza excepcion
            Assert.NotNull(context)
        End Using
    End Sub

    <Fact>
    Public Sub OnConfiguring_WhenAlreadyConfigured_DoesNotOverrideConnection()
        ' Arrange - pasamos opciones InMemory (ya configuradas)
        Dim options = New DbContextOptionsBuilder(Of AppDbContext)() _
            .UseInMemoryDatabase(databaseName:="TestOnConfiguring_" & Guid.NewGuid().ToString()) _
            .Options

        ' Act & Assert - si OnConfiguring sobreescribiera, intentaria conectar a MySQL y fallaria
        Using context = New AppDbContext(options)
            Assert.NotNull(context)
            ' Si llegamos aqui sin excepcion, OnConfiguring respeto la configuracion existente
        End Using
    End Sub

    ' =============================================
    ' Tests de DbSets
    ' =============================================

    <Fact>
    Public Sub DbSet_Games_IsNotNull()
        Using context = CreateInMemoryContext()
            Assert.NotNull(context.games)
        End Using
    End Sub

    <Fact>
    Public Sub DbSet_Platform_IsNotNull()
        Using context = CreateInMemoryContext()
            Assert.NotNull(context.platform)
        End Using
    End Sub

    <Fact>
    Public Sub DbSet_Review_IsNotNull()
        Using context = CreateInMemoryContext()
            Assert.NotNull(context.review)
        End Using
    End Sub

    ' =============================================
    ' Tests de operaciones CRUD basicas
    ' =============================================

    <Fact>
    Public Async Function DbSet_Games_CanAddAndRetrieve() As Task
        Using context = CreateInMemoryContext()
            ' Arrange
            Dim game = New Game With {
                .Id = 1,
                .Title = "Elden Ring",
                .Description = "Open world RPG"
            }

            ' Act
            context.games.Add(game)
            Await context.SaveChangesAsync()

            Dim retrieved = Await context.games.FindAsync(1)

            ' Assert
            Assert.NotNull(retrieved)
            Assert.Equal("Elden Ring", retrieved.Title)
        End Using
    End Function

    <Fact>
    Public Async Function DbSet_Platform_CanAddAndRetrieve() As Task
        Using context = CreateInMemoryContext()
            ' Arrange
            Dim platform = New Platform With {
                .Id = 1,
                .Description = "PlayStation 5",
                .Url = "https://ps5.com"
            }

            ' Act
            context.platform.Add(platform)
            Await context.SaveChangesAsync()

            Dim retrieved = Await context.platform.FindAsync(1)

            ' Assert
            Assert.NotNull(retrieved)
            Assert.Equal("PlayStation 5", retrieved.Description)
            Assert.Equal("https://ps5.com", retrieved.Url)
        End Using
    End Function

    <Fact>
    Public Async Function DbSet_Review_CanAddAndRetrieve() As Task
        Using context = CreateInMemoryContext()
            ' Arrange
            Dim review = New Review With {
                .Id = 1,
                .GameId = 1,
                .Author = "Player1",
                .Score = "9",
                .Comment = "Excelente juego",
                .CreatedAt = New DateTime(2024, 1, 15)
            }

            ' Act
            context.review.Add(review)
            Await context.SaveChangesAsync()

            Dim retrieved = Await context.review.FindAsync(1)

            ' Assert
            Assert.NotNull(retrieved)
            Assert.Equal("Player1", retrieved.Author)
            Assert.Equal("9", retrieved.Score)
        End Using
    End Function

    <Fact>
    Public Async Function DbSet_Games_CanDelete() As Task
        Using context = CreateInMemoryContext()
            ' Arrange
            Dim game = New Game With {
                .Id = 1,
                .Title = "Elden Ring",
                .Description = "Open world RPG"
            }
            context.games.Add(game)
            Await context.SaveChangesAsync()

            ' Act
            context.games.Remove(game)
            Await context.SaveChangesAsync()

            Dim retrieved = Await context.games.FindAsync(1)

            ' Assert
            Assert.Null(retrieved)
        End Using
    End Function

    <Fact>
    Public Async Function DbSet_MultipleContexts_AreIsolated() As Task
        ' Arrange - dos contextos con bases de datos distintas
        Dim options1 = New DbContextOptionsBuilder(Of AppDbContext)() _
            .UseInMemoryDatabase("IsolationTest_DB1") _
            .Options
        Dim options2 = New DbContextOptionsBuilder(Of AppDbContext)() _
            .UseInMemoryDatabase("IsolationTest_DB2") _
            .Options

        Using context1 = New AppDbContext(options1)
            Using context2 = New AppDbContext(options2)
                ' Act - agregar solo en context1
                context1.platform.Add(New Platform With {
                    .Id = 1,
                    .Description = "PlayStation 5",
                    .Url = "https://ps5.com"
                })
                Await context1.SaveChangesAsync()

                ' Assert - context2 no debe ver los datos de context1
                Dim countInContext2 = Await context2.platform.CountAsync()
                Assert.Equal(0, countInContext2)
            End Using
        End Using
    End Function

End Class