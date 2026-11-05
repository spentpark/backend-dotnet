Imports System.Threading.Tasks

''' <summary>Consultas EF Core sobre la tabla <c>similar_game</c>.</summary>
Public Class SimilarGameRepository
    Implements ISimilarGameRepository

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    Public Async Function FindByGameIdAsync(idGame As Integer) As Task(Of SimilarGame) Implements ISimilarGameRepository.FindByGameIdAsync
        Return Await _context.similar_game.FindAsync(idGame)
    End Function
End Class
