Imports System.Text.Json.Serialization

''' <summary>Entidad de la tabla <c>platform</c>.</summary>
Public Class Platform
    <JsonPropertyName("id")>
    Public Property Id As Integer

    <JsonPropertyName("description")>
    Public Property Description As String

    <JsonPropertyName("url")>
    Public Property Url As String
End Class
