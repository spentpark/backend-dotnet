Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc

''' <summary>Estadísticas del catálogo (<c>/api/stats</c>, JWT).</summary>
<Route("api/stats")>
<ApiController>
<Authorize>
Public Class StatsController
    Inherits ControllerBase

    Private ReadOnly _statsService As IStatsService

    Public Sub New(statsService As IStatsService)
        _statsService = statsService
    End Sub

    <HttpGet>
    Public Async Function GetStats() As Task(Of IActionResult)
        Return Ok(Await _statsService.GetStatsAsync())
    End Function
End Class
