Imports System.Collections.Generic
Imports System.Threading.Tasks

''' <summary>Acceso a datos de las estadísticas del catálogo.</summary>
Public Interface IStatsRepository
    Function GetSummaryAsync() As Task(Of StatsSummary)
    Function GetByPlatformAsync(limit As Integer) As Task(Of IEnumerable(Of StatsEntry))
    Function GetByGenreAsync(limit As Integer) As Task(Of IEnumerable(Of StatsEntry))
    Function GetByDecadeAsync(limit As Integer) As Task(Of IEnumerable(Of StatsEntry))
End Interface
