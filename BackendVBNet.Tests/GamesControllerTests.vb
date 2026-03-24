Imports Xunit
Imports Microsoft.AspNetCore.Mvc
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports BackendVBNet.Controllers
Imports BackendVBNet.Data
Imports BackendVBNet.Models
Imports Microsoft.EntityFrameworkCore

Public Class TestAppDbContext
    Inherits AppDbContext

    Public Sub New(dbName As String)
        MyBase.New(New DbContextOptionsBuilder(Of AppDbContext)().UseInMemoryDatabase(dbName).Options)
    End Sub

    Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
        ' Do not configure, use in-memory
    End Sub
End Class

Public Class GamesControllerTests

    Private Function CreateTestContext() As AppDbContext
        Dim context = New TestAppDbContext(Guid.NewGuid().ToString())
        context.games.AddRange(New List(Of Game) From {
            New Game With {.Id = 1, .Title = "Game 1", .Platform = "PS5"},
            New Game With {.Id = 2, .Title = "Game 2", .Platform = "PS5"},
            New Game With {.Id = 3, .Title = "Other Title", .Platform = "Xbox"}
        })
        context.SaveChanges()
        Return context
    End Function

    <Fact>
    Public Async Function GetById_ReturnsOk_WhenGameExists() As Task
        ' Arrange
        Using context = CreateTestContext()
            Dim controller = New GamesController(context)

            ' Act
            Dim result = Await controller.GetById(1)

            ' Assert
            Dim okResult = Assert.IsType(Of OkObjectResult)(result)
            Dim game = Assert.IsType(Of Game)(okResult.Value)
            Assert.Equal(1, game.Id)
        End Using
    End Function

    <Fact>
    Public Async Function GetById_ReturnsNotFound_WhenGameDoesNotExist() As Task
        ' Arrange
        Using context = CreateTestContext()
            Dim controller = New GamesController(context)

            ' Act
            Dim result = Await controller.GetById(999)

            ' Assert
            Assert.IsType(Of NotFoundResult)(result)
        End Using
    End Function

    <Fact>
    Public Async Function FindByPlatformPaginated_ReturnsOk_WithData() As Task
        ' Arrange
        Using context = CreateTestContext()
            Dim controller = New GamesController(context)

            ' Act
            Dim result = Await controller.FindByPlatformPaginated("PS5", 1, 10)

            ' Assert
            Dim okResult = Assert.IsType(Of OkObjectResult)(result)
            Dim response = okResult.Value
            Assert.Equal(1, response.page)
            Assert.Equal(10, response.limit)
            Assert.Equal(2, response.total)
            Assert.Equal(2, response.data.Count)
        End Using
    End Function

    <Fact>
    Public Async Function FindByTitlePaginated_ReturnsOk_WithFilteredData() As Task
        ' Arrange
        Using context = CreateTestContext()
            Dim controller = New GamesController(context)

            ' Act
            Dim result = Await controller.FindByTitlePaginated("Game", 1, 10)

            ' Assert
            Dim okResult = Assert.IsType(Of OkObjectResult)(result)
            Dim response = okResult.Value
            Assert.Equal(1, response.page)
            Assert.Equal(10, response.limit)
            Assert.Equal(2, response.total) ' Game 1 and Game 2
            Assert.Equal(2, response.data.Count)
        End Using
    End Function

    <Fact>
    Public Async Function FindByTitlePaginated_ReturnsOk_WithNoFilter() As Task
        ' Arrange
        Using context = CreateTestContext()
            Dim controller = New GamesController(context)

            ' Act
            Dim result = Await controller.FindByTitlePaginated("", 1, 10)

            ' Assert
            Dim okResult = Assert.IsType(Of OkObjectResult)(result)
            Dim response = okResult.Value
            Assert.Equal(1, response.page)
            Assert.Equal(10, response.limit)
            Assert.Equal(3, response.total) ' All games
            Assert.Equal(3, response.data.Count)
        End Using
    End Function

End Class