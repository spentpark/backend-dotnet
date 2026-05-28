Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Moq
Imports Xunit

Public Class ReviewServiceTests

    Private ReadOnly _repositoryMock As Mock(Of IReviewRepository)
    Private ReadOnly _reviewService As ReviewService

    Public Sub New()
        ' Inicializamos el mock del repositorio de reviews y el servicio
        _repositoryMock = New Mock(Of IReviewRepository)()
        _reviewService = New ReviewService(_repositoryMock.Object)
    End Sub

    ''' <summary>
    ''' Test para GetReviewsByGameIdAsync: Asegura que el servicio consulte las reviews usando el ID correcto
    ''' </summary>
    <Fact>
    Public Async Function GetReviewsByGameIdAsync_ReturnsReviewsFromRepository() As Task
        ' Arrange (Preparar)
        Dim targetGameId As Integer = 256
        Dim mockData As New List(Of ReviewSummary) From {
            New ReviewSummary() With { .Id = 10, .GameId = targetGameId, .Comment = "Excelente emulación." },
            New ReviewSummary() With { .Id = 11, .GameId = targetGameId, .Comment = "Un clásico de peleas." }
        }

        ' Configuramos el repositorio simulado para el ID específico
        _repositoryMock.
            Setup(Function(r) r.GetByGameIdAsync(targetGameId)).
            ReturnsAsync(mockData)

        ' Act (Ejecutar)
        Dim result = Await _reviewService.GetReviewsByGameIdAsync(targetGameId)

        ' Assert (Verificar)
        Assert.NotNull(result)
        Assert.Same(mockData, result) ' Verifica que la lista no sufrió alteraciones
        _repositoryMock.Verify(Function(r) r.GetByGameIdAsync(targetGameId), Times.Once()) ' Comprueba la llamada única al repositorio
    End Function

End Class