Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Text.RegularExpressions
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.Logging

''' <summary>
''' Configuración de seguridad compartida con backend-node20: JWT HS256 (mismo
''' secreto, <c>issuer</c> y <c>audience</c>), usuario estático, CORS, rate
''' limiting, límite de cuerpo y Swagger por entorno.
'''
''' Las variables se leen con los mismos nombres que usa Node (<c>JWT_SECRET</c>,
''' <c>AUTH_PASSWORD_HASH</c>…), por lo que un token emitido por cualquiera de los
''' servicios es válido en los demás.
''' </summary>
Public Module SecurityConfig
    Public Const DefaultIssuer As String = "rss-game-node"
    Public Const DefaultAudience As String = "rss-game-node-api"
    Public Const DefaultUsername As String = "admin"
    Public Const DefaultRole As String = "admin"
    Public Const DefaultExpiresIn As String = "1h"
    Public Const MinSecretLength As Integer = 32
    Public Const DefaultTokenSeconds As Integer = 3600

    Public Const MaxBodyBytes As Integer = 10 * 1024
    Public Const BodyTooLargeMessage As String = "El cuerpo de la petición es demasiado grande"
    Public Const InvalidRequestMessage As String = "Petición inválida"
    Public Const InternalErrorMessage As String = "Error interno del servidor"
    Public Const NotFoundMessage As String = "Ruta no encontrada"
    Public Const UnauthorizedMessage As String = "No autorizado"
    Public Const ForbiddenMessage As String = "Acceso denegado"
    Public Const ApiRateLimitMessage As String = "Demasiadas solicitudes. Inténtalo más tarde."
    Public Const LoginRateLimitMessage As String = "Demasiados intentos de inicio de sesión. Inténtalo más tarde."

    Public Const DefaultApiWindowSeconds As Integer = 900
    Public Const DefaultApiMax As Integer = 300
    Public Const DefaultAuthWindowSeconds As Integer = 900
    Public Const DefaultAuthMax As Integer = 5

    ' Allowlist por defecto (mismos orígenes que backend-node20 / backend-python).
    Public ReadOnly DefaultAllowedOrigins As String() = {
        "http://localhost:4200", "http://127.0.0.1:4200",
        "http://localhost:4300", "http://127.0.0.1:4300",
        "http://localhost:4000", "http://127.0.0.1:4000",
        "http://localhost:5173", "http://127.0.0.1:5173",
        "http://localhost:4173", "http://127.0.0.1:4173",
        "http://localhost:8065", "http://127.0.0.1:8065",
        "http://localhost", "http://127.0.0.1",
        "http://192.168.1.82:4200", "http://192.168.1.82:4300", "http://192.168.1.82:8065",
        "http://200.83.105.123:4200", "http://200.83.105.123:4300", "http://200.83.105.123:8065",
        "https://tudominio.com"
    }

    Private ReadOnly LocalOriginPattern As New Regex(
        "^http://(localhost|127\.0\.0\.1)(:\d+)?$",
        RegexOptions.IgnoreCase Or RegexOptions.Compiled)

    Private ReadOnly DurationPattern As New Regex(
        "^\s*(\d+)\s*([smhd]?)\s*$",
        RegexOptions.IgnoreCase Or RegexOptions.Compiled)

    ''' <summary>Lee una cadena de configuración; <c>fallback</c> si falta o está en blanco.</summary>
    Public Function GetValue(config As IConfiguration, name As String, fallback As String) As String
        Dim value = config(name)

        Return If(String.IsNullOrWhiteSpace(value), fallback, value.Trim())
    End Function

    ''' <summary>Lee un entero positivo; si no es válido usa el fallback.</summary>
    Public Function GetPositiveInt(config As IConfiguration, name As String, fallback As Integer) As Integer
        Dim raw = config(name)
        Dim parsed As Integer

        If String.IsNullOrWhiteSpace(raw) OrElse Not Integer.TryParse(raw, parsed) OrElse parsed <= 0 Then
            Return fallback
        End If

        Return parsed
    End Function

    ''' <summary><c>True</c> si el entorno activo es producción.</summary>
    Public Function IsProduction(config As IConfiguration) As Boolean
        Return String.Equals(GetEnvironmentName(config), "production", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>Nombre del entorno (<c>ENVIRONMENT</c>, <c>NODE_ENV</c> o <c>ASPNETCORE_ENVIRONMENT</c>).</summary>
    Public Function GetEnvironmentName(config As IConfiguration) As String
        Dim value = config("ENVIRONMENT")

        If String.IsNullOrWhiteSpace(value) Then value = config("NODE_ENV")
        If String.IsNullOrWhiteSpace(value) Then value = config("ASPNETCORE_ENVIRONMENT")
        If String.IsNullOrWhiteSpace(value) Then value = "development"

        Return value.Trim()
    End Function

    Public Function GetJwtSecret(config As IConfiguration) As String
        Return GetValue(config, "JWT_SECRET", "")
    End Function

    Public Function GetJwtIssuer(config As IConfiguration) As String
        Return GetValue(config, "JWT_ISSUER", DefaultIssuer)
    End Function

    Public Function GetJwtAudience(config As IConfiguration) As String
        Return GetValue(config, "JWT_AUDIENCE", DefaultAudience)
    End Function

    Public Function GetJwtExpiresIn(config As IConfiguration) As String
        Return GetValue(config, "JWT_EXPIRES_IN", DefaultExpiresIn)
    End Function

    ''' <summary>Convierte <c>1h</c>/<c>30m</c>/<c>3600</c> a segundos (por defecto 1 h).</summary>
    Public Function GetExpiresInSeconds(config As IConfiguration) As Integer
        Dim raw = GetJwtExpiresIn(config)
        Dim match = DurationPattern.Match(raw)

        If Not match.Success Then Return DefaultTokenSeconds

        Dim amount As Integer

        If Not Integer.TryParse(match.Groups(1).Value, amount) OrElse amount <= 0 Then
            Return DefaultTokenSeconds
        End If

        Select Case match.Groups(2).Value.ToLowerInvariant()
            Case "d" : Return amount * 86400
            Case "h" : Return amount * 3600
            Case "m" : Return amount * 60
            Case Else : Return amount
        End Select
    End Function

    Public Function GetAuthUsername(config As IConfiguration) As String
        Return GetValue(config, "AUTH_USERNAME", DefaultUsername)
    End Function

    Public Function GetAuthRole(config As IConfiguration) As String
        Return GetValue(config, "AUTH_ROLE", DefaultRole)
    End Function

    Public Function GetAuthPasswordHash(config As IConfiguration) As String
        Return GetValue(config, "AUTH_PASSWORD_HASH", "")
    End Function

    ''' <summary>
    ''' Valida la configuración de autenticación: en producción falla cerrado si
    ''' falta el secreto o el hash; en desarrollo solo registra un aviso.
    ''' </summary>
    Public Function ValidateAuthConfiguration(config As IConfiguration, logger As ILogger) As Boolean
        Dim missing As New List(Of String)()

        If String.IsNullOrWhiteSpace(GetJwtSecret(config)) Then missing.Add("JWT_SECRET")
        If String.IsNullOrWhiteSpace(GetAuthPasswordHash(config)) Then missing.Add("AUTH_PASSWORD_HASH")

        If missing.Count > 0 Then
            If IsProduction(config) Then
                Throw New InvalidOperationException(
                    "Configuración de autenticación incompleta: " & String.Join(", ", missing))
            End If

            logger.LogWarning(
                "Autenticación incompleta ({Missing}): el login fallará hasta configurarla",
                String.Join(", ", missing))
            Return False
        End If

        If GetJwtSecret(config).Length < MinSecretLength Then
            If IsProduction(config) Then
                Throw New InvalidOperationException(
                    $"JWT_SECRET debe tener al menos {MinSecretLength} caracteres")
            End If

            logger.LogWarning("JWT_SECRET es demasiado corto: usa al menos {Length} caracteres", MinSecretLength)
        End If

        Return True
    End Function

    ''' <summary>Swagger en <c>/api-docs</c>: apagado en producción salvo <c>API_DOCS_ENABLED=true</c>.</summary>
    Public Function IsDocsEnabled(config As IConfiguration) As Boolean
        Dim configured = GetValue(config, "API_DOCS_ENABLED", "").ToLowerInvariant()

        If configured = "false" Then Return False
        If IsProduction(config) Then Return configured = "true"

        Return True
    End Function

    ''' <summary>Orígenes permitidos por CORS (<c>CORS_ORIGINS</c> los sobreescribe).</summary>
    Public Function GetAllowedOrigins(config As IConfiguration) As String()
        Dim raw = config("CORS_ORIGINS")

        If Not String.IsNullOrWhiteSpace(raw) Then
            Return raw.Split(","c).
                Select(Function(origin) origin.Trim()).
                Where(Function(origin) origin.Length > 0).
                ToArray()
        End If

        Return DefaultAllowedOrigins
    End Function

    ''' <summary>Valida un origen CORS: allowlist + localhost en desarrollo.</summary>
    Public Function IsAllowedOrigin(origin As String, config As IConfiguration) As Boolean
        If String.IsNullOrWhiteSpace(origin) Then Return False

        If GetAllowedOrigins(config).Any(Function(allowed) String.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase)) Then
            Return True
        End If

        Return Not IsProduction(config) AndAlso LocalOriginPattern.IsMatch(origin)
    End Function
End Module
