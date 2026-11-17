Imports System
Imports System.Collections.Generic
Imports System.IdentityModel.Tokens.Jwt
Imports System.Security.Claims
Imports System.Security.Cryptography
Imports System.Text
Imports Microsoft.Extensions.Configuration
Imports Microsoft.IdentityModel.Tokens

''' <summary>Usuario autenticado (usuario estático configurado por entorno).</summary>
Public Class AuthUser
    Public Property username As String
    Public Property role As String
    Public Property passwordHash As String
End Class

''' <summary>Servicio de autenticación: login contra bcrypt y emisión de JWT HS256.</summary>
Public Interface IAuthService
    ''' <summary>Devuelve el usuario si las credenciales son válidas; <c>Nothing</c> si no.</summary>
    Function Login(username As String, password As String) As AuthUser

    ''' <summary>Firma un JWT HS256 con <c>sub</c> y claim <c>role</c>.</summary>
    Function IssueToken(user As AuthUser) As LoginResponse
End Interface

''' <summary>
''' Réplica de <c>auth.service.js</c> de backend-node20: bcrypt para la contraseña
''' y JWT HS256 con el mismo secreto, <c>issuer</c>, <c>audience</c> y claim
''' <c>role</c> que Node/rss-game-node.
''' </summary>
Public Class AuthService
    Implements IAuthService

    Private ReadOnly _config As IConfiguration

    Public Sub New(config As IConfiguration)
        _config = config
    End Sub

    Public Function Login(username As String, password As String) As AuthUser Implements IAuthService.Login
        Dim configured = New AuthUser With {
            .username = SecurityConfig.GetAuthUsername(_config),
            .role = SecurityConfig.GetAuthRole(_config),
            .passwordHash = SecurityConfig.GetAuthPasswordHash(_config)
        }

        If String.IsNullOrEmpty(configured.passwordHash) OrElse password Is Nothing Then
            Return Nothing
        End If

        ' Se verifica el hash aunque el usuario no coincida para no filtrar por tiempos.
        Dim passwordMatches As Boolean

        Try
            passwordMatches = BCrypt.Net.BCrypt.Verify(password, configured.passwordHash)
        Catch ex As Exception
            passwordMatches = False
        End Try

        Dim usernameMatches = FixedTimeEquals(username, configured.username)

        Return If(passwordMatches AndAlso usernameMatches, configured, Nothing)
    End Function

    Public Function IssueToken(user As AuthUser) As LoginResponse Implements IAuthService.IssueToken
        Dim secret = SecurityConfig.GetJwtSecret(_config)

        If String.IsNullOrWhiteSpace(secret) Then
            Throw New InvalidOperationException("JWT_SECRET no está configurado")
        End If

        Dim now = DateTimeOffset.UtcNow
        Dim expires = now.AddSeconds(SecurityConfig.GetExpiresInSeconds(_config))
        Dim key = New SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        Dim credentials = New SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        Dim claims As New List(Of Claim) From {
            New Claim("sub", user.username),
            New Claim("role", user.role),
            New Claim("iat", now.ToUnixTimeSeconds().ToString(Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        }

        Dim token = New JwtSecurityToken(
            SecurityConfig.GetJwtIssuer(_config),
            SecurityConfig.GetJwtAudience(_config),
            claims,
            now.UtcDateTime,
            expires.UtcDateTime,
            credentials)

        Return New LoginResponse With {
            .token = New JwtSecurityTokenHandler().WriteToken(token),
            .tokenType = "Bearer",
            .expiresAt = expires.UtcDateTime.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                Globalization.CultureInfo.InvariantCulture)
        }
    End Function

    ''' <summary>Compara cadenas en tiempo constante (evita filtrar el usuario).</summary>
    Private Shared Function FixedTimeEquals(left As String, right As String) As Boolean
        If left Is Nothing OrElse right Is Nothing Then Return False
        If left.Length <> right.Length Then Return False

        Return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right))
    End Function
End Class
