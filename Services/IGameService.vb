Imports System.Threading.Tasks

''' <summary>Reglas de negocio de juegos.</summary>
Public Interface IGameService
    Function GetGamesAsync(filters As GameFilters, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of GameSummary))
    Function SearchGamesAsync(title As String, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of GameSummary))
    Function GetRandomGameAsync(filters As GameFilters) As Task(Of GameRandom)
    Function GetGameByIdAsync(id As Integer) As Task(Of Game)
    Function CreateGameAsync(game As Game) As Task(Of Game)
    Function UpdateGameAsync(id As Integer, game As Game) As Task(Of Boolean)
    Function DeleteGameAsync(id As Integer) As Task(Of Boolean)
End Interface
