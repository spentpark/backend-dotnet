Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Mvc
Imports Moq
Imports Xunit
Imports BackendVBNet

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
        
        ' Ajustado perfectamente a la estructura real de tu ReviewSummary
        Dim mockReviews As New List(Of ReviewSummary) From {
            New ReviewSummary() With { 
                .id = 1, 
                .gameId = targetGameId, 
                .author = "Player1", 
                .score = 5, 
                .comment = "Juegazo, una obra maestra de la época." 
            },
            New ReviewSummary() With { 
                .id = 2, 
                .gameId = targetGameId, 
                .author = "RetroGamer", 
                .score = 4, 
                .comment = "Muy buena jugabilidad, pero la música se vuelve repetitiva." 
            }
        }

        ' Configuramos el Mock para que devuelva la lista fuertemente tipada al pasarle el ID del juego
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