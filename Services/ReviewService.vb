Imports System.Threading.Tasks

''' <summary>Servicio de reseñas: normaliza la paginación (por defecto 10).</summary>
Public Class ReviewService
    Implements IReviewService

    Public Const DefaultLimit As Integer = 10

    Private ReadOnly _repository As IReviewRepository

    Public Sub New(repository As IReviewRepository)
        _repository = repository
    End Sub

    Public Async Function GetReviewsAsync(gameId As Integer, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of Review)) Implements IReviewService.GetReviewsAsync
        Dim safePage = If(page <= 0, 1, page)
        Dim safeLimit = If(limit <= 0, DefaultLimit, limit)

        Return Await _repository.FindByGameIdPaginatedAsync(gameId, safePage, safeLimit)
    End Function
End Class
