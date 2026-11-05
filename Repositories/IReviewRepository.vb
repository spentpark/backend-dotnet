Imports System.Threading.Tasks

''' <summary>Acceso a datos de reseñas (EF Core).</summary>
Public Interface IReviewRepository
    Function FindByGameIdPaginatedAsync(gameId As Integer, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of Review))
End Interface
