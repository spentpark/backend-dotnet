Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore

''' <summary>
''' Consultas EF Core sobre la tabla <c>games</c>. Reproduce los caminos del
''' repositorio de backend-node20: listado por plataforma, filtros avanzados con
''' orden permitido, búsqueda por prefijo y juego aleatorio con tráiler.
''' </summary>
Public Class GameRepository
    Implements IGameRepository

    Private Const RandomAttempts As Integer = 6

    Private ReadOnly _context As AppDbContext

    Public Sub New(context As AppDbContext)
        _context = context
    End Sub

    Public Async Function FindSummariesPaginatedAsync(filters As GameFilters, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of GameSummary)) Implements IGameRepository.FindSummariesPaginatedAsync
        Dim query As IQueryable(Of Game) = _context.games.AsQueryable()

        If Not filters.HasAdvancedFilters() Then
            ' Camino clásico de Node: solo plataforma exacta; sin plataforma no
            ' hay resultados (la comparación con NULL nunca coincide).
            If String.IsNullOrEmpty(filters.platform) Then
                Return PaginatedResult(Of GameSummary).Create(New List(Of GameSummary)(), 0, page, limit)
            End If

            query = query.Where(Function(g) g.Platform = filters.platform)
            query = query.OrderBy(Function(g) g.Id)
        Else
            If Not String.IsNullOrEmpty(filters.platform) Then
                Dim platform = filters.platform
                query = query.Where(Function(g) g.Platform = platform)
            End If

            If Not String.IsNullOrEmpty(filters.genre) Then
                Dim genre = filters.genre
                query = query.Where(Function(g) g.Genre = genre)
            End If

            If Not String.IsNullOrEmpty(filters.year) Then
                Dim year = filters.year.Substring(0, Math.Min(4, filters.year.Length))
                query = query.Where(Function(g) g.ReleaseDate IsNot Nothing AndAlso g.ReleaseDate.StartsWith(year))
            End If

            If Not String.IsNullOrEmpty(filters.decade) Then
                Dim decade = filters.decade.Substring(0, Math.Min(3, filters.decade.Length))
                query = query.Where(Function(g) g.ReleaseDate IsNot Nothing AndAlso g.ReleaseDate.StartsWith(decade))
            End If

            If Not String.IsNullOrEmpty(filters.players) Then
                Dim players = filters.players
                query = query.Where(Function(g) g.Players IsNot Nothing AndAlso g.Players.StartsWith(players))
            End If

            query = ApplySort(query, filters.sort)
        End If

        Return Await PaginateAsync(query, page, limit)
    End Function

    Public Async Function FindByTitlePaginatedAsync(title As String, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of GameSummary)) Implements IGameRepository.FindByTitlePaginatedAsync
        Dim query = _context.games.Where(Function(g) g.Title.StartsWith(title))
        Return Await PaginateAsync(query.OrderBy(Function(g) g.Title), page, limit)
    End Function

    Public Async Function FindRandomAsync(filters As GameFilters) As Task(Of Game) Implements IGameRepository.FindRandomAsync
        Dim random As New Random()
        Dim query = _context.games.Where(
            Function(g) g.Youtube_Trailer IsNot Nothing AndAlso g.Youtube_Trailer <> "")

        If Not String.IsNullOrEmpty(filters.platform) Then
            Dim platform = filters.platform
            query = query.Where(Function(g) g.Platform = platform)
        End If

        If Not String.IsNullOrEmpty(filters.genre) Then
            Dim genre = filters.genre
            query = query.Where(Function(g) g.Genre = genre)
        End If

        If filters.HasRandomFilters() Then
            ' Con plataforma/género se materializan solo los ids candidatos: la
            ' tabla es enorme y un MAX(id) con filtros recorre demasiadas filas.
            Dim candidateIds = Await query.Select(Function(g) g.Id).ToListAsync()

            If candidateIds.Count = 0 Then Return Nothing

            Return Await _context.games.FindAsync(candidateIds(random.Next(candidateIds.Count)))
        End If

        ' Sin filtros: id máximo por la PK (barato) y sorteo de un id dentro del
        ' rango; los huecos se reintentan y, si fallan todos, se usa el primero.
        Dim maxId = Await _context.games.
            OrderByDescending(Function(g) g.Id).
            Select(Function(g) g.Id).
            FirstOrDefaultAsync()

        If maxId = 0 Then Return Nothing

        For attempt = 1 To RandomAttempts
            Dim game = Await _context.games.FindAsync(random.Next(1, maxId + 1))

            If game IsNot Nothing AndAlso Not String.IsNullOrEmpty(game.Youtube_Trailer) Then
                Return game
            End If
        Next

        Return Await query.OrderBy(Function(g) g.Id).FirstOrDefaultAsync()
    End Function

    Public Async Function GetByIdAsync(id As Integer) As Task(Of Game) Implements IGameRepository.GetByIdAsync
        Return Await _context.games.FindAsync(id)
    End Function

    Public Async Function CreateAsync(game As Game) As Task(Of Game) Implements IGameRepository.CreateAsync
        _context.games.Add(game)
        Await _context.SaveChangesAsync()
        Return game
    End Function

    Public Async Function UpdateAsync(id As Integer, game As Game) As Task(Of Boolean) Implements IGameRepository.UpdateAsync
        Dim existing = Await _context.games.FindAsync(id)

        If existing Is Nothing Then Return False

        CopyFields(game, existing)
        Await _context.SaveChangesAsync()
        Return True
    End Function

    Public Async Function DeleteAsync(id As Integer) As Task(Of Boolean) Implements IGameRepository.DeleteAsync
        Dim existing = Await _context.games.FindAsync(id)

        If existing Is Nothing Then Return False

        _context.games.Remove(existing)
        Await _context.SaveChangesAsync()
        Return True
    End Function

    ''' <summary>Whitelist de ordenaciones del catálogo (nunca input concatenado).</summary>
    Private Shared Function ApplySort(query As IQueryable(Of Game), sort As String) As IQueryable(Of Game)
        Select Case If(sort, "id_asc").ToLowerInvariant()
            Case "id_desc"
                Return query.OrderByDescending(Function(g) g.Id)
            Case "title_asc"
                Return query.OrderBy(Function(g) g.Title)
            Case "title_desc"
                Return query.OrderByDescending(Function(g) g.Title)
            Case "year_asc"
                Return query.OrderBy(Function(g) g.ReleaseDate).ThenBy(Function(g) g.Id)
            Case "year_desc"
                Return query.OrderByDescending(Function(g) g.ReleaseDate).ThenByDescending(Function(g) g.Id)
            Case "score_desc"
                Return query.
                    OrderByDescending(Function(g) If(String.IsNullOrEmpty(g.Metacritic_Score), 0, Convert.ToInt32(g.Metacritic_Score))).
                    ThenBy(Function(g) g.Id)
            Case Else
                Return query.OrderBy(Function(g) g.Id)
        End Select
    End Function

    ''' <summary>Ejecuta COUNT + SELECT de resumen (id, title, platform) paginados.</summary>
    Private Shared Async Function PaginateAsync(query As IQueryable(Of Game), page As Integer, limit As Integer) As Task(Of PaginatedResult(Of GameSummary))
        Dim total = Await query.CountAsync()
        Dim rows = Await query.
            Skip((page - 1) * limit).
            Take(limit).
            Select(Function(g) New GameSummary With {
                .id = g.Id,
                .title = g.Title,
                .platform = g.Platform
            }).
            ToListAsync()

        Return PaginatedResult(Of GameSummary).Create(rows, total, page, limit)
    End Function

    ''' <summary>Copia los campos mutables del cuerpo sobre la entidad existente.</summary>
    Private Shared Sub CopyFields(source As Game, target As Game)
        ' Actualizacion parcial como `UPDATE ... SET ?` de Node: los campos
        ' ausentes en el body llegan como Nothing y no se sobreescriben.
        If source.Title IsNot Nothing Then target.Title = source.Title
        If source.Description IsNot Nothing Then target.Description = source.Description
        If source.GameDBId.HasValue Then target.GameDBId = source.GameDBId
        If source.Image_Large IsNot Nothing Then target.Image_Large = source.Image_Large
        If source.Image_Medium IsNot Nothing Then target.Image_Medium = source.Image_Medium
        If source.Image_Original IsNot Nothing Then target.Image_Original = source.Image_Original
        If source.Image_Front IsNot Nothing Then target.Image_Front = source.Image_Front
        If source.Platform IsNot Nothing Then target.Platform = source.Platform
        If source.Publisher IsNot Nothing Then target.Publisher = source.Publisher
        If source.ReleaseDate IsNot Nothing Then target.ReleaseDate = source.ReleaseDate
        If source.Players IsNot Nothing Then target.Players = source.Players
        If source.Genre IsNot Nothing Then target.Genre = source.Genre
        If source.Youtube_Trailer IsNot Nothing Then target.Youtube_Trailer = source.Youtube_Trailer
        If source.Youtube_Walk IsNot Nothing Then target.Youtube_Walk = source.Youtube_Walk
        If source.Wiki_Url IsNot Nothing Then target.Wiki_Url = source.Wiki_Url
        If source.Wiki_Page IsNot Nothing Then target.Wiki_Page = source.Wiki_Page
        If source.Youtube_Ending IsNot Nothing Then target.Youtube_Ending = source.Youtube_Ending
        If source.Youtube_Secrets IsNot Nothing Then target.Youtube_Secrets = source.Youtube_Secrets
        If source.Youtube_Ost IsNot Nothing Then target.Youtube_Ost = source.Youtube_Ost
        If source.Youtube_Speedrun IsNot Nothing Then target.Youtube_Speedrun = source.Youtube_Speedrun
        If source.Youtube_Review IsNot Nothing Then target.Youtube_Review = source.Youtube_Review
        If source.Spotify_Ost IsNot Nothing Then target.Spotify_Ost = source.Spotify_Ost
        If source.Ign_Url IsNot Nothing Then target.Ign_Url = source.Ign_Url
        If source.Metacritic_Url IsNot Nothing Then target.Metacritic_Url = source.Metacritic_Url
        If source.Metacritic_Score IsNot Nothing Then target.Metacritic_Score = source.Metacritic_Score
        If source.Metacritic_ScoreU IsNot Nothing Then target.Metacritic_ScoreU = source.Metacritic_ScoreU
        If source._3djuegos_Url IsNot Nothing Then target._3djuegos_Url = source._3djuegos_Url
        If source.Areajugones_Url IsNot Nothing Then target.Areajugones_Url = source.Areajugones_Url
        If source.Meristation_Url IsNot Nothing Then target.Meristation_Url = source.Meristation_Url
        If source._3djuegos_Score IsNot Nothing Then target._3djuegos_Score = source._3djuegos_Score
        If source.Opencritic_Url IsNot Nothing Then target.Opencritic_Url = source.Opencritic_Url
        If source.Esrb_Letter IsNot Nothing Then target.Esrb_Letter = source.Esrb_Letter
        If source.Esbr_Message IsNot Nothing Then target.Esbr_Message = source.Esbr_Message
    End Sub
End Class
