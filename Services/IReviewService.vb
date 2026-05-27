Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Interface IReviewService
    Function GetReviewsByGameIdAsync(gameId As Integer) As Task(Of IEnumerable(Of ReviewSummary))
End Interface