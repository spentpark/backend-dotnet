Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore

Public Class ReviewRepository
    Implements IReviewRepository

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    Public Async Function GetByGameIdAsync(gameId As Integer) As Task(Of IEnumerable(Of ReviewSummary)) Implements IReviewRepository.GetByGameIdAsync
        Return Await _context.review _
            .Where(Function(r) r.GameId = gameId) _
            .Select(Function(r) New ReviewSummary With {
                .id = r.Id,
                .gameId = r.GameId,
                .author = r.Author,
                .score = r.Score,
                .comment = r.Comment,
                .createdAt = r.CreatedAt
            }) _
            .ToListAsync()
    End Function
End Class