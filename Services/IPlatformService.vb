Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Interface IPlatformService
    Function GetActivePlatformsAsync() As Task(Of IEnumerable(Of PlatformSummary))
End Interface