Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.Logging

''' <summary>Cabeceras equivalentes a las que añade <c>helmet()</c> en Express.</summary>
Public Class SecurityHeadersMiddleware
    Private ReadOnly _next As RequestDelegate

    Public Sub New(nextDelegate As RequestDelegate)
        _next = nextDelegate
    End Sub

    Private Shared ReadOnly Headers As Dictionary(Of String, String) = New Dictionary(Of String, String) From {
        {"X-Content-Type-Options", "nosniff"},
        {"X-Frame-Options", "SAMEORIGIN"},
        {"Referrer-Policy", "no-referrer"},
        {"Cross-Origin-Opener-Policy", "same-origin"},
        {"Cross-Origin-Resource-Policy", "same-origin"},
        {"Origin-Agent-Cluster", "?1"},
        {"X-DNS-Prefetch-Control", "off"},
        {"X-Download-Options", "noopen"},
        {"X-Permitted-Cross-Domain-Policies", "none"},
        {"Strict-Transport-Security", "max-age=31536000; includeSubDomains"}
    }

    Public Const ContentSecurityPolicy As String =
        "default-src 'self';" &
        "base-uri 'self';" &
        "font-src 'self' https: data:;" &
        "form-action 'self';" &
        "frame-ancestors 'self';" &
        "img-src 'self' data:;" &
        "object-src 'none';" &
        "script-src 'self';" &
        "script-src-attr 'none';" &
        "style-src 'self' https: 'unsafe-inline';" &
        "upgrade-insecure-requests"

    Public Async Function InvokeAsync(context As HttpContext) As Task
        ' Las cabeceras se escriben antes de continuar el pipeline: despues la
        ' respuesta puede haberse iniciado y ya serian de solo lectura.
        For Each header In Headers
            If Not context.Response.Headers.ContainsKey(header.Key) Then
                context.Response.Headers(header.Key) = header.Value
            End If
        Next

        ' Swagger UI usa scripts y estilos inline: se sirve sin CSP, como en Node.
        If Not context.Request.Path.StartsWithSegments("/api-docs") Then
            If Not context.Response.Headers.ContainsKey("Content-Security-Policy") Then
                context.Response.Headers("Content-Security-Policy") = ContentSecurityPolicy
            End If
        End If

        Await _next(context)
    End Function
End Class

''' <summary>
''' Rechaza cuerpos con <c>Content-Length</c> mayor de 10 kb (413), equivalente a
''' <c>express.json({ limit: '10kb' })</c>.
''' </summary>
Public Class BodyLimitMiddleware
    Private ReadOnly _next As RequestDelegate

    Public Sub New(nextDelegate As RequestDelegate)
        _next = nextDelegate
    End Sub

    Public Async Function InvokeAsync(context As HttpContext) As Task
        Dim contentLength = context.Request.ContentLength

        If contentLength.HasValue AndAlso contentLength.Value > SecurityConfig.MaxBodyBytes Then
            context.Response.StatusCode = 413
            context.Response.ContentType = "application/json"
            Await context.Response.WriteAsync("{""error"":""" & SecurityConfig.BodyTooLargeMessage & """}")
            Return
        End If

        Await _next(context)
    End Function
End Class

''' <summary>Opciones del rate limiter (ventana fija por IP, igual que express-rate-limit).</summary>
Public Class RateLimitOptions
    Public Property windowSeconds As Integer = SecurityConfig.DefaultApiWindowSeconds
    Public Property limit As Integer = SecurityConfig.DefaultApiMax
    Public Property message As String = SecurityConfig.ApiRateLimitMessage
    Public Property pathPrefix As String = "/api"
    Public Property method As String = ""
    Public Property trustProxy As Boolean = False
End Class

''' <summary>
''' Rate limiting en memoria por IP (ventana fija). Global de la API: 300/15 min;
''' login: 5/15 min. Responde 429 con <c>RateLimit-*</c> y <c>Retry-After</c>.
''' </summary>
Public Class RateLimitMiddleware
    Private Const MaxBuckets As Integer = 10000

    Private ReadOnly _next As RequestDelegate
    Private ReadOnly _options As RateLimitOptions
    Private ReadOnly _buckets As New Dictionary(Of String, RateBucket)()
    Private ReadOnly _sync As New Object()

    Public Sub New(nextDelegate As RequestDelegate, options As RateLimitOptions)
        _next = nextDelegate
        _options = options
    End Sub

    Private Class RateBucket
        Public WindowStart As Double
        Public Count As Integer
    End Class

    Public Async Function InvokeAsync(context As HttpContext) As Task
        If Not context.Request.Path.StartsWithSegments(_options.pathPrefix) Then
            Await _next(context)
            Return
        End If

        If Not String.IsNullOrEmpty(_options.method) AndAlso
            Not String.Equals(context.Request.Method, _options.method, StringComparison.OrdinalIgnoreCase) Then
            Await _next(context)
            Return
        End If

        Dim now = MonotonicSeconds()
        Dim key = ClientKey(context)
        Dim bucket As RateBucket = Nothing
        Dim retryAfter As Integer

        SyncLock _sync
            Prune(now)

            If Not _buckets.TryGetValue(key, bucket) Then
                bucket = New RateBucket With {.WindowStart = now, .Count = 0}
            End If

            If now - bucket.WindowStart >= _options.windowSeconds Then
                bucket.WindowStart = now
                bucket.Count = 0
            End If

            bucket.Count += 1
            _buckets(key) = bucket
            retryAfter = Math.Max(CInt(Math.Ceiling(bucket.WindowStart + _options.windowSeconds - now)), 1)
        End SyncLock

        Dim remaining = Math.Max(_options.limit - bucket.Count, 0)
        Dim resetSeconds = Math.Max(CInt(Math.Ceiling(bucket.WindowStart + _options.windowSeconds - now)), 0)

        If bucket.Count > _options.limit Then
            context.Response.StatusCode = 429
            context.Response.ContentType = "application/json"
            context.Response.Headers("RateLimit-Limit") = _options.limit.ToString()
            context.Response.Headers("RateLimit-Remaining") = "0"
            context.Response.Headers("RateLimit-Reset") = resetSeconds.ToString()
            context.Response.Headers("RateLimit-Policy") = $"{_options.limit};w={_options.windowSeconds}"
            context.Response.Headers("Retry-After") = retryAfter.ToString()
            Await context.Response.WriteAsync("{""error"":""" & _options.message & """}")
            Return
        End If

        ' Las cabeceras se escriben antes del resto del pipeline: después la
        ' respuesta ya puede haber comenzado y serían de solo lectura.
        context.Response.Headers("RateLimit-Limit") = _options.limit.ToString()
        context.Response.Headers("RateLimit-Remaining") = remaining.ToString()
        context.Response.Headers("RateLimit-Reset") = resetSeconds.ToString()
        context.Response.Headers("RateLimit-Policy") = $"{_options.limit};w={_options.windowSeconds}"

        Await _next(context)
    End Function

    Private Function ClientKey(context As HttpContext) As String
        If _options.trustProxy Then
            Dim forwarded = context.Request.Headers("X-Forwarded-For").ToString()

            If Not String.IsNullOrWhiteSpace(forwarded) Then
                Return forwarded.Split(","c)(0).Trim()
            End If
        End If

        Return If(context.Connection.RemoteIpAddress?.ToString(), "unknown")
    End Function

    Private Sub Prune(now As Double)
        If _buckets.Count < MaxBuckets Then Return

        Dim expired As New List(Of String)()

        For Each item In _buckets
            If now - item.Value.WindowStart >= _options.windowSeconds Then expired.Add(item.Key)
        Next

        For Each key In expired
            _buckets.Remove(key)
        Next
    End Sub

    Private Shared Function MonotonicSeconds() As Double
        Return Stopwatch.GetTimestamp() / CDbl(Stopwatch.Frequency)
    End Function
End Class

''' <summary>
''' Normaliza los errores al contrato JSON de Node: 404 de rutas desconocidas,
''' 500 sin stack trace y registro del error en el servidor.
''' </summary>
Public Class JsonErrorMiddleware
    Private ReadOnly _next As RequestDelegate
    Private ReadOnly _logger As ILogger(Of JsonErrorMiddleware)

    Public Sub New(nextDelegate As RequestDelegate, logger As ILogger(Of JsonErrorMiddleware))
        _next = nextDelegate
        _logger = logger
    End Sub

    Public Async Function InvokeAsync(context As HttpContext) As Task
        Dim failure As Exception = Nothing

        Try
            Await _next(context)
        Catch ex As Exception
            failure = ex
        End Try

        If failure IsNot Nothing Then
            _logger.LogError(failure, "Error interno en {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, failure.Message)

            If Not context.Response.HasStarted Then
                context.Response.Clear()
                context.Response.StatusCode = 500
                context.Response.ContentType = "application/json"
                Await context.Response.WriteAsync("{""error"":""" & SecurityConfig.InternalErrorMessage & """}")
            End If

            Return
        End If

        If context.Response.StatusCode = 404 AndAlso
            context.Request.Path.StartsWithSegments("/api") AndAlso
            Not context.Response.HasStarted Then

            context.Response.ContentType = "application/json"
            Await context.Response.WriteAsync("{""error"":""" & SecurityConfig.NotFoundMessage & """}")
        End If
    End Function
End Class
