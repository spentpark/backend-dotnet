Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Text.Json.Serialization

''' <summary>
''' Entidad de la tabla <c>similar_game</c>: una fila por juego con un TEXT JSON
''' <c>[{ id, title, similarity }]</c> ya calculado por el motor de recomendación.
''' </summary>
<Table("similar_game")>
Public Class SimilarGame
    <Key>
    <Column("id_game")>
    Public Property IdGame As Integer

    <Column("json")>
    Public Property Json As String
End Class

''' <summary>Entrada normalizada de <c>similar_game</c> tal y como la devuelve Node.</summary>
Public Class SimilarGameEntry
    <JsonPropertyName("id")>
    Public Property id As Integer

    <JsonPropertyName("title")>
    Public Property title As String

    <JsonPropertyName("similarity")>
    Public Property similarity As Double

    <JsonPropertyName("similarityPercentage")>
    Public Property similarityPercentage As Double
End Class
