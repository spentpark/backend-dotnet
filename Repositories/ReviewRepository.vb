Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore

''' <summary>Consultas EF Core sobre la tabla <c>review</c> (esquema de producción).</summary>
Public Class ReviewRepository
    Implements IReviewRepository

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    Public Async Function FindByGameIdPaginatedAsync(gameId As Integer, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of Review)) Implements IReviewRepository.FindByGameIdPaginatedAsync
        Dim query = _context.review.Where(Function(r) r.IdGame = gameId)

        Dim total = Await query.CountAsync()
        Dim rows = Await query.
            OrderBy(Function(r) r.Id).
            Skip((page - 1) * limit).
            Take(limit).
            ToListAsync()

        Return PaginatedResult(Of Review).Create(rows, total, page, limit)
    End Function
End Class
