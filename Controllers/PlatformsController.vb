Imports Microsoft.AspNetCore.Mvc
Imports Microsoft.EntityFrameworkCore

<Route("api/platforms")>
<ApiController>
Public Class PlatformsController
    Inherits ControllerBase

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    <HttpGet>
    Public Async Function GetPlatforms() As Task(Of IActionResult)
        Dim platforms = Await _context.Platform _
            .Where(Function(p) Not String.IsNullOrEmpty(p.Url)) _
            .ToListAsync()

        Return Ok(platforms)
    End Function
End Class