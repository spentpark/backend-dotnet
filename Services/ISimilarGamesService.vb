Imports System.Collections.Generic
Imports System.Threading.Tasks

''' <summary>Resultado de <c>GET /api/games/{id}/similar</c>.</summary>
Public Class SimilarGamesResult
    Public Property gameId As Integer
    Public Property total As Integer
    Public Property similarGames As List(Of SimilarGameEntry)
End Class

''' <summary>El campo <c>json</c> de <c>similar_game</c> no contiene un array válido.</summary>
Public Class SimilarGamesParseException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

''' <summary>Reglas de negocio de los juegos similares precalculados.</summary>
Public Interface ISimilarGamesService
    Function GetSimilarGamesAsync(gameId As Integer, limit As Integer?) As Task(Of SimilarGamesResult)
End Interface
