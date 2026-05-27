Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Interface IPlatformRepository
    Function GetAllWithUrlAsync() As Task(Of IEnumerable(Of PlatformSummary))
End Interface