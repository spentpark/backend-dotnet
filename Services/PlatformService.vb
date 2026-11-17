Imports System.Collections.Generic
Imports System.Threading.Tasks

''' <summary>Servicio de plataformas: delega en el repositorio.</summary>
Public Class PlatformService
    Implements IPlatformService

    Private ReadOnly _repository As IPlatformRepository

    Public Sub New(repository As IPlatformRepository)
        _repository = repository
    End Sub

    Public Async Function GetPlatformsAsync() As Task(Of IEnumerable(Of PlatformSummary)) Implements IPlatformService.GetPlatformsAsync
        Return Await _repository.GetAllWithUrlAsync()
    End Function

    Public Async Function GetPlatformByIdAsync(id As Integer) As Task(Of Platform) Implements IPlatformService.GetPlatformByIdAsync
        Return Await _repository.GetByIdAsync(id)
    End Function

    Public Async Function GetTopGamesAsync(id As Integer) As Task(Of IEnumerable(Of TopGame)) Implements IPlatformService.GetTopGamesAsync
        Return Await _repository.GetTopGamesAsync(id)
    End Function

    Public Async Function CreatePlatformAsync(platform As Platform) As Task(Of Platform) Implements IPlatformService.CreatePlatformAsync
        Return Await _repository.CreateAsync(platform)
    End Function

    Public Async Function UpdatePlatformAsync(id As Integer, platform As Platform) As Task(Of Boolean) Implements IPlatformService.UpdatePlatformAsync
        Return Await _repository.UpdateAsync(id, platform)
    End Function

    Public Async Function DeletePlatformAsync(id As Integer) As Task(Of Boolean) Implements IPlatformService.DeletePlatformAsync
        Return Await _repository.DeleteAsync(id)
    End Function
End Class
