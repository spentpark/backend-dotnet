Imports Microsoft.AspNetCore.Mvc

<Route("api/games")>
<ApiController>
Public Class GamesController
    Inherits ControllerBase

    Private ReadOnly _gameService As IGameService

    Public Sub New(gameService As IGameService)
        _gameService = gameService
    End Sub

    <HttpGet("platform")>
    Public Async Function FindByPlatformPaginated(
        <FromQuery> platform As String,
        <FromQuery> page As Integer,
        <FromQuery> limit As Integer
    ) As Task(Of IActionResult)

        Dim result = Await _gameService.GetGamesByPlatformAsync(platform, page, limit)
        Return Ok(result)

    End Function

    <HttpGet("search")>
    Public Async Function FindByTitlePaginated(
        <FromQuery> title As String,
        <FromQuery> page As Integer,
        <FromQuery> limit As Integer
    ) As Task(Of IActionResult)

        Dim result = Await _gameService.SearchGamesByTitleAsync(title, page, limit)
        Return Ok(result)

    End Function

    <HttpGet("{id}")>
    Public Async Function GetById(<FromRoute> id As Integer) As Task(Of IActionResult)

        Dim game = Await _gameService.GetGameByIdAsync(id)

        If game Is Nothing Then
            Return NotFound()
        End If

        Return Ok(game)

    End Function

End Class