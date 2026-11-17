Imports System
Imports System.Data
Imports System.Data.Common
Imports System.Globalization
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging

''' <summary>Endpoints de utilidades (<c>/api/saludo</c> y <c>/api/hello</c>, públicos).</summary>
<Route("api")>
<ApiController>
Public Class MainController
    Inherits ControllerBase

    Private ReadOnly _context As AppDbContext
    Private ReadOnly _logger As ILogger(Of MainController)

    Public Sub New(context As AppDbContext, logger As ILogger(Of MainController))
        _context = context
        _logger = logger
    End Sub

    <HttpGet("saludo")>
    <AllowAnonymous>
    Public Function Saludo() As IActionResult
        Return Ok(New With {.mensaje = "¡Hola desde .NET con ASP.NET Core!"})
    End Function

    ''' <summary>Health-check de la base de datos: devuelve la fecha del servidor.</summary>
    <HttpGet("hello")>
    <AllowAnonymous>
    Public Async Function Hello() As Task(Of IActionResult)
        Try
            Return Ok(New With {
                .mensaje = "¡Conectado a MariaDB!",
                .fecha = Await ServerDateAsync()
            })
        Catch ex As Exception
            _logger.LogError(ex, "Error al consultar la fecha del servidor: {Message}", ex.Message)
            Return StatusCode(500, New With {.error = "Error en la base de datos"})
        End Try
    End Function

    Private Async Function ServerDateAsync() As Task(Of String)
        Dim connection = _context.Database.GetDbConnection()
        Dim shouldClose = connection.State <> ConnectionState.Open

        If shouldClose Then Await _context.Database.OpenConnectionAsync()

        Try
            Using command = connection.CreateCommand()
                command.CommandText = "SELECT NOW()"
                Dim value = Await command.ExecuteScalarAsync()
                Dim fecha = Convert.ToDateTime(value, CultureInfo.InvariantCulture)

                Return fecha.ToUniversalTime().ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)
            End Using
        Finally
            If shouldClose Then _context.Database.CloseConnection()
        End Try
    End Function
End Class
