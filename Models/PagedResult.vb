Imports System
Imports System.Collections.Generic

Public Class PagedResult(Of T)
    Public Property page As Integer
    Public Property limit As Integer
    Public Property total As Integer
    Public Property data As IEnumerable(Of T)
End Class