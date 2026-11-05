Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>
''' Réplica de <c>similar-games.service.js</c>: parsea el JSON de
''' <c>similar_game</c>, descarta entradas corruptas, excluye el propio juego,
''' ordena por similitud descendente y calcula el porcentaje redondeado.
''' </summary>
Public Class SimilarGamesService
    Implements ISimilarGamesService

    Private Const PercentageMultiplier As Double = 100.0
    Private Const PercentageFactor As Double = 100.0

    Private ReadOnly _repository As ISimilarGameRepository

    Public Sub New(repository As ISimilarGameRepository)
        _repository = repository
    End Sub

    Public Async Function GetSimilarGamesAsync(gameId As Integer, limit As Integer?) As Task(Of SimilarGamesResult) Implements ISimilarGamesService.GetSimilarGamesAsync
        Dim row = Await _repository.FindByGameIdAsync(gameId)

        If row Is Nothing Then Return Nothing

        Dim parsed = ParseSimilarGames(row.Json)

        Dim similar = parsed.
            Where(Function(entry) entry.id <> gameId).
            OrderByDescending(Function(entry) entry.similarity).
            ToList()

        If limit.HasValue AndAlso limit.Value > 0 Then
            similar = similar.Take(limit.Value).ToList()
        End If

        If similar.Count = 0 Then Return Nothing

        Return New SimilarGamesResult With {
            .gameId = gameId,
            .total = similar.Count,
            .similarGames = similar
        }
    End Function

    ''' <summary>Parsea el TEXT de <c>similar_game</c>; lanza si no es un array JSON.</summary>
    Public Shared Function ParseSimilarGames(rawJson As String) As List(Of SimilarGameEntry)
        Dim entries As New List(Of SimilarGameEntry)()

        If String.IsNullOrEmpty(rawJson) Then Return entries

        Dim document As JsonDocument

        Try
            document = JsonDocument.Parse(rawJson)
        Catch ex As JsonException
            Throw New SimilarGamesParseException("El campo json de similar_game no es JSON válido")
        End Try

        Using document
            If document.RootElement.ValueKind <> JsonValueKind.Array Then
                Throw New SimilarGamesParseException("El campo ""json"" de similar_game no contiene un array")
            End If

            For Each element In document.RootElement.EnumerateArray()
                Dim entry = ToSimilarGame(element)

                If entry IsNot Nothing Then entries.Add(entry)
            Next
        End Using

        Return entries
    End Function

    ''' <summary>Normaliza una entrada del JSON; <c>Nothing</c> si está corrupta.</summary>
    Private Shared Function ToSimilarGame(element As JsonElement) As SimilarGameEntry
        If element.ValueKind <> JsonValueKind.Object Then Return Nothing

        Dim idElement As JsonElement
        Dim similarityElement As JsonElement

        If Not element.TryGetProperty("id", idElement) OrElse idElement.ValueKind <> JsonValueKind.Number Then
            Return Nothing
        End If

        If Not element.TryGetProperty("similarity", similarityElement) OrElse
            similarityElement.ValueKind <> JsonValueKind.Number Then
            Return Nothing
        End If

        Dim id As Integer
        Dim similarity As Double

        If Not idElement.TryGetInt32(id) Then Return Nothing
        If Not similarityElement.TryGetDouble(similarity) Then Return Nothing
        If Double.IsNaN(similarity) OrElse Double.IsInfinity(similarity) Then Return Nothing

        Dim title As String = Nothing
        Dim titleElement As JsonElement

        If element.TryGetProperty("title", titleElement) AndAlso titleElement.ValueKind = JsonValueKind.String Then
            title = titleElement.GetString()
        End If

        Return New SimilarGameEntry With {
            .id = id,
            .title = title,
            .similarity = similarity,
            .similarityPercentage = ToSimilarityPercentage(similarity)
        }
    End Function

    ''' <summary>Convierte la similitud (0–1) al porcentaje recortado a [0, 100].</summary>
    Public Shared Function ToSimilarityPercentage(similarity As Double) As Double
        Dim percentage = Math.Min(PercentageMultiplier, Math.Max(0.0, similarity * PercentageMultiplier))

        Return Math.Round(percentage * PercentageFactor) / PercentageFactor
    End Function
End Class
