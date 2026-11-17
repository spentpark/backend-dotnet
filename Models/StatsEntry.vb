Imports System.Text.Json.Serialization

''' <summary>Fila de totales del catálogo (<c>GET /api/stats</c>).</summary>
Public Class StatsSummary
    Public Property totalGames As Integer
    Public Property scoredGames As Integer
    Public Property totalPlatforms As Integer
    Public Property totalGenres As Integer
End Class

''' <summary>Entrada de las distribuciones de <c>GET /api/stats</c>.</summary>
Public Class StatsEntry
    Public Property label As String
    Public Property total As Integer
End Class
