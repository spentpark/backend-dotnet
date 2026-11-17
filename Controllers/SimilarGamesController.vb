Imports System
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc

''' <summary>Juegos similares precalculados (<c>/api/games/{id}/similar</c>, JWT).</summary>
<Route("api/games")>
<ApiController>
<Authorize>
Public Class SimilarGamesController
    Inherits ControllerBase

    Private ReadOnly _similarGamesService As ISimilarGamesService

    Public Sub New(similarGamesService As ISimilarGamesService)
        _similarGamesService = similarGamesService
    End Sub

    <HttpGet("{id:int}/similar")>
    Public Async Function GetSimilarGames(
        <FromRoute> id As Integer,
        <FromQuery> Optional limit As String = Nothing
    ) As Task(Of IActionResult)

        Try
            Dim result = Await _similarGamesService.GetSimilarGamesAsync(id, ParseLimit(limit))

            If result Is Nothing Then
                Return NotFound(New With {
                    .error = "No se encontraron juegos similares para el juego indicado"
                })
            End If

            Return Ok(result)
        Catch ex As SimilarGamesParseException
            Return StatusCode(500, New With {.error = "Error al obtener juegos similares"})
        End Try
    End Function

    ''' <summary><c>limit</c> opcional: si no es un entero positivo se devuelven todos.</summary>
    Private Shared Function ParseLimit(value As String) As Integer?
        Dim parsed As Integer

        If Integer.TryParse(value, parsed) AndAlso parsed > 0 Then
            Return parsed
        End If

        Return Nothing
    End Function
End Class
