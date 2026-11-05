Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore

''' <summary>
''' Consultas de agregación de <c>GET /api/stats</c>. Se usan expresiones LINQ
''' portables (sin SQL específico de MySQL) para que también funcionen con el
''' proveedor InMemory de las pruebas.
''' </summary>
Public Class StatsRepository
    Implements IStatsRepository

    Public Const DefaultDistributionLimit As Integer = 20

    ' Prefijos de década válidos (1950–2035); descarta fechas corruptas.
    Private Shared ReadOnly ValidDecadePrefixes As String() = {
        "195", "196", "197", "198", "199", "200", "201", "202", "203"
    }

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    Public Async Function GetSummaryAsync() As Task(Of StatsSummary) Implements IStatsRepository.GetSummaryAsync
        Dim totalGames = Await _context.games.CountAsync()
        Dim scoredGames = Await _context.games.CountAsync(
            Function(g) g.Metacritic_Score IsNot Nothing AndAlso g.Metacritic_Score <> "")
        Dim totalPlatforms = Await _context.games.
            Where(Function(g) g.Platform IsNot Nothing).
            Select(Function(g) g.Platform).
            Distinct().
            CountAsync()
        Dim totalGenres = Await _context.games.
            Where(Function(g) g.Genre IsNot Nothing).
            Select(Function(g) g.Genre).
            Distinct().
            CountAsync()

        Return New StatsSummary With {
            .totalGames = totalGames,
            .scoredGames = scoredGames,
            .totalPlatforms = totalPlatforms,
            .totalGenres = totalGenres
        }
    End Function

    Public Async Function GetByPlatformAsync(limit As Integer) As Task(Of IEnumerable(Of StatsEntry)) Implements IStatsRepository.GetByPlatformAsync
        Dim grouped = Await _context.games.
            Where(Function(g) g.Platform IsNot Nothing AndAlso g.Platform <> "").
            GroupBy(Function(g) g.Platform).
            Select(Function(group) New StatsEntry With {.label = group.Key, .total = group.Count()}).
            ToListAsync()

        Return grouped.
            OrderByDescending(Function(entry) entry.total).
            ThenBy(Function(entry) entry.label).
            Take(limit).
            ToList()
    End Function

    Public Async Function GetByGenreAsync(limit As Integer) As Task(Of IEnumerable(Of StatsEntry)) Implements IStatsRepository.GetByGenreAsync
        Dim grouped = Await _context.games.
            Where(Function(g) g.Genre IsNot Nothing AndAlso g.Genre <> "").
            GroupBy(Function(g) g.Genre).
            Select(Function(group) New StatsEntry With {.label = group.Key, .total = group.Count()}).
            ToListAsync()

        Return grouped.
            OrderByDescending(Function(entry) entry.total).
            ThenBy(Function(entry) entry.label).
            Take(limit).
            ToList()
    End Function

    Public Async Function GetByDecadeAsync(limit As Integer) As Task(Of IEnumerable(Of StatsEntry)) Implements IStatsRepository.GetByDecadeAsync
        Dim grouped = Await _context.games.
            Where(Function(g) g.ReleaseDate IsNot Nothing AndAlso g.ReleaseDate <> "" AndAlso
                g.ReleaseDate.Length >= 3 AndAlso ValidDecadePrefixes.Contains(g.ReleaseDate.Substring(0, 3))).
            GroupBy(Function(g) g.ReleaseDate.Substring(0, 3)).
            Select(Function(group) New StatsEntry With {.label = group.Key, .total = group.Count()}).
            ToListAsync()

        Return grouped.
            Select(Function(entry) New StatsEntry With {.label = entry.label & "0", .total = entry.total}).
            OrderByDescending(Function(entry) entry.label).
            Take(limit).
            ToList()
    End Function
End Class
