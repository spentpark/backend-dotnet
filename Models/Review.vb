Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Text.Json.Serialization

''' <summary>
''' Entidad de la tabla <c>review</c> de producción (reseñas de prensa):
''' columnas <c>name</c>, <c>source</c>, <c>review</c>, <c>score</c> (VARCHAR),
''' <c>date</c> e <c>id_game</c>. Los nombres JSON replican los de backend-node20.
''' </summary>
<Table("review")>
Public Class Review
    <JsonPropertyName("id")>
    Public Property Id As Integer

    <Column("id_game")>
    <JsonPropertyName("id_game")>
    Public Property IdGame As Integer

    <JsonPropertyName("name")>
    Public Property Name As String

    <JsonPropertyName("source")>
    Public Property Source As String

    <Column("review")>
    <JsonPropertyName("review")>
    Public Property Text As String

    <JsonPropertyName("score")>
    Public Property Score As String

    <Column("date")>
    <JsonPropertyName("date")>
    Public Property ReviewDate As String
End Class
