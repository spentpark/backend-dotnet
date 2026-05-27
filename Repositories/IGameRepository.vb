Imports System.Threading.Tasks

Public Interface IGameRepository
    Function FindByPlatformPaginatedAsync(platform As String, page As Integer, limit As Integer) As Task(Of PagedResult(Of GameSummary))
    Function FindByTitlePaginatedAsync(title As String, page As Integer, limit As Integer) As Task(Of PagedResult(Of GameSummary))
    Function GetByIdAsync(id As Integer) As Task(Of Game)
End Interface