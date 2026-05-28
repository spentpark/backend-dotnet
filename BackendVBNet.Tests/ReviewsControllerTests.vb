Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Mvc
Imports Moq
Imports Xunit

Public Class ReviewsControllerTests

    Private ReadOnly _reviewServiceMock As Mock(Of IReviewService)
    Private ReadOnly _controller As ReviewsController

    Public Sub New()
        ' Inicializamos el mock del servicio de reviews y el controlador
        _reviewServiceMock = New Mock(Of IReviewService)()
        _controller = New ReviewsController(_reviewServiceMock.Object)
    End Sub

    ''' <summary>
    ''' Test para GetReviews: Debe retornar OkObjectResult con la lista de reviews asociadas al juego
    ''' </summary>
    <Fact>
    Public Async Function GetReviews_ReturnsOkWithListOfReviews() As Task
        ' Arrange (Preparar datos de prueba)
        Dim targetGameId As Integer = 120
        
        ' Simulamos un par de reviews para el juego seleccionado
        Dim mockReviews As New List(Of Object) From {
            New With { .Id = 1, .GameId = targetGameId, .User = "Player1", .Rating = 5, .Comment = "Juegazo, una obra maestra de la época." },
            New With { .Id = 2, .GameId = targetGameId, .User = "RetroGamer", .Rating = 4, .Comment = "Muy buena jugabilidad, pero la música se vuelve repetitiva." }
        }

        ' Configuramos el Mock para que devuelva la lista al pasarle el ID del juego
        _reviewServiceMock.
            Setup(Function(s) s.GetReviewsByGameIdAsync(targetGameId)).
            ReturnsAsync(mockReviews)

        ' Act (Ejecutar el método del controlador)
        Dim actionResult = Await _controller.GetReviews(targetGameId)

        ' Assert (Verificaciones básicas de código de estado y contenido)
        Dim okResult = Assert.IsType(Of OkObjectResult)(actionResult)
        Assert.Equal(200, okResult.StatusCode)
        Assert.Equal(mockReviews, okResult.Value)
    End Function

End Class