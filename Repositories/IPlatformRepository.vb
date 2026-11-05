Imports System.Collections.Generic
Imports System.Threading.Tasks

''' <summary>Acceso a datos de plataformas (EF Core).</summary>
Public Interface IPlatformRepository
    Function GetAllWithUrlAsync() As Task(Of IEnumerable(Of PlatformSummary))
    Function GetByIdAsync(id As Integer) As Task(Of Platform)
    Function GetTopGamesAsync(id As Integer) As Task(Of IEnumerable(Of TopGame))
    Function CreateAsync(platform As Platform) As Task(Of Platform)
    Function UpdateAsync(id As Integer, platform As Platform) As Task(Of Boolean)
    Function DeleteAsync(id As Integer) As Task(Of Boolean)
End Interface
