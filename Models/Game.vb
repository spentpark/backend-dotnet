Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Text.Json.Serialization

''' <summary>
''' Entidad de la tabla <c>games</c>. Los <c>JsonPropertyName</c> replican los
''' nombres de columna reales que devuelve backend-node20 en el detalle
''' (<c>Platform</c>, <c>releasedate</c>, <c>image_*</c>, <c>metacritic_score</c>…).
''' </summary>
Public Class Game
    <JsonPropertyName("id")>
    Public Property Id As Integer

    <JsonPropertyName("title")>
    Public Property Title As String

    <JsonPropertyName("description")>
    Public Property Description As String

    <Column("GameDBId")>
    <JsonPropertyName("GameDBId")>
    Public Property GameDBId As Integer?

    <Column("image_Large")>
    <JsonPropertyName("image_Large")>
    Public Property Image_Large As String

    <Column("image_Medium")>
    <JsonPropertyName("image_Medium")>
    Public Property Image_Medium As String

    <Column("image_Original")>
    <JsonPropertyName("image_Original")>
    Public Property Image_Original As String

    <Column("image_Front")>
    <JsonPropertyName("image_Front")>
    Public Property Image_Front As String

    <JsonPropertyName("Platform")>
    Public Property Platform As String

    <JsonPropertyName("Publisher")>
    Public Property Publisher As String

    <Column("releasedate")>
    <JsonPropertyName("releasedate")>
    Public Property ReleaseDate As String

    <JsonPropertyName("players")>
    Public Property Players As String

    <JsonPropertyName("genre")>
    Public Property Genre As String

    <Column("youtube_Trailer")>
    <JsonPropertyName("youtube_Trailer")>
    Public Property Youtube_Trailer As String

    <Column("youtube_Walk")>
    <JsonPropertyName("youtube_Walk")>
    Public Property Youtube_Walk As String

    <Column("wiki_url")>
    <JsonPropertyName("wiki_url")>
    Public Property Wiki_Url As String

    <Column("wiki_page")>
    <JsonPropertyName("wiki_page")>
    Public Property Wiki_Page As String

    <Column("youtube_ending")>
    <JsonPropertyName("youtube_ending")>
    Public Property Youtube_Ending As String

    <Column("youtube_secrets")>
    <JsonPropertyName("youtube_secrets")>
    Public Property Youtube_Secrets As String

    <Column("youtube_ost")>
    <JsonPropertyName("youtube_ost")>
    Public Property Youtube_Ost As String

    <Column("youtube_speedrun")>
    <JsonPropertyName("youtube_speedrun")>
    Public Property Youtube_Speedrun As String

    <Column("youtube_review")>
    <JsonPropertyName("youtube_review")>
    Public Property Youtube_Review As String

    <Column("spotify_ost")>
    <JsonPropertyName("spotify_ost")>
    Public Property Spotify_Ost As String

    <Column("ign_url")>
    <JsonPropertyName("ign_url")>
    Public Property Ign_Url As String

    <Column("metacritic_url")>
    <JsonPropertyName("metacritic_url")>
    Public Property Metacritic_Url As String

    <Column("metacritic_score")>
    <JsonPropertyName("metacritic_score")>
    Public Property Metacritic_Score As String

    <Column("metacritic_scoreu")>
    <JsonPropertyName("metacritic_scoreu")>
    Public Property Metacritic_ScoreU As String

    <Column("3djuegos_url")>
    <JsonPropertyName("3djuegos_url")>
    Public Property _3djuegos_Url As String

    <Column("areajugones_url")>
    <JsonPropertyName("areajugones_url")>
    Public Property Areajugones_Url As String

    <Column("meristation_url")>
    <JsonPropertyName("meristation_url")>
    Public Property Meristation_Url As String

    <Column("3djuegos_score")>
    <JsonPropertyName("3djuegos_score")>
    Public Property _3djuegos_Score As String

    <Column("opencritic_url")>
    <JsonPropertyName("opencritic_url")>
    Public Property Opencritic_Url As String

    <Column("esrb_letter")>
    <JsonPropertyName("esrb_letter")>
    Public Property Esrb_Letter As String

    <Column("esbr_message")>
    <JsonPropertyName("esbr_message")>
    Public Property Esbr_Message As String
End Class
