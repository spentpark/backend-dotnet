Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore

''' <summary>Consultas EF Core sobre la tabla <c>platform</c>.</summary>
Public Class PlatformRepository
    Implements IPlatformRepository

    Private Const TopGamesLimit As Integer = 10

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    Public Async Function GetAllWithUrlAsync() As Task(Of IEnumerable(Of PlatformSummary)) Implements IPlatformRepository.GetAllWithUrlAsync
        Return Await _context.platform.
            Where(Function(p) Not String.IsNullOrEmpty(p.Url)).
            OrderBy(Function(p) p.Id).
            Select(Function(p) New PlatformSummary With {
                .id = p.Id,
                .description = p.Description,
                .url = p.Url
            }).
            ToListAsync()
    End Function

    Public Async Function GetByIdAsync(id As Integer) As Task(Of Platform) Implements IPlatformRepository.GetByIdAsync
        Return Await _context.platform.FindAsync(id)
    End Function

    Public Async Function GetTopGamesAsync(id As Integer) As Task(Of IEnumerable(Of TopGame)) Implements IPlatformRepository.GetTopGamesAsync
        ' Se materializa el subconjunto de la plataforma con nota y se ordena en
        ' memoria: CAST(metacritic_score AS UNSIGNED) no es portable entre
        ' proveedores y el número de filas por plataforma es pequeño.
        Dim rows = Await (
            From g In _context.games
            Join p In _context.platform On g.Platform Equals p.Description
            Where p.Id = id AndAlso g.Metacritic_Score IsNot Nothing AndAlso g.Metacritic_Score <> ""
            Select New TopGame With {
                .id = g.Id,
                .title = g.Title,
                .Platform = g.Platform,
                .genre = g.Genre,
                .releasedate = g.ReleaseDate,
                .image_Original = g.Image_Original,
                .metacritic_score = g.Metacritic_Score
            }).ToListAsync()

        Return rows.
            OrderByDescending(Function(game) ParseScore(game.metacritic_score)).
            ThenBy(Function(game) game.id).
            Take(TopGamesLimit).
            ToList()
    End Function

    Public Async Function CreateAsync(platform As Platform) As Task(Of Platform) Implements IPlatformRepository.CreateAsync
        _context.platform.Add(platform)
        Await _context.SaveChangesAsync()
        Return platform
    End Function

    Public Async Function UpdateAsync(id As Integer, platform As Platform) As Task(Of Boolean) Implements IPlatformRepository.UpdateAsync
        Dim existing = Await _context.platform.FindAsync(id)

        If existing Is Nothing Then Return False

        ' Actualización parcial como `UPDATE ... SET ?` de Node: los campos
        ' ausentes en el body llegan como Nothing y no se sobrescriben.
        If platform.Description IsNot Nothing Then existing.Description = platform.Description
        If platform.Url IsNot Nothing Then existing.Url = platform.Url
        Await _context.SaveChangesAsync()
        Return True
    End Function

    Public Async Function DeleteAsync(id As Integer) As Task(Of Boolean) Implements IPlatformRepository.DeleteAsync
        Dim existing = Await _context.platform.FindAsync(id)

        If existing Is Nothing Then Return False

        _context.platform.Remove(existing)
        Await _context.SaveChangesAsync()
        Return True
    End Function

    Private Shared Function ParseScore(value As String) As Integer
        Dim parsed As Integer

        Return If(Integer.TryParse(value, parsed), parsed, 0)
    End Function
End Class
