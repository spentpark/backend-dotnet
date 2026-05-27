Imports Microsoft.AspNetCore.Mvc

<Route("api/reviews")>
<ApiController>
Public Class ReviewsController
    Inherits ControllerBase

    Private ReadOnly _reviewService As IReviewService

    Public Sub New(reviewService As IReviewService)
        _reviewService = reviewService
    End Sub

    <HttpGet("{gameId}")>
    Public Async Function GetReviews(gameId As Integer) As Task(Of IActionResult)
        Dim reviews = Await _reviewService.GetReviewsByGameIdAsync(gameId)
        Return Ok(reviews)
    End Function

End Class