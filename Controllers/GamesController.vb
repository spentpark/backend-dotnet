Imports System
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc

''' <summary>
''' CRUD de juegos (<c>/api/games</c>) con los mismos nombres de endpoint y
''' contrato que backend-node20. Todas las rutas exigen JWT; las escrituras,
''' rol <c>admin</c>.
''' </summary>
<Route("api/games")>
<ApiController>
<Authorize>
Public Class GamesController
    Inherits ControllerBase

    Private Const MaxTitleLength As Integer = 100

    Private ReadOnly _gameService As IGameService

    Public Sub New(gameService As IGameService)
        _gameService = gameService
    End Sub

    ''' <summary><c>GET /api/games?platform=...&amp;page=&amp;limit=</c></summary>
    <HttpGet>
    Public Async Function GetGames(
        <FromQuery> Optional platform As String = Nothing,
        <FromQuery> Optional genre As String = Nothing,
        <FromQuery> Optional year As String = Nothing,
        <FromQuery> Optional decade As String = Nothing,
        <FromQuery> Optional players As String = Nothing,
        <FromQuery> Optional sort As String = Nothing,
        <FromQuery> Optional page As Integer = 1,
        <FromQuery> Optional limit As Integer = 10
    ) As Task(Of IActionResult)

        Dim filters = New GameFilters With {
            .platform = platform,
            .genre = genre,
            .year = year,
            .decade = decade,
            .players = players,
            .sort = sort
        }

        Return Ok(Await _gameService.GetGamesAsync(filters, page, limit))
    End Function

    ''' <summary><c>GET /api/games/search?title=...</c> (title obligatorio).</summary>
    <HttpGet("search")>
    Public Async Function SearchGames(
        <FromQuery> Optional title As String = Nothing,
        <FromQuery> Optional page As Integer = 1,
        <FromQuery> Optional limit As Integer = 10
    ) As Task(Of IActionResult)

        If String.IsNullOrWhiteSpace(title) Then
            Return BadRequest(New With {.error = "El parámetro ""title"" es requerido"})
        End If

        If title.Length > MaxTitleLength Then title = title.Substring(0, MaxTitleLength)

        Return Ok(Await _gameService.SearchGamesAsync(title, page, limit))
    End Function

    ''' <summary><c>GET /api/games/random?platform=&amp;genre=</c></summary>
    <HttpGet("random")>
    Public Async Function GetRandomGame(
        <FromQuery> Optional platform As String = Nothing,
        <FromQuery> Optional genre As String = Nothing
    ) As Task(Of IActionResult)

        Dim game = Await _gameService.GetRandomGameAsync(
            New GameFilters With {.platform = platform, .genre = genre})

        If game Is Nothing Then
            Return NotFound(New With {.error = "No hay ningún juego con tráiler disponible"})
        End If

        Return Ok(game)
    End Function

    ''' <summary><c>GET /api/games/{id}</c></summary>
    <HttpGet("{id:int}")>
    Public Async Function GetGameById(<FromRoute> id As Integer) As Task(Of IActionResult)
        Dim game = Await _gameService.GetGameByIdAsync(id)

        If game Is Nothing Then
            Return NotFound(New With {.error = "Juego no encontrado"})
        End If

        Return Ok(game)
    End Function

    ''' <summary><c>POST /api/games</c> (rol admin).</summary>
    <HttpPost>
    <Authorize(Roles:="admin")>
    Public Async Function CreateGame(<FromBody> game As Game) As Task(Of IActionResult)
        If game Is Nothing Then
            Return StatusCode(500, New With {.error = "Error al crear el juego"})
        End If

        Dim created = Await _gameService.CreateGameAsync(game)
        Return StatusCode(201, created)
    End Function

    ''' <summary><c>PUT /api/games/{id}</c> (rol admin).</summary>
    <HttpPut("{id:int}")>
    <Authorize(Roles:="admin")>
    Public Async Function UpdateGame(<FromRoute> id As Integer, <FromBody> game As Game) As Task(Of IActionResult)
        Dim updated = Await _gameService.UpdateGameAsync(id, game)

        If Not updated Then
            Return NotFound(New With {.error = "Juego no encontrado"})
        End If

        Return Ok(New With {
            .message = "Juego actualizado",
            .result = New With {.affectedRows = 1}
        })
    End Function

    ''' <summary><c>DELETE /api/games/{id}</c> (rol admin).</summary>
    <HttpDelete("{id:int}")>
    <Authorize(Roles:="admin")>
    Public Async Function DeleteGame(<FromRoute> id As Integer) As Task(Of IActionResult)
        Dim deleted = Await _gameService.DeleteGameAsync(id)

        If Not deleted Then
            Return NotFound(New With {.error = "Juego no encontrado"})
        End If

        Return Ok(New With {
            .message = "Juego eliminado",
            .result = New With {.affectedRows = 1}
        })
    End Function
End Class
