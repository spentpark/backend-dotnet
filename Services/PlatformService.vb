Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Class PlatformService
    Implements IPlatformService

    Private ReadOnly _repository As IPlatformRepository

    Public Sub New(repository As IPlatformRepository)
        _repository = repository
    End Sub

    Public Async Function GetActivePlatformsAsync() As Task(Of IEnumerable(Of PlatformSummary)) Implements IPlatformService.GetActivePlatformsAsync
        Return Await _repository.GetAllWithUrlAsync()
    End Function
End Class