Imports Microsoft.AspNetCore.Mvc

<Route("api/platforms")>
<ApiController>
Public Class PlatformsController
    Inherits ControllerBase

    Private ReadOnly _platformService As IPlatformService

    Public Sub New(platformService As IPlatformService)
        _platformService = platformService
    End Sub

    <HttpGet>
    Public Async Function GetPlatforms() As Task(Of IActionResult)
        Dim platforms = Await _platformService.GetActivePlatformsAsync()
        Return Ok(platforms)
    End Function
End Class