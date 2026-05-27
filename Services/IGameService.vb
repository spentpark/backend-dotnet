Imports System.Threading.Tasks

Public Interface IGameService
    Function GetGamesByPlatformAsync(platform As String, page As Integer, limit As Integer) As Task(Of PagedResult(Of GameSummary))
    Function SearchGamesByTitleAsync(title As String, page As Integer, limit As Integer) As Task(Of PagedResult(Of GameSummary))
    Function GetGameByIdAsync(id As Integer) As Task(Of Game)
End Interface