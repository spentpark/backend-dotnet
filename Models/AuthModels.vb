Imports System.Text.Json.Serialization

''' <summary>Cuerpo de <c>POST /api/auth/login</c>.</summary>
Public Class LoginRequest
    <JsonPropertyName("username")>
    Public Property username As String

    <JsonPropertyName("password")>
    Public Property password As String
End Class

''' <summary>Respuesta del login (mismo contrato que backend-node20).</summary>
Public Class LoginResponse
    <JsonPropertyName("token")>
    Public Property token As String

    <JsonPropertyName("tokenType")>
    Public Property tokenType As String = "Bearer"

    <JsonPropertyName("expiresAt")>
    Public Property expiresAt As String
End Class

''' <summary>Respuesta de <c>GET /api/auth/me</c>.</summary>
Public Class MeResponse
    <JsonPropertyName("username")>
    Public Property username As String

    <JsonPropertyName("role")>
    Public Property role As String
End Class
