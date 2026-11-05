Imports System.Threading.Tasks

''' <summary>Acceso a datos de juegos similares precalculados.</summary>
Public Interface ISimilarGameRepository
    Function FindByGameIdAsync(idGame As Integer) As Task(Of SimilarGame)
End Interface
