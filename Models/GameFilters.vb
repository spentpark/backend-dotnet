''' <summary>
''' Filtros del catálogo de juegos (<c>GET /api/games</c>), con los mismos
''' nombres de query string que backend-node20.
''' </summary>
Public Class GameFilters
    Public Property platform As String
    Public Property genre As String
    Public Property year As String
    Public Property decade As String
    Public Property players As String
    Public Property sort As String

    ''' <summary>
    ''' <c>True</c> si hay algún filtro además de la plataforma (activa el camino
    ''' "avanzado" del listado, igual que en Node).
    ''' </summary>
    Public Function HasAdvancedFilters() As Boolean
        Return Not String.IsNullOrEmpty(genre) OrElse
            Not String.IsNullOrEmpty(year) OrElse
            Not String.IsNullOrEmpty(decade) OrElse
            Not String.IsNullOrEmpty(players) OrElse
            Not String.IsNullOrEmpty(sort)
    End Function

    ''' <summary>Plataforma y género usados por el juego aleatorio.</summary>
    Public Function HasRandomFilters() As Boolean
        Return Not String.IsNullOrEmpty(platform) OrElse Not String.IsNullOrEmpty(genre)
    End Function

    ''' <summary>Sin plataforma, Node responde una página vacía (NULL = ?).</summary>
    Public Function MatchesNothing() As Boolean
        Return String.IsNullOrEmpty(platform)
    End Function
End Class
