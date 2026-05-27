Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Class ReviewService
    Implements IReviewService

    Private ReadOnly _repository As IReviewRepository

    Public Sub New(repository As IReviewRepository)
        _repository = repository
    End Sub

    Public Async Function GetReviewsByGameIdAsync(gameId As Integer) As Task(Of IEnumerable(Of ReviewSummary)) Implements IReviewService.GetReviewsByGameIdAsync
        Return Await _repository.GetByGameIdAsync(gameId)
    End Function
End Class