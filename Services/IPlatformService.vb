Imports System.Collections.Generic
Imports System.Threading.Tasks

''' <summary>Reglas de negocio de plataformas.</summary>
Public Interface IPlatformService
    Function GetPlatformsAsync() As Task(Of IEnumerable(Of PlatformSummary))
    Function GetPlatformByIdAsync(id As Integer) As Task(Of Platform)
    Function GetTopGamesAsync(id As Integer) As Task(Of IEnumerable(Of TopGame))
    Function CreatePlatformAsync(platform As Platform) As Task(Of Platform)
    Function UpdatePlatformAsync(id As Integer, platform As Platform) As Task(Of Boolean)
    Function DeletePlatformAsync(id As Integer) As Task(Of Boolean)
End Interface
