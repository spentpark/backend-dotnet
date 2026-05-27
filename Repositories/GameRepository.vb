Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore

Public Class GameRepository
    Implements IGameRepository

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    Public Async Function FindByPlatformPaginatedAsync(platform As String, page As Integer, limit As Integer) As Task(Of PagedResult(Of GameSummary)) Implements IGameRepository.FindByPlatformPaginatedAsync
        Dim offset = (page - 1) * limit
        Dim query = _context.games.Where(Function(g) g.Platform = platform)
        
        Dim total = Await query.CountAsync()
        Dim rows = Await query _
            .OrderBy(Function(g) g.Title) _
            .Skip(offset) _
            .Take(limit) _
            .Select(Function(g) New GameSummary With {
                .id = g.Id,
                .title = g.Title,
                .platform = g.Platform
            }) _
            .ToListAsync()

        Return New PagedResult(Of GameSummary) With {
            .page = page,
            .limit = limit,
            .total = total,
            .data = rows
        }
    End Function

    Public Async Function FindByTitlePaginatedAsync(title As String, page As Integer, limit As Integer) As Task(Of PagedResult(Of GameSummary)) Implements IGameRepository.FindByTitlePaginatedAsync
        Dim offset = (page - 1) * limit
        Dim query = _context.games.AsQueryable()

        If Not String.IsNullOrWhiteSpace(title) Then
            query = query.Where(Function(g) g.Title.Contains(title))
        End If

        Dim total = Await query.CountAsync()
        Dim rows = Await query _
            .OrderBy(Function(g) g.Title) _
            .Skip(offset) _
            .Take(limit) _
            .Select(Function(g) New GameSummary With {
                .id = g.Id,
                .title = g.Title,
                .platform = g.Platform
            }) _
            .ToListAsync()

        Return New PagedResult(Of GameSummary) With {
            .page = page,
            .limit = limit,
            .total = total,
            .data = rows
        }
    End Function

    Public Async Function GetByIdAsync(id As Integer) As Task(Of Game) Implements IGameRepository.GetByIdAsync
        Return Await _context.games.FindAsync(id)
    End Function
End Class