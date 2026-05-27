Imports System.Threading.Tasks

Public Class GameService
    Implements IGameService

    Private ReadOnly _repository As IGameRepository

    Public Sub New(repository As IGameRepository)
        _repository = repository
    End Sub

    Public Async Function GetGamesByPlatformAsync(platform As String, page As Integer, limit As Integer) As Task(Of PagedResult(Of GameSummary)) Implements IGameService.GetGamesByPlatformAsync
        If page <= 0 Then page = 1
        If limit <= 0 Then limit = 20
        Return Await _repository.FindByPlatformPaginatedAsync(platform, page, limit)
    End Function

    Public Async Function SearchGamesByTitleAsync(title As String, page As Integer, limit As Integer) As Task(Of PagedResult(Of GameSummary)) Implements IGameService.SearchGamesByTitleAsync
        If page <= 0 Then page = 1
        If limit <= 0 Then limit = 20
        Return Await _repository.FindByTitlePaginatedAsync(title, page, limit)
    End Function

    Public Async Function GetGameByIdAsync(id As Integer) As Task(Of Game) Implements IGameService.GetGameByIdAsync
        Return Await _repository.GetByIdAsync(id)
    End Function
End Class