Imports System.ComponentModel.DataAnnotations.Schema

Public Class Game
    Public Property Id As Integer
    Public Property Title As String
    Public Property Description As String
    <Column("GameDBId")>
    Public Property GameDBId As Integer?
    <Column("image_Large")>
    Public Property Image_Large As String
    <Column("image_Medium")>
    Public Property Image_Medium As String
    <Column("image_Original")>
    Public Property Image_Original As String
    <Column("image_Front")>
    Public Property Image_Front As String
    Public Property Platform As String
    Public Property Publisher As String
    <Column("releasedate")>
    Public Property ReleaseDate As String
    Public Property Players As String
    Public Property Genre As String
    <Column("youtube_Trailer")>
    Public Property Youtube_Trailer As String
    <Column("youtube_Walk")>
    Public Property Youtube_Walk As String
    <Column("wiki_url")>
    Public Property Wiki_Url As String
    <Column("wiki_page")>
    Public Property Wiki_Page As String
    <Column("youtube_ending")>
    Public Property Youtube_Ending As String
    <Column("youtube_secrets")>
    Public Property Youtube_Secrets As String
    <Column("youtube_ost")>
    Public Property Youtube_Ost As String
    <Column("youtube_speedrun")>
    Public Property Youtube_Speedrun As String
    <Column("youtube_review")>
    Public Property Youtube_Review As String
    <Column("spotify_ost")>
    Public Property Spotify_Ost As String
    <Column("ign_url")>
    Public Property Ign_Url As String
    <Column("metacritic_url")>
    Public Property Metacritic_Url As String
    <Column("metacritic_score")>
    Public Property Metacritic_Score As String
    <Column("metacritic_scoreu")>
    Public Property Metacritic_ScoreU As String
    <Column("3djuegos_url")>
    Public Property _3djuegos_Url As String
    <Column("areajugones_url")>
    Public Property Areajugones_Url As String
    <Column("meristation_url")>
    Public Property Meristation_Url As String
    <Column("3djuegos_score")>
    Public Property _3djuegos_Score As String
    <Column("opencritic_url")>
    Public Property Opencritic_Url As String
    <Column("esrb_letter")>
    Public Property Esrb_Letter As String
    <Column("esbr_message")>
    Public Property Esbr_Message As String
End Class