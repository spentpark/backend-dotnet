Imports System
Imports System.Collections.Generic

''' <summary>
''' Envoltorio paginado común de las colecciones de la API
''' (<c>{ "data": [...], "pagination": { ... } }</c>), igual que backend-node20.
''' </summary>
Public Class PaginatedResult(Of T)
    Public Property data As IEnumerable(Of T)
    Public Property pagination As PaginationInfo

    ''' <summary>Construye la respuesta paginada calculando <c>totalPages</c>.</summary>
    Public Shared Function Create(rows As IEnumerable(Of T), total As Integer, page As Integer, limit As Integer) As PaginatedResult(Of T)
        Dim safeLimit = If(limit <= 0, 1, limit)

        Return New PaginatedResult(Of T) With {
            .data = rows,
            .pagination = New PaginationInfo With {
                .currentPage = page,
                .perPage = limit,
                .totalRecords = total,
                .totalPages = CInt(Math.Ceiling(total / CDbl(safeLimit)))
            }
        }
    End Function
End Class
