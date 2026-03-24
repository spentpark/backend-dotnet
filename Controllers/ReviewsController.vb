Imports Microsoft.AspNetCore.Mvc
Imports Microsoft.EntityFrameworkCore

<Route("api/reviews")>
<ApiController>
Public Class ReviewsController
    Inherits ControllerBase

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    <HttpGet("{gameId}")>
    Public Async Function GetReviews(gameId As Integer) As Task(Of IActionResult)
        Dim reviews = Await _context.Review.Where(Function(r) r.GameId = gameId).ToListAsync()
        Return Ok(reviews)
    End Function

    
End Class