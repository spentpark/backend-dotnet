Imports System
Imports System.IO
Imports System.Net
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.Logging.Abstractions
Imports Xunit

''' <summary>Pruebas de los middlewares de seguridad (headers, body, rate limit y errores).</summary>
Public Class SecurityHeadersMiddlewareTests

    Private Shared Function CreateContext(path As String) As DefaultHttpContext
        Dim context = New DefaultHttpContext()
        context.Request.Path = path
        context.Response.Body = New MemoryStream()

        Return context
    End Function

    Private Shared Async Function Invoke(middleware As SecurityHeadersMiddleware, context As HttpContext) As Task
        Await middleware.InvokeAsync(context)
    End Function

    <Fact>
    Public Async Function AddsHelmetHeadersAndCsp() As Task
        Dim context = CreateContext("/api/games")
        Dim middleware = New SecurityHeadersMiddleware(
            Function(ctx) Task.CompletedTask)

        Await middleware.InvokeAsync(context)

        Assert.Equal("nosniff", context.Response.Headers("X-Content-Type-Options").ToString())
        Assert.Equal("SAMEORIGIN", context.Response.Headers("X-Frame-Options").ToString())
        Assert.Contains("default-src", context.Response.Headers("Content-Security-Policy").ToString())
        Assert.Contains("max-age=31536000", context.Response.Headers("Strict-Transport-Security").ToString())
    End Function

    <Fact>
    Public Async Function SkipsCspForDocumentationAndKeepsExistingHeaders() As Task
        Dim context = CreateContext("/api-docs/index.html")
        context.Response.Headers("X-Frame-Options") = "DENY"
        Dim middleware = New SecurityHeadersMiddleware(Function(ctx) Task.CompletedTask)

        Await middleware.InvokeAsync(context)

        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"))
        Assert.Equal("DENY", context.Response.Headers("X-Frame-Options").ToString())
    End Function
End Class

Public Class BodyLimitMiddlewareTests

    Private Shared Function CreateContext(contentLength As Long?) As DefaultHttpContext
        Dim context = New DefaultHttpContext()
        context.Request.Path = "/api/games"
        context.Response.Body = New MemoryStream()
        context.Request.ContentLength = contentLength

        Return context
    End Function

    Private Shared Function ReadBody(context As HttpContext) As String
        context.Response.Body.Position = 0
        Return New StreamReader(context.Response.Body).ReadToEnd()
    End Function

    <Fact>
    Public Async Function RejectsBodiesLargerThan10Kb() As Task
        Dim context = CreateContext(SecurityConfig.MaxBodyBytes + 1)
        Dim called = False
        Dim middleware = New BodyLimitMiddleware(
            Function(ctx)
                called = True
                Return Task.CompletedTask
            End Function)

        Await middleware.InvokeAsync(context)

        Assert.False(called)
        Assert.Equal(413, context.Response.StatusCode)
        Assert.Contains("demasiado grande", ReadBody(context))
    End Function

    <Fact>
    Public Async Function AllowsSmallOrUnknownBodies() As Task
        Dim calls = 0
        Dim middleware = New BodyLimitMiddleware(
            Function(ctx)
                calls += 1
                Return Task.CompletedTask
            End Function)

        Await middleware.InvokeAsync(CreateContext(SecurityConfig.MaxBodyBytes))
        Await middleware.InvokeAsync(CreateContext(Nothing))

        Assert.Equal(2, calls)
    End Function
End Class

Public Class JsonErrorMiddlewareTests

    Private Shared Function CreateContext(path As String) As DefaultHttpContext
        Dim context = New DefaultHttpContext()
        context.Request.Path = path
        context.Request.Method = "GET"
        context.Response.Body = New MemoryStream()

        Return context
    End Function

    Private Shared Function ReadBody(context As HttpContext) As String
        context.Response.Body.Position = 0
        Return New StreamReader(context.Response.Body).ReadToEnd()
    End Function

    <Fact>
    Public Async Function UnknownApiRoute_ReturnsJson404() As Task
        Dim context = CreateContext("/api/no-existe")
        Dim middleware = New JsonErrorMiddleware(
            Function(ctx)
                ctx.Response.StatusCode = 404
                Return Task.CompletedTask
            End Function,
            NullLogger(Of JsonErrorMiddleware).Instance)

        Await middleware.InvokeAsync(context)

        Assert.Equal(404, context.Response.StatusCode)
        Assert.Contains("Ruta no encontrada", ReadBody(context))
    End Function

    <Fact>
    Public Async Function UnknownNonApiRoute_KeepsEmptyBody() As Task
        Dim context = CreateContext("/otra")
        Dim middleware = New JsonErrorMiddleware(
            Function(ctx)
                ctx.Response.StatusCode = 404
                Return Task.CompletedTask
            End Function,
            NullLogger(Of JsonErrorMiddleware).Instance)

        Await middleware.InvokeAsync(context)

        Assert.Equal(404, context.Response.StatusCode)
        Assert.Equal("", ReadBody(context))
    End Function

    Private Shared Function ThrowBoom(context As HttpContext) As Task
        Throw New InvalidOperationException("boom")
    End Function

    <Fact>
    Public Async Function UnhandledException_ReturnsJson500() As Task
        Dim context = CreateContext("/api/games")
        Dim middleware = New JsonErrorMiddleware(
            AddressOf ThrowBoom,
            NullLogger(Of JsonErrorMiddleware).Instance)

        Await middleware.InvokeAsync(context)

        Assert.Equal(500, context.Response.StatusCode)
        Assert.Contains("Error interno del servidor", ReadBody(context))
    End Function
End Class

Public Class RateLimitMiddlewareTests

    Private Shared Function CreateContext(path As String, Optional method As String = "GET",
                                          Optional ip As String = "10.0.0.1") As DefaultHttpContext
        Dim context = New DefaultHttpContext()
        context.Request.Path = path
        context.Request.Method = method
        context.Response.Body = New MemoryStream()
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip)

        Return context
    End Function

    Private Shared Function Options(limit As Integer, Optional method As String = "",
                                    Optional trustProxy As Boolean = False) As RateLimitOptions
        Return New RateLimitOptions With {
            .windowSeconds = 60,
            .limit = limit,
            .message = "Demasiadas solicitudes",
            .pathPrefix = "/api",
            .method = method,
            .trustProxy = trustProxy
        }
    End Function

    <Fact>
    Public Async Function WithinLimit_PassesAndSetsHeaders() As Task
        Dim calls = 0
        Dim middleware = New RateLimitMiddleware(
            Function(ctx)
                calls += 1
                Return Task.CompletedTask
            End Function,
            Options(2))

        Dim context = CreateContext("/api/games")
        Await middleware.InvokeAsync(context)

        Assert.Equal(1, calls)
        Assert.Equal("2", context.Response.Headers("RateLimit-Limit").ToString())
        Assert.Equal("1", context.Response.Headers("RateLimit-Remaining").ToString())
    End Function

    <Fact>
    Public Async Function ExceedingLimit_Returns429WithRetryAfter() As Task
        Dim middleware = New RateLimitMiddleware(
            Function(ctx) Task.CompletedTask,
            Options(1))

        Dim first = CreateContext("/api/games")
        Await middleware.InvokeAsync(first)
        Assert.Equal("1", first.Response.Headers("RateLimit-Limit").ToString())
        Assert.Equal("0", first.Response.Headers("RateLimit-Remaining").ToString())

        Dim second = CreateContext("/api/games")
        Await middleware.InvokeAsync(second)

        Assert.Equal(429, second.Response.StatusCode)
        Assert.True(second.Response.Headers.ContainsKey("Retry-After"))
    End Function

    <Fact>
    Public Async Function DifferentPathsOrMethods_AreNotLimited() As Task
        Dim calls = 0
        Dim middleware = New RateLimitMiddleware(
            Function(ctx)
                calls += 1
                Return Task.CompletedTask
            End Function,
            Options(1, method:="POST"))

        Await middleware.InvokeAsync(CreateContext("/otra"))
        Await middleware.InvokeAsync(CreateContext("/api/games", method:="GET"))

        Assert.Equal(2, calls)
    End Function

    <Fact>
    Public Async Function TrustProxy_UsesForwardedForAddress() As Task
        Dim middleware = New RateLimitMiddleware(
            Function(ctx) Task.CompletedTask,
            Options(1, trustProxy:=True))

        Dim first = CreateContext("/api/games")
        first.Request.Headers("X-Forwarded-For") = "1.1.1.1, 2.2.2.2"
        Await middleware.InvokeAsync(first)

        Dim sameIp = CreateContext("/api/games", ip:="9.9.9.9")
        sameIp.Request.Headers("X-Forwarded-For") = "1.1.1.1"
        Await middleware.InvokeAsync(sameIp)

        Dim otherIp = CreateContext("/api/games", ip:="9.9.9.9")
        otherIp.Request.Headers("X-Forwarded-For") = "3.3.3.3"
        Await middleware.InvokeAsync(otherIp)

        Assert.Equal(429, sameIp.Response.StatusCode)
        Assert.Equal(200, otherIp.Response.StatusCode)
    End Function
End Class
