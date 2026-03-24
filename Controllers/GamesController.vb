Imports Microsoft.AspNetCore.Mvc
Imports Microsoft.EntityFrameworkCore

<Route("api/games")>
<ApiController>
Public Class GamesController
    Inherits ControllerBase

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    <HttpGet("platform")>
    Public Async Function FindByPlatformPaginated(
        <FromQuery> platform As String,
        <FromQuery> page As Integer,
        <FromQuery> limit As Integer
    ) As Task(Of IActionResult)

        If page <= 0 Then page = 1
        If limit <= 0 Then limit = 20

        Dim offset = (page - 1) * limit

        Dim query = _context.Games.Where(Function(g) g.Platform = platform)

        ' total de registros
        Dim total = Await query.CountAsync()

        ' registros paginados (solo columnas necesarias)
        Dim rows = Await query _
            .OrderBy(Function(g) g.Title) _
            .Skip(offset) _
            .Take(limit) _
            .Select(Function(g) New With {
                .id = g.Id,
                .title = g.Title,
                .platform = g.Platform
            }) _
            .ToListAsync()

        Return Ok(New With {
            .page = page,
            .limit = limit,
            .total = total,
            .data = rows
        })

    End Function

    <HttpGet("search")>
    Public Async Function FindByTitlePaginated(
        <FromQuery> title As String,
        <FromQuery> page As Integer,
        <FromQuery> limit As Integer
    ) As Task(Of IActionResult)

        If page <= 0 Then page = 1
        If limit <= 0 Then limit = 20

        Dim offset = (page - 1) * limit

        Dim query = _context.Games.AsQueryable()

        If Not String.IsNullOrWhiteSpace(title) Then
            query = query.Where(Function(g) g.Title.Contains(title))
        End If

        Dim total = Await query.CountAsync()

        Dim rows = Await query _
            .OrderBy(Function(g) g.Title) _
            .Skip(offset) _
            .Take(limit) _
            .Select(Function(g) New With {
                .id = g.Id,
                .title = g.Title,
                .platform = g.Platform
            }) _
            .ToListAsync()

        Return Ok(New With {
            .page = page,
            .limit = limit,
            .total = total,
            .data = rows
        })

    End Function

    <HttpGet("{id}")>
    Public Async Function GetById(<FromRoute> id As Integer) As Task(Of IActionResult)

        Dim game = Await _context.Games.FindAsync(id)

        If game Is Nothing Then
            Return NotFound()
        End If

        Return Ok(game)

    End Function

End Class