Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks

''' <summary>
''' Estadísticas del catálogo con caché en memoria de 10 minutos (los GROUP BY
''' sobre la BD remota son costosos, igual que en Node).
''' </summary>
Public Class StatsService
    Implements IStatsService

    Public Const CacheTtlMinutes As Integer = 10

    Private Shared ReadOnly CacheSync As New Object()
    Private Shared _cache As Object
    Private Shared _cacheExpiresAt As DateTimeOffset = DateTimeOffset.MinValue

    Private ReadOnly _repository As IStatsRepository

    Public Sub New(repository As IStatsRepository)
        _repository = repository
    End Sub

    ''' <summary>Vacía la caché (pruebas y utilidades).</summary>
    Public Shared Sub ClearCache()
        SyncLock CacheSync
            _cache = Nothing
            _cacheExpiresAt = DateTimeOffset.MinValue
        End SyncLock
    End Sub

    Public Async Function GetStatsAsync() As Task(Of Object) Implements IStatsService.GetStatsAsync
        SyncLock CacheSync
            If _cache IsNot Nothing AndAlso DateTimeOffset.UtcNow < _cacheExpiresAt Then
                Return _cache
            End If
        End SyncLock

        Dim data = Await LoadAsync()

        SyncLock CacheSync
            _cache = data
            _cacheExpiresAt = DateTimeOffset.UtcNow.AddMinutes(CacheTtlMinutes)
        End SyncLock

        Return data
    End Function

    Private Async Function LoadAsync() As Task(Of Object)
        Dim summary = Await _repository.GetSummaryAsync()
        Dim platforms = Await _repository.GetByPlatformAsync(StatsRepository.DefaultDistributionLimit)
        Dim genres = Await _repository.GetByGenreAsync(StatsRepository.DefaultDistributionLimit)
        Dim decades = Await _repository.GetByDecadeAsync(StatsRepository.DefaultDistributionLimit)

        Dim summaryPayload = New With {
            .totalGames = summary.totalGames,
            .scoredGames = summary.scoredGames,
            .totalPlatforms = summary.totalPlatforms,
            .totalGenres = summary.totalGenres
        }

        Return New Dictionary(Of String, Object) From {
            {"summary", summaryPayload},
            {"platforms", platforms},
            {"genres", genres},
            {"decades", decades}
        }
    End Function
End Class
