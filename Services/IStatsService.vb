Imports System.Threading.Tasks

''' <summary>Reglas de negocio de las estadísticas del catálogo.</summary>
Public Interface IStatsService
    Function GetStatsAsync() As Task(Of Object)
End Interface
