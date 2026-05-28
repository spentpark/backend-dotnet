Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports Xunit

Public Class PlatformRepositoryTests

    Private Function CreateInMemoryContext() As AppDbContext
        Dim options = New DbContextOptionsBuilder(Of AppDbContext)() _
            .UseInMemoryDatabase(databaseName:=Guid.NewGuid().ToString()) _
            .Options
        Return New AppDbContext(options)
    End Function

    <Fact>
    Public Async Function GetAllWithUrlAsync_ReturnsOnlyPlatformsWithUrl() As Task
        ' Arrange
        Using context = CreateInMemoryContext()
            context.platform.AddRange(
                New Platform With {.Id = 1, .Description = "PlayStation 5", .Url = "https://ps5.com"},
                New Platform With {.Id = 2, .Description = "Xbox Series X", .Url = "https://xbox.com"},
                New Platform With {.Id = 3, .Description = "Sin URL", .Url = Nothing},
                New Platform With {.Id = 4, .Description = "Vacia", .Url = ""}
            )
            Await context.SaveChangesAsync()

            Dim repository = New PlatformRepository(context)

            ' Act
            Dim result = Await repository.GetAllWithUrlAsync()
            Dim list = result.ToList()

            ' Assert
            Assert.Equal(2, list.Count)
            Assert.All(list, Function(p) Assert.False(String.IsNullOrEmpty(p.url)))
        End Using
    End Function

    <Fact>
    Public Async Function GetAllWithUrlAsync_ReturnsCorrectFields() As Task
        ' Arrange
        Using context = CreateInMemoryContext()
            context.platform.Add(
                New Platform With {.Id = 1, .Description = "PlayStation 5", .Url = "https://ps5.com"}
            )
            Await context.SaveChangesAsync()

            Dim repository = New PlatformRepository(context)

            ' Act
            Dim result = Await repository.GetAllWithUrlAsync()
            Dim platform = result.First()

            ' Assert
            Assert.Equal(1, platform.id)
            Assert.Equal("PlayStation 5", platform.description)
            Assert.Equal("https://ps5.com", platform.url)
        End Using
    End Function

    <Fact>
    Public Async Function GetAllWithUrlAsync_WhenNoPlatformsWithUrl_ReturnsEmptyList() As Task
        ' Arrange
        Using context = CreateInMemoryContext()
            context.platform.AddRange(
                New Platform With {.Id = 1, .Description = "Sin URL", .Url = Nothing},
                New Platform With {.Id = 2, .Description = "Vacia", .Url = ""}
            )
            Await context.SaveChangesAsync()

            Dim repository = New PlatformRepository(context)

            ' Act
            Dim result = Await repository.GetAllWithUrlAsync()

            ' Assert
            Assert.Empty(result)
        End Using
    End Function

    <Fact>
    Public Async Function GetAllWithUrlAsync_WhenDatabaseEmpty_ReturnsEmptyList() As Task
        ' Arrange
        Using context = CreateInMemoryContext()
            Dim repository = New PlatformRepository(context)

            ' Act
            Dim result = Await repository.GetAllWithUrlAsync()

            ' Assert
            Assert.Empty(result)
        End Using
    End Function

End Class