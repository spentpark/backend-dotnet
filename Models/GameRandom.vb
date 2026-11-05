Imports System.Text.Json.Serialization

''' <summary>
''' Juego aleatorio con tráiler (<c>GET /api/games/random</c>) con el contrato
''' camelCase de backend-node20.
''' </summary>
Public Class GameRandom
    <JsonPropertyName("id")>
    Public Property id As Integer

    <JsonPropertyName("title")>
    Public Property title As String

    <JsonPropertyName("description")>
    Public Property description As String

    <JsonPropertyName("imageMedium")>
    Public Property imageMedium As String

    <JsonPropertyName("imageLarge")>
    Public Property imageLarge As String

    <JsonPropertyName("imageOriginal")>
    Public Property imageOriginal As String

    <JsonPropertyName("imageFront")>
    Public Property imageFront As String

    <JsonPropertyName("platform")>
    Public Property platform As String

    <JsonPropertyName("publisher")>
    Public Property publisher As String

    <JsonPropertyName("releasedate")>
    Public Property releasedate As String

    <JsonPropertyName("players")>
    Public Property players As String

    <JsonPropertyName("genre")>
    Public Property genre As String

    <JsonPropertyName("youtubeTrailer")>
    Public Property youtubeTrailer As String
End Class
