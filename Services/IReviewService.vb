Imports System.Threading.Tasks

''' <summary>Reglas de negocio de reseñas.</summary>
Public Interface IReviewService
    Function GetReviewsAsync(gameId As Integer, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of Review))
End Interface
