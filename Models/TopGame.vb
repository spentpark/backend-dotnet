Imports System.Text.Json.Serialization

''' <summary>
''' Fila del top 10 de una plataforma (<c>GET /api/platforms/{id}/top-games</c>)
''' con los mismos nombres de columna que devuelve backend-node20.
''' </summary>
Public Class TopGame
    <JsonPropertyName("id")>
    Public Property id As Integer

    <JsonPropertyName("title")>
    Public Property title As String

    <JsonPropertyName("Platform")>
    Public Property Platform As String

    <JsonPropertyName("genre")>
    Public Property genre As String

    <JsonPropertyName("releasedate")>
    Public Property releasedate As String

    <JsonPropertyName("image_Original")>
    Public Property image_Original As String

    <JsonPropertyName("metacritic_score")>
    Public Property metacritic_score As String
End Class
