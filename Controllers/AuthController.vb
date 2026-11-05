Imports System
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc

''' <summary>Endpoints de autenticación (<c>/api/auth</c>), igual que backend-node20.</summary>
<Route("api/auth")>
<ApiController>
Public Class AuthController
    Inherits ControllerBase

    Private Const MaxUsernameLength As Integer = 100
    Private Const MaxPasswordLength As Integer = 200

    Private ReadOnly _authService As IAuthService

    Public Sub New(authService As IAuthService)
        _authService = authService
    End Sub

    ''' <summary>Valida usuario/contraseña (bcrypt) y emite un JWT HS256.</summary>
    <HttpPost("login")>
    <AllowAnonymous>
    Public Function Login(<FromBody> request As LoginRequest) As IActionResult
        Dim username = If(request?.username, "")
        Dim password = If(request?.password, "")

        If String.IsNullOrEmpty(username) OrElse String.IsNullOrEmpty(password) Then
            Return BadRequest(New With {.error = "Usuario y contraseña son obligatorios"})
        End If

        If username.Length > MaxUsernameLength OrElse password.Length > MaxPasswordLength Then
            Return BadRequest(New With {.error = "Credenciales inválidas"})
        End If

        Dim user = _authService.Login(username, password)

        If user Is Nothing Then
            SetNoStoreHeaders()
            Return Unauthorized(New With {.error = "Credenciales inválidas"})
        End If

        Try
            Dim issued = _authService.IssueToken(user)
            SetNoStoreHeaders()
            Return Ok(issued)
        Catch ex As Exception
            Return StatusCode(500, New With {.error = "Error al iniciar sesión"})
        End Try
    End Function

    ''' <summary>Devuelve <c>{username, role}</c> a partir del JWT recibido.</summary>
    <HttpGet("me")>
    <Authorize>
    Public Function GetMe() As IActionResult
        SetNoStoreHeaders()

        Return Ok(New MeResponse With {
            .username = User.FindFirst("sub")?.Value,
            .role = User.FindFirst("role")?.Value
        })
    End Function

    ''' <summary>Evita que el token quede en cachés intermedias o del navegador.</summary>
    Private Sub SetNoStoreHeaders()
        Response.Headers("Cache-Control") = "no-store"
        Response.Headers("Pragma") = "no-cache"
    End Sub
End Class
