Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc

''' <summary>Reseñas paginadas de un juego (<c>/api/reviews/{id}</c>, JWT).</summary>
<Route("api/reviews")>
<ApiController>
<Authorize>
Public Class ReviewsController
    Inherits ControllerBase

    Private ReadOnly _reviewService As IReviewService

    Public Sub New(reviewService As IReviewService)
        _reviewService = reviewService
    End Sub

    <HttpGet("{id:int}")>
    Public Async Function GetReviews(
        <FromRoute> id As Integer,
        <FromQuery> Optional page As Integer = 1,
        <FromQuery> Optional limit As Integer = 10
    ) As Task(Of IActionResult)

        Dim result = Await _reviewService.GetReviewsAsync(id, page, limit)

        If result.pagination.totalRecords = 0 Then
            Return NotFound(New With {.error = "Reseña no encontrada"})
        End If

        Return Ok(result)
    End Function
End Class
