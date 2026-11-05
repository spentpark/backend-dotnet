Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc

''' <summary>
''' CRUD de plataformas (<c>/api/platforms</c>) con los mismos nombres de endpoint
''' que backend-node20. Lecturas con JWT; escrituras con rol <c>admin</c>.
''' </summary>
<Route("api/platforms")>
<ApiController>
<Authorize>
Public Class PlatformsController
    Inherits ControllerBase

    Private ReadOnly _platformService As IPlatformService

    Public Sub New(platformService As IPlatformService)
        _platformService = platformService
    End Sub

    ''' <summary><c>GET /api/platforms</c> → array de plataformas con <c>url</c>.</summary>
    <HttpGet>
    Public Async Function GetPlatforms() As Task(Of IActionResult)
        Return Ok(Await _platformService.GetPlatformsAsync())
    End Function

    ''' <summary><c>GET /api/platforms/{id}/top-games</c> → top 10 por nota.</summary>
    <HttpGet("{id:int}/top-games")>
    Public Async Function GetTopGames(<FromRoute> id As Integer) As Task(Of IActionResult)
        Return Ok(Await _platformService.GetTopGamesAsync(id))
    End Function

    ''' <summary><c>GET /api/platforms/{id}</c></summary>
    <HttpGet("{id:int}")>
    Public Async Function GetPlatformById(<FromRoute> id As Integer) As Task(Of IActionResult)
        Dim platform = Await _platformService.GetPlatformByIdAsync(id)

        If platform Is Nothing Then
            Return NotFound(New With {.error = "Plataforma no encontrada"})
        End If

        Return Ok(platform)
    End Function

    ''' <summary><c>POST /api/platforms</c> (rol admin).</summary>
    <HttpPost>
    <Authorize(Roles:="admin")>
    Public Async Function CreatePlatform(<FromBody> platform As Platform) As Task(Of IActionResult)
        Dim created = Await _platformService.CreatePlatformAsync(platform)
        Return StatusCode(201, created)
    End Function

    ''' <summary><c>PUT /api/platforms/{id}</c> (rol admin).</summary>
    <HttpPut("{id:int}")>
    <Authorize(Roles:="admin")>
    Public Async Function UpdatePlatform(<FromRoute> id As Integer, <FromBody> platform As Platform) As Task(Of IActionResult)
        Dim updated = Await _platformService.UpdatePlatformAsync(id, platform)

        If Not updated Then
            Return NotFound(New With {.error = "Plataforma no encontrada"})
        End If

        Return Ok(New With {
            .message = "Plataforma actualizada",
            .result = New With {.affectedRows = 1}
        })
    End Function

    ''' <summary><c>DELETE /api/platforms/{id}</c> (rol admin).</summary>
    <HttpDelete("{id:int}")>
    <Authorize(Roles:="admin")>
    Public Async Function DeletePlatform(<FromRoute> id As Integer) As Task(Of IActionResult)
        Dim deleted = Await _platformService.DeletePlatformAsync(id)

        If Not deleted Then
            Return NotFound(New With {.error = "Plataforma no encontrada"})
        End If

        Return Ok(New With {
            .message = "Plataforma eliminada",
            .result = New With {.affectedRows = 1}
        })
    End Function
End Class
