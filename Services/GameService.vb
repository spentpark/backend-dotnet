Imports System
Imports System.Threading.Tasks

''' <summary>
''' Servicio de juegos: normaliza la paginación (page ≥ 1, limit por defecto 10),
''' delega los filtros en el repositorio y mapea el juego aleatorio al contrato
''' camelCase de backend-node20.
''' </summary>
Public Class GameService
    Implements IGameService

    Public Const DefaultLimit As Integer = 10

    Private ReadOnly _repository As IGameRepository

    Public Sub New(repository As IGameRepository)
        _repository = repository
    End Sub

    Public Async Function GetGamesAsync(filters As GameFilters, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of GameSummary)) Implements IGameService.GetGamesAsync
        Return Await _repository.FindSummariesPaginatedAsync(filters, NormalizePage(page), NormalizeLimit(limit))
    End Function

    Public Async Function SearchGamesAsync(title As String, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of GameSummary)) Implements IGameService.SearchGamesAsync
        Return Await _repository.FindByTitlePaginatedAsync(title, NormalizePage(page), NormalizeLimit(limit))
    End Function

    Public Async Function GetRandomGameAsync(filters As GameFilters) As Task(Of GameRandom) Implements IGameService.GetRandomGameAsync
        Dim game = Await _repository.FindRandomAsync(filters)

        If game Is Nothing Then Return Nothing

        Return New GameRandom With {
            .id = game.Id,
            .title = game.Title,
            .description = game.Description,
            .imageMedium = game.Image_Medium,
            .imageLarge = game.Image_Large,
            .imageOriginal = game.Image_Original,
            .imageFront = game.Image_Front,
            .platform = game.Platform,
            .publisher = game.Publisher,
            .releasedate = game.ReleaseDate,
            .players = game.Players,
            .genre = game.Genre,
            .youtubeTrailer = game.Youtube_Trailer
        }
    End Function

    Public Async Function GetGameByIdAsync(id As Integer) As Task(Of Game) Implements IGameService.GetGameByIdAsync
        Return Await _repository.GetByIdAsync(id)
    End Function

    Public Async Function CreateGameAsync(game As Game) As Task(Of Game) Implements IGameService.CreateGameAsync
        Return Await _repository.CreateAsync(game)
    End Function

    Public Async Function UpdateGameAsync(id As Integer, game As Game) As Task(Of Boolean) Implements IGameService.UpdateGameAsync
        Return Await _repository.UpdateAsync(id, game)
    End Function

    Public Async Function DeleteGameAsync(id As Integer) As Task(Of Boolean) Implements IGameService.DeleteGameAsync
        Return Await _repository.DeleteAsync(id)
    End Function

    Private Shared Function NormalizePage(page As Integer) As Integer
        Return If(page <= 0, 1, page)
    End Function

    Private Shared Function NormalizeLimit(limit As Integer) As Integer
        Return If(limit <= 0, DefaultLimit, limit)
    End Function
End Class
