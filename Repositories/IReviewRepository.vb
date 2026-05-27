Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Interface IReviewRepository
    Function GetByGameIdAsync(gameId As Integer) As Task(Of IEnumerable(Of ReviewSummary))
End Interface