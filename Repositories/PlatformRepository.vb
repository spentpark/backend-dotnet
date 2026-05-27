Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore

Public Class PlatformRepository
    Implements IPlatformRepository

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    Public Async Function GetAllWithUrlAsync() As Task(Of IEnumerable(Of PlatformSummary)) Implements IPlatformRepository.GetAllWithUrlAsync
        Return Await _context.platform _
            .Where(Function(p) Not String.IsNullOrEmpty(p.Url)) _
            .Select(Function(p) New PlatformSummary With {
                .id = p.Id,
                .description = p.Description,
                .url = p.Url
            }) _
            .ToListAsync()
    End Function
End Class